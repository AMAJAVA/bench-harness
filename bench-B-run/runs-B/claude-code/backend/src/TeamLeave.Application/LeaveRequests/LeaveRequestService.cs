using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Overlap;
using TeamLeave.Domain.Users;

namespace TeamLeave.Application.LeaveRequests;

/// <summary>
/// Cas d'usage des demandes de congé (US-03 à US-11, step2_arch §5.3). Une transaction par écriture, rejouée sur conflit.
/// Ordre des contrôles : format (400) → existence/périmètre (404) → droit d'agir (403) → état (409) → règles (422).
/// </summary>
public sealed class LeaveRequestService(
    ITeamLeaveDbContext db,
    ICurrentUser currentUser,
    Clock clock,
    UseCaseExecutor executor,
    HolidayCalendar holidays,
    OverlapService overlapService,
    BalanceService balanceService,
    NotificationComposer notifications)
{
    private bool IsHr => currentUser.IsInRole(Role.HrAdmin);

    // ------------------------------------------------------------------ Simulation et soumission

    /// <summary>US-03 : aucune écriture, aucun email, aucune donnée de collègue (CA-03.4, CA-03.5, GAP-10).</summary>
    public async Task<SimulationDto> SimulateAsync(SimulateRequest input, CancellationToken ct)
    {
        var evaluation = await EvaluateAsync(currentUser.Id, input.LeaveTypeId!.Value, input.StartDate!.Value, input.EndDate!.Value, createBalance: false, ct);
        SimulationBalanceDto? balance = null;
        if (evaluation.Type.IsTracked)
        {
            var amount = evaluation.Balance?.AmountHundredths ?? 0;
            var before = amount - evaluation.PendingHundredths;
            balance = new SimulationBalanceDto(
                DayAmount.ToDecimal(amount),
                DayAmount.ToDecimal(evaluation.PendingHundredths),
                DayAmount.ToDecimal(before),
                DayAmount.ToDecimal(before - DayAmount.FromDays(evaluation.WorkingDays.Count)));
        }

        return new SimulationDto(
            evaluation.WorkingDays.Count,
            evaluation.WorkingDays,
            balance,
            evaluation.BlockingReasons.Count == 0,
            evaluation.BlockingReasons.Select(r => r.Code).ToList());
    }

    /// <summary>US-04 : soumission pour soi uniquement (H-11).</summary>
    public async Task<LeaveRequestDto> SubmitAsync(SubmitLeaveRequest input, CancellationToken ct)
    {
        var requesterId = currentUser.Id;
        var id = await executor.ExecuteAsync(
            async token =>
            {
                var evaluation = await EvaluateAsync(requesterId, input.LeaveTypeId!.Value, input.StartDate!.Value, input.EndDate!.Value, createBalance: true, token);
                if (evaluation.BlockingReasons.Count > 0)
                {
                    throw evaluation.BlockingReasons[0];
                }

                var requester = await db.Users.FirstOrDefaultAsync(u => u.Id == requesterId, token);
                if (requester is not { IsActive: true })
                {
                    throw new UnauthenticatedException(); // §6.3 S5 : désactivation concurrente.
                }

                var analysis = await overlapService.AnalyzeAsync(requesterId, requester.ManagerId, evaluation.Period, evaluation.WorkingDays, token);
                var request = LeaveRequest.Submit(
                    Guid.CreateVersion7(),
                    requesterId,
                    evaluation.Type.Id,
                    evaluation.Period,
                    evaluation.WorkingDays.Count,
                    input.Comment,
                    analysis,
                    clock.UtcNow);
                db.LeaveRequests.Add(request);

                // Points de sérialisation (§6.2 M3) : demandeur, solde, type.
                requester.Touch();
                evaluation.Type.Touch();
                evaluation.Balance?.Touch();

                await notifications.QueueSubmissionAsync(request, requester, evaluation.Type, token);
                return request.Id;
            },
            ct);

        return await GetAsync(id, ct);
    }

    // ------------------------------------------------------------------ Décisions

    /// <summary>US-09, US-10 : approbation avec recalcul de l'analyse et acquittement (RB-17, RB-18), débit atomique (RB-12).</summary>
    public async Task<LeaveRequestDto> ApproveAsync(Guid id, ApproveRequest input, CancellationToken ct)
    {
        await executor.ExecuteAsync(
            async token =>
            {
                var (request, requester) = await LoadForDecisionAsync(id, token);
                request.EnsurePending();

                var analysis = await overlapService.AnalyzeAsync(request, requester.ManagerId, token);
                if (analysis.IsAlert && !input.OverlapAcknowledged)
                {
                    throw new DomainException(
                        ErrorCodes.OverlapNotAcknowledged,
                        "Plus de 50 % de l'équipe est absente sur au moins un jour de la demande. Confirmez l'approbation en connaissance de cause.",
                        new Dictionary<string, object?> { ["overlap"] = analysis.ToDto(request.Id, clock.UtcNow) });
                }

                var type = await db.LeaveTypes.FirstAsync(t => t.Id == request.LeaveTypeId, token);
                var now = clock.UtcNow;
                if (type.IsTracked)
                {
                    var balance = await balanceService.GetOrCreateAsync(request.RequesterId, type.Id, token);
                    db.BalanceMovements.Add(balance.Debit(request.DaysCount, request.Id, currentUser.Id, now));
                }

                request.Approve(currentUser.Id, input.Comment, analysis, input.OverlapAcknowledged, now);
                notifications.QueueDecision(request, requester, type);
            },
            ct);

        return await GetAsync(id, ct);
    }

    /// <summary>US-11 : refus motivé (RB-15) ; le commentaire est contrôlé avant tout chargement (CA-11.2).</summary>
    public async Task<LeaveRequestDto> RejectAsync(Guid id, RejectRequest input, CancellationToken ct)
    {
        var comment = LeaveRequest.NormalizeRejectionComment(input.Comment);
        await executor.ExecuteAsync(
            async token =>
            {
                var (request, requester) = await LoadForDecisionAsync(id, token);
                request.EnsurePending();
                var type = await db.LeaveTypes.AsNoTracking().FirstAsync(t => t.Id == request.LeaveTypeId, token);
                request.Reject(currentUser.Id, comment, clock.UtcNow);
                notifications.QueueDecision(request, requester, type);
            },
            ct);

        return await GetAsync(id, ct);
    }

    /// <summary>US-06 : annulation par le demandeur (RB-14), recrédit et email si la demande était approuvée.</summary>
    public async Task<LeaveRequestDto> CancelAsync(Guid id, CancellationToken ct)
    {
        await executor.ExecuteAsync(
            async token =>
            {
                var me = currentUser.Id;
                var isHr = IsHr;
                var row = await (
                        from r in db.LeaveRequests
                        join u in db.Users on r.RequesterId equals u.Id
                        where r.Id == id && (r.RequesterId == me || u.ManagerId == me || isHr)
                        select new { Request = r, Requester = u })
                    .FirstOrDefaultAsync(token)
                    ?? throw new NotFoundException();
                if (row.Request.RequesterId != me)
                {
                    throw new ForbiddenException(ErrorCodes.NotRequester, "Seul le demandeur peut annuler sa demande.");
                }

                var now = clock.UtcNow;
                var previous = row.Request.CancelByRequester(me, clock.Today, now);
                if (previous == LeaveRequestStatus.Approved)
                {
                    var type = await db.LeaveTypes.AsNoTracking().FirstAsync(t => t.Id == row.Request.LeaveTypeId, token);
                    if (type.IsTracked)
                    {
                        var balance = await balanceService.GetOrCreateAsync(me, type.Id, token);
                        db.BalanceMovements.Add(balance.Recredit(row.Request.DaysCount, row.Request.Id, me, now));
                    }

                    await notifications.QueueApprovedCancellationAsync(row.Request, row.Requester, type, token);
                }
            },
            ct);

        return await GetAsync(id, ct);
    }

    // ------------------------------------------------------------------ Lectures

    /// <summary>Détail (US-05, US-09) : périmètre dans la clause WHERE, 404 hors périmètre (ENF-S7).</summary>
    public async Task<LeaveRequestDto> GetAsync(Guid id, CancellationToken ct)
    {
        var me = currentUser.Id;
        var isHr = IsHr;
        var row = await (
                from r in db.LeaveRequests.AsNoTracking()
                join u in db.Users.AsNoTracking() on r.RequesterId equals u.Id
                join t in db.LeaveTypes.AsNoTracking() on r.LeaveTypeId equals t.Id
                where r.Id == id && (r.RequesterId == me || u.ManagerId == me || isHr)
                select new { Request = r, u.ManagerId, Type = t })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException();

        var request = row.Request;
        var history = await db.LeaveRequestHistory.AsNoTracking()
            .Where(h => h.LeaveRequestId == id)
            .OrderBy(h => h.Id)
            .ToListAsync(ct);
        var people = await db.PeopleAsync(
            history.Where(h => h.AuthorId.HasValue).Select(h => h.AuthorId!.Value)
                .Append(request.RequesterId)
                .Concat(new[] { request.DecidedById, request.OverlapAcknowledgedById, request.CancelledById }.Where(x => x.HasValue).Select(x => x!.Value)),
            ct);

        // GAP-24 : analyse seulement pour une demande en attente, pour l'approbateur ou un admin RH, jamais pour ses propres demandes.
        OverlapAnalysisDto? overlap = null;
        if (request.Status == LeaveRequestStatus.Pending && request.RequesterId != me && (row.ManagerId == me || isHr))
        {
            var analysis = await overlapService.AnalyzeAsync(request, row.ManagerId, ct);
            overlap = analysis.ToDto(request.Id, clock.UtcNow);
        }

        return new LeaveRequestDto(
            request.Id,
            people[request.RequesterId],
            Mapping.TypeRef(row.Type),
            request.StartDate,
            request.EndDate,
            request.DaysCount,
            request.RequesterComment,
            request.Status,
            request.SubmittedAtUtc,
            BuildDecision(request, people),
            BuildCancellation(request, people),
            history.Select(h => new HistoryEntryDto(
                h.FromStatus,
                h.ToStatus,
                h.AuthorId is { } a ? people.GetValueOrDefault(a) : null,
                h.OccurredAtUtc,
                h.Comment)).ToList(),
            overlap);
    }

    /// <summary>GET /leave-requests/{id}/overlap : approbateur ou admin RH (RB-19).</summary>
    public async Task<OverlapAnalysisDto> GetOverlapAsync(Guid id, CancellationToken ct)
    {
        var me = currentUser.Id;
        var isHr = IsHr;
        var row = await (
                from r in db.LeaveRequests.AsNoTracking()
                join u in db.Users.AsNoTracking() on r.RequesterId equals u.Id
                where r.Id == id && (r.RequesterId == me || u.ManagerId == me || isHr)
                select new { Request = r, u.ManagerId })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException();
        if (row.Request.RequesterId == me)
        {
            throw new ForbiddenException(ErrorCodes.Forbidden, "L'analyse de chevauchement n'est pas visible par le demandeur (RB-19).");
        }

        var analysis = await overlapService.AnalyzeAsync(row.Request, row.ManagerId, ct);
        return analysis.ToDto(row.Request.Id, clock.UtcNow);
    }

    /// <summary>Listes : scope=mine (US-05), to-decide (US-08), all (US-19).</summary>
    public async Task<PagedResult<LeaveRequestSummaryDto>> ListAsync(LeaveRequestListQuery query, CancellationToken ct)
    {
        var me = currentUser.Id;
        var isHr = IsHr;
        var source =
            from r in db.LeaveRequests.AsNoTracking()
            join u in db.Users.AsNoTracking() on r.RequesterId equals u.Id
            join t in db.LeaveTypes.AsNoTracking() on r.LeaveTypeId equals t.Id
            select new Row { Request = r, FirstName = u.FirstName, LastName = u.LastName, ManagerId = u.ManagerId, Type = t };

        IQueryable<Row> filtered;
        var withAlert = false;
        switch (query.Scope)
        {
            case "mine":
                filtered = ApplyFilters(source.Where(x => x.Request.RequesterId == me), query)
                    .OrderByDescending(x => x.Request.StartDate).ThenByDescending(x => x.Request.SubmittedAtUtc);
                break;
            case "to-decide":
                if (!currentUser.IsInRole(Role.Manager) && !isHr)
                {
                    throw new ForbiddenException(ErrorCodes.Forbidden, "Réservé aux managers et aux admins RH.");
                }

                // P-10 : en attente, pas soi-même, manager direct actuel, ou admin RH pour les employés sans manager.
                filtered = source
                    .Where(x => x.Request.Status == LeaveRequestStatus.Pending
                        && x.Request.RequesterId != me
                        && (x.ManagerId == me || (x.ManagerId == null && isHr)))
                    .OrderBy(x => x.Request.SubmittedAtUtc);
                withAlert = true;
                break;
            case "all":
                if (!isHr)
                {
                    throw new ForbiddenException(ErrorCodes.Forbidden, "Réservé aux admins RH.");
                }

                var all = ApplyFilters(source, query);
                if (query.ManagerId is { } managerId)
                {
                    all = all.Where(x => x.ManagerId == managerId);
                }

                if (query.RequesterId is { } requesterId)
                {
                    all = all.Where(x => x.Request.RequesterId == requesterId);
                }

                filtered = all.OrderByDescending(x => x.Request.StartDate).ThenByDescending(x => x.Request.SubmittedAtUtc);
                withAlert = true;
                break;
            default:
                throw new ValidationException("scope", "scope doit valoir mine, to-decide ou all.");
        }

        var page = await filtered.ToPageAsync(query.Page, query.PageSize, ct);
        var items = new List<LeaveRequestSummaryDto>(page.Items.Count);
        foreach (var x in page.Items)
        {
            OverlapAlertDto? alert = null;
            if (withAlert && x.Request.Status == LeaveRequestStatus.Pending && x.Request.RequesterId != me)
            {
                alert = (await overlapService.AnalyzeAsync(x.Request, x.ManagerId, ct)).ToAlert();
            }

            items.Add(new LeaveRequestSummaryDto(
                x.Request.Id,
                Mapping.Person(x.Request.RequesterId, x.FirstName, x.LastName),
                Mapping.TypeRef(x.Type),
                x.Request.StartDate,
                x.Request.EndDate,
                x.Request.DaysCount,
                x.Request.Status,
                x.Request.SubmittedAtUtc,
                alert));
        }

        return new PagedResult<LeaveRequestSummaryDto>(items, page.Page, page.PageSize, page.TotalCount);
    }

    // ------------------------------------------------------------------ Interne

    private static IQueryable<Row> ApplyFilters(IQueryable<Row> source, LeaveRequestListQuery query)
    {
        if (query.Status is { } status)
        {
            source = source.Where(x => x.Request.Status == status);
        }

        if (query.LeaveTypeId is { } typeId)
        {
            source = source.Where(x => x.Request.LeaveTypeId == typeId);
        }

        if (query.From is { } from)
        {
            source = source.Where(x => x.Request.EndDate >= from);
        }

        if (query.To is { } to)
        {
            source = source.Where(x => x.Request.StartDate <= to);
        }

        return source;
    }

    /// <summary>Chargement pour décision : 404 si inexistante, puis 403 selon l'approbateur actuel (GAP-01, §5.4).</summary>
    private async Task<(LeaveRequest Request, User Requester)> LoadForDecisionAsync(Guid id, CancellationToken ct)
    {
        var request = await db.LeaveRequests.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw new NotFoundException();
        var requester = await db.Users.FirstAsync(u => u.Id == request.RequesterId, ct);
        ApproverPolicy.EnsureCanDecide(currentUser.Id, IsHr, requester.Id, requester.ManagerId);
        return (request, requester);
    }

    private async Task<Evaluation> EvaluateAsync(Guid requesterId, Guid leaveTypeId, DateOnly start, DateOnly end, bool createBalance, CancellationToken ct)
    {
        var period = new DateRange(start, end); // 400 si fin < début (CA-04.3)
        var type = await db.LeaveTypes.FirstOrDefaultAsync(t => t.Id == leaveTypeId, ct)
            ?? throw new NotFoundException("Type de congé introuvable.");
        if (!type.IsActive)
        {
            throw new DomainException(ErrorCodes.LeaveTypeInactive, "Ce type de congé est désactivé.");
        }

        if (period.CalendarDays > DateRange.MaxCalendarDays)
        {
            throw new DomainException(ErrorCodes.PeriodTooLong, $"La période ne peut pas dépasser {DateRange.MaxCalendarDays} jours calendaires.");
        }

        var workingDays = await holidays.WorkingDaysAsync(period, ct);
        if (workingDays.Count == 0)
        {
            throw new DomainException(ErrorCodes.NoWorkingDay, "La période ne contient aucun jour ouvré.");
        }

        var blocking = new List<DomainException>();
        if (start < clock.Today && !type.AllowsRetroactive)
        {
            blocking.Add(new DomainException(ErrorCodes.PastDateNotAllowed, "Ce type de congé n'autorise pas une date de début passée."));
        }

        var overlapsOwn = await db.LeaveRequests.AnyAsync(
            r => r.RequesterId == requesterId
                && (r.Status == LeaveRequestStatus.Pending || r.Status == LeaveRequestStatus.Approved)
                && r.StartDate <= end
                && r.EndDate >= start,
            ct);
        if (overlapsOwn)
        {
            blocking.Add(new DomainException(ErrorCodes.OverlapsOwnRequest, "La période recoupe une de vos demandes en attente ou approuvées."));
        }

        Balance? balance = null;
        var pending = 0;
        if (type.IsTracked)
        {
            balance = createBalance
                ? await balanceService.GetOrCreateAsync(requesterId, type.Id, ct)
                : await db.Balances.AsNoTracking().FirstOrDefaultAsync(b => b.UserId == requesterId && b.LeaveTypeId == type.Id, ct);
            pending = await balanceService.PendingHundredthsAsync(requesterId, type.Id, ct);
            var available = (balance?.AmountHundredths ?? 0) - pending;
            var needed = DayAmount.FromDays(workingDays.Count);
            if (needed > available)
            {
                blocking.Add(new DomainException(
                    ErrorCodes.InsufficientBalance,
                    $"La demande décompte {workingDays.Count} jour(s) ; le disponible {type.Code} est de {Mapping.Days(DayAmount.ToDecimal(available))} jour(s)."));
            }
        }

        return new Evaluation(type, period, workingDays, blocking, balance, pending);
    }

    private static DecisionDto? BuildDecision(LeaveRequest r, IReadOnlyDictionary<Guid, PersonRef> people)
    {
        if (r.DecidedById is not { } deciderId || r.DecidedAtUtc is not { } decidedAt)
        {
            return null;
        }

        OverlapAcknowledgementDto? ack = null;
        if (r.OverlapAcknowledgedById is { } ackBy && r.OverlapAcknowledgedAtUtc is { } ackAt)
        {
            ack = new OverlapAcknowledgementDto(
                people[ackBy],
                ackAt,
                new Rate(r.OverlapAcknowledgedAbsent ?? 0, r.OverlapAcknowledgedTeamSize ?? 0).ToDto());
        }

        var outcome = r.Status == LeaveRequestStatus.Rejected ? LeaveRequestStatus.Rejected : LeaveRequestStatus.Approved;
        return new DecisionDto(outcome, people[deciderId], decidedAt, r.DecisionComment, ack);
    }

    private static CancellationDto? BuildCancellation(LeaveRequest r, IReadOnlyDictionary<Guid, PersonRef> people) =>
        r.CancelledAtUtc is { } at
            ? new CancellationDto(r.CancelledById is { } by ? people[by] : null, r.CancelledById is null, at, r.CancellationReason)
            : null;

    /// <summary>Projection composable par EF (initialiseur de membres, pas de constructeur).</summary>
    private sealed class Row
    {
        public required LeaveRequest Request { get; init; }

        public required string FirstName { get; init; }

        public required string LastName { get; init; }

        public Guid? ManagerId { get; init; }

        public required LeaveType Type { get; init; }
    }

    private sealed record Evaluation(
        LeaveType Type,
        DateRange Period,
        IReadOnlyList<DateOnly> WorkingDays,
        List<DomainException> BlockingReasons,
        Balance? Balance,
        int PendingHundredths);
}
