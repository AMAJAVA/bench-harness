using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Exceptions;
using TeamLeave.Application.Interfaces;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;
using TeamLeave.Domain.Services;

namespace TeamLeave.Application.Services;

public class LeaveRequestService
{
    private readonly ITeamLeaveDbContext _db;
    private readonly IClock _clock;

    public LeaveRequestService(ITeamLeaveDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<LeaveRequestDto> SubmitAsync(Guid userId, CreateLeaveRequestRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Include(u => u.Team)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new AppException(404, "USER_NOT_FOUND", "Utilisateur introuvable.");

        if (!user.IsActive) throw new AppException(422, "USER_INACTIVE", "L'utilisateur est inactif.");
        if (user.TeamId == null) throw new AppException(422, "NO_TEAM", "L'utilisateur n'est rattaché à aucune équipe.");
        if (user.Team == null || user.Team.ManagerId == Guid.Empty) throw new AppException(422, "NO_TEAM_MANAGER", "L'équipe n'a pas de manager.");

        var leaveType = await _db.LeaveTypes.FindAsync(new object?[] { request.LeaveTypeId }, cancellationToken)
            ?? throw new AppException(400, "LEAVE_TYPE_NOT_FOUND", "Type de congé introuvable.");
        if (!leaveType.IsActive) throw new AppException(400, "LEAVE_TYPE_INACTIVE", "Le type de congé est inactif.");

        if (request.StartDate > request.EndDate)
            throw new AppException(400, "INVALID_DATES", "La date de début doit être antérieure ou égale à la date de fin.");
        if (request.StartDate.Year != request.EndDate.Year)
            throw new AppException(400, "CROSS_YEAR", "Les demandes à cheval sur deux années civiles sont interdites.");

        var holidays = await _db.Holidays.Where(h => h.IsActive).ToListAsync(cancellationToken);
        var workingDays = WorkingDaysCalculator.Calculate(request.StartDate, request.EndDate, holidays);
        if (workingDays == 0)
            throw new AppException(422, "NO_WORKING_DAYS", "La demande ne contient aucun jour ouvré.");

        var year = request.StartDate.Year;
        var balance = await _db.LeaveBalances
            .SingleOrDefaultAsync(b => b.UserId == userId && b.LeaveTypeId == leaveType.Id && b.Year == year, cancellationToken);

        decimal acquired = balance?.AcquiredDays ?? 0;
        decimal adjustment = balance?.AdjustmentDays ?? 0;
        decimal used = balance?.UsedDays ?? 0;

        if (!BalanceCalculator.IsSufficient(acquired, adjustment, used, workingDays))
        {
            var available = BalanceCalculator.Available(acquired, adjustment, used);
            throw new AppException(422, "INSUFFICIENT_BALANCE", $"La durée demandée ({workingDays} jours) dépasse le solde disponible ({available} jours).");
        }

        var approvedRequests = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == LeaveRequestStatus.Approved && r.TeamId == user.TeamId)
            .ToListAsync(cancellationToken);

        var overlap = OverlapCalculator.Calculate(
            request.StartDate,
            request.EndDate,
            userId,
            user.TeamId!.Value,
            approvedRequests,
            holidays);

        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = userId,
            TeamId = user.TeamId.Value,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            WorkingDays = workingDays,
            Status = LeaveRequestStatus.Pending,
            EmployeeComment = request.Comment,
            SubmittedAt = _clock.UtcNow,
            HasOverlapWarning = overlap.HasWarning,
            OverlapRatio = overlap.Ratio,
            Version = 0
        };

        _db.LeaveRequests.Add(leaveRequest);

        var manager = await _db.Users.FindAsync(new object?[] { user.Team.ManagerId }, cancellationToken);
        if (manager != null)
        {
            _db.EmailOutbox.Add(new EmailOutbox
            {
                Id = Guid.NewGuid(),
                Recipient = manager.Email,
                Template = "LeaveRequestSubmitted",
                Payload = $"Employee={user.FirstName} {user.LastName};Type={leaveType.Code};Start={request.StartDate};End={request.EndDate};HasOverlapWarning={overlap.HasWarning};OverlapRatio={overlap.Ratio}",
                Status = EmailStatus.Pending,
                Attempts = 0,
                CreatedAt = _clock.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(leaveRequest, approvedRequests, holidays);
    }

    public async Task<LeaveRequestDto> GetByIdAsync(Guid id, Guid currentUserId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default)
    {
        var request = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.Team)
            .Include(r => r.LeaveType)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new AppException(404, "LEAVE_REQUEST_NOT_FOUND", "Demande introuvable.");

        var isAdmin = roles.Contains(RoleNames.AdminRh);
        var isOwner = request.EmployeeId == currentUserId;
        var isManagerOfTeam = roles.Contains(RoleNames.Manager) && request.Team.ManagerId == currentUserId;

        if (!isAdmin && !isOwner && !isManagerOfTeam)
            throw new AppException(403, "FORBIDDEN", "Vous n'avez pas accès à cette demande.");

        var holidays = await _db.Holidays.Where(h => h.IsActive).ToListAsync(cancellationToken);
        var approvedRequests = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == LeaveRequestStatus.Approved && r.TeamId == request.TeamId)
            .ToListAsync(cancellationToken);

        return MapToDto(request, approvedRequests, holidays);
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetMyRequestsAsync(
        Guid userId,
        LeaveRequestStatus? status,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var query = _db.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.Team)
            .Include(r => r.LeaveType)
            .Where(r => r.EmployeeId == userId);

        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (from.HasValue) query = query.Where(r => r.EndDate >= from.Value);
        if (to.HasValue) query = query.Where(r => r.StartDate <= to.Value);

        var requests = await query.OrderByDescending(r => r.SubmittedAt).ToListAsync(cancellationToken);
        var holidays = await _db.Holidays.Where(h => h.IsActive).ToListAsync(cancellationToken);
        var approvedRequests = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == LeaveRequestStatus.Approved)
            .ToListAsync(cancellationToken);

        return requests
            .Select(r => MapToDto(r, approvedRequests.Where(ar => ar.TeamId == r.TeamId).ToList(), holidays))
            .ToList();
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetTeamPendingRequestsAsync(Guid teamId, Guid managerId, CancellationToken cancellationToken = default)
    {
        var team = await _db.Teams.FindAsync(new object?[] { teamId }, cancellationToken)
            ?? throw new AppException(404, "TEAM_NOT_FOUND", "Équipe introuvable.");
        if (team.ManagerId != managerId)
            throw new AppException(403, "NOT_TEAM_MANAGER", "Vous n'êtes pas le manager de cette équipe.");

        var requests = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.Team)
            .Include(r => r.LeaveType)
            .Where(r => r.TeamId == teamId && r.Status == LeaveRequestStatus.Pending)
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);

        var holidays = await _db.Holidays.Where(h => h.IsActive).ToListAsync(cancellationToken);
        var approvedRequests = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == LeaveRequestStatus.Approved && r.TeamId == teamId)
            .ToListAsync(cancellationToken);

        return requests.Select(r => MapToDto(r, approvedRequests, holidays)).ToList();
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetTeamCalendarAsync(
        Guid teamId,
        Guid currentUserId,
        IReadOnlyList<string> roles,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var team = await _db.Teams.FindAsync(new object?[] { teamId }, cancellationToken)
            ?? throw new AppException(404, "TEAM_NOT_FOUND", "Équipe introuvable.");

        var isAdmin = roles.Contains(RoleNames.AdminRh);
        var isManager = roles.Contains(RoleNames.Manager) && team.ManagerId == currentUserId;
        var isMember = await _db.Users.AnyAsync(u => u.Id == currentUserId && u.TeamId == teamId, cancellationToken);

        if (!isAdmin && !isManager && !isMember)
            throw new AppException(403, "FORBIDDEN", "Vous n'avez pas accès au calendrier de cette équipe.");

        var requests = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.Team)
            .Include(r => r.LeaveType)
            .Where(r => r.TeamId == teamId
                        && r.Status == LeaveRequestStatus.Approved
                        && r.StartDate <= to
                        && r.EndDate >= from)
            .OrderBy(r => r.StartDate)
            .ToListAsync(cancellationToken);

        var holidays = await _db.Holidays.Where(h => h.IsActive).ToListAsync(cancellationToken);
        var approvedRequests = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == LeaveRequestStatus.Approved && r.TeamId == teamId)
            .ToListAsync(cancellationToken);

        return requests.Select(r => MapToDto(r, approvedRequests, holidays)).ToList();
    }

    public async Task<LeaveRequestDto> DecideAsync(
        Guid requestId,
        Guid managerId,
        IReadOnlyList<string> roles,
        DecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.Team)
            .Include(r => r.LeaveType)
            .SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new AppException(404, "LEAVE_REQUEST_NOT_FOUND", "Demande introuvable.");

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
            throw new AppException(409, "ALREADY_DECIDED", $"La demande est déjà au statut {leaveRequest.Status}.");

        if (leaveRequest.Team.ManagerId != managerId)
            throw new AppException(403, "NOT_TEAM_MANAGER", "Vous n'êtes pas le manager de cette équipe.");

        if (leaveRequest.EmployeeId == managerId)
            throw new AppException(403, "SELF_APPROVAL", "Vous ne pouvez pas décider de votre propre demande.");

        var action = request.Action.Trim().ToUpperInvariant();
        if (action != "APPROVE" && action != "REJECT")
            throw new AppException(400, "INVALID_ACTION", "L'action doit être APPROVE ou REJECT.");

        if (action == "REJECT" && string.IsNullOrWhiteSpace(request.Comment))
            throw new AppException(400, "COMMENT_REQUIRED", "Un commentaire est obligatoire pour un refus.");

        var holidays = await _db.Holidays.Where(h => h.IsActive).ToListAsync(cancellationToken);
        var approvedRequests = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == LeaveRequestStatus.Approved && r.TeamId == leaveRequest.TeamId)
            .ToListAsync(cancellationToken);

        var overlap = OverlapCalculator.Calculate(
            leaveRequest.StartDate,
            leaveRequest.EndDate,
            leaveRequest.EmployeeId,
            leaveRequest.TeamId,
            approvedRequests,
            holidays);

        if (action == "APPROVE")
        {
            if (overlap.HasWarning && !request.AcknowledgeOverlapWarning)
                throw new AppException(409, "OVERLAP_WARNING_NOT_ACKNOWLEDGED", "Un avertissement de chevauchement critique est actif. Vous devez l'acquitter explicitement pour approuver.");

            var year = leaveRequest.StartDate.Year;
            var balance = await _db.LeaveBalances
                .SingleOrDefaultAsync(b => b.UserId == leaveRequest.EmployeeId && b.LeaveTypeId == leaveRequest.LeaveTypeId && b.Year == year, cancellationToken);

            decimal acquired = balance?.AcquiredDays ?? 0;
            decimal adjustment = balance?.AdjustmentDays ?? 0;
            decimal used = balance?.UsedDays ?? 0;

            if (!BalanceCalculator.IsSufficient(acquired, adjustment, used, leaveRequest.WorkingDays))
            {
                var available = BalanceCalculator.Available(acquired, adjustment, used);
                throw new AppException(409, "INSUFFICIENT_BALANCE_AT_APPROVAL", $"Le solde est devenu insuffisant (disponible: {available}, demandé: {leaveRequest.WorkingDays}).");
            }

            if (balance == null)
            {
                balance = new LeaveBalance
                {
                    Id = Guid.NewGuid(),
                    UserId = leaveRequest.EmployeeId,
                    LeaveTypeId = leaveRequest.LeaveTypeId,
                    Year = year,
                    AcquiredDays = 0,
                    AdjustmentDays = 0,
                    UsedDays = 0,
                    Version = 0
                };
                _db.LeaveBalances.Add(balance);
            }

            balance.UsedDays += leaveRequest.WorkingDays;
            balance.Version++;

            leaveRequest.Status = LeaveRequestStatus.Approved;
            leaveRequest.HasOverlapWarning = overlap.HasWarning;
            leaveRequest.OverlapRatio = overlap.Ratio;

            if (request.AcknowledgeOverlapWarning)
            {
                leaveRequest.AcknowledgedOverlapBy = managerId;
                leaveRequest.AcknowledgedOverlapAt = _clock.UtcNow;
            }
        }
        else
        {
            leaveRequest.Status = LeaveRequestStatus.Rejected;
            leaveRequest.HasOverlapWarning = overlap.HasWarning;
            leaveRequest.OverlapRatio = overlap.Ratio;
        }

        leaveRequest.DeciderId = managerId;
        leaveRequest.DecidedAt = _clock.UtcNow;
        leaveRequest.ManagerComment = request.Comment;
        leaveRequest.Version++;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = managerId,
            Action = action == "APPROVE" ? "LEAVE_REQUEST_APPROVED" : "LEAVE_REQUEST_REJECTED",
            EntityType = nameof(LeaveRequest),
            EntityId = leaveRequest.Id,
            OccurredAt = _clock.UtcNow,
            Details = $"Status={leaveRequest.Status};Comment={request.Comment};HasOverlapWarning={overlap.HasWarning};OverlapRatio={overlap.Ratio};Acknowledged={request.AcknowledgeOverlapWarning}",
            Reason = request.Comment
        });

        _db.EmailOutbox.Add(new EmailOutbox
        {
            Id = Guid.NewGuid(),
            Recipient = leaveRequest.Employee.Email,
            Template = action == "APPROVE" ? "LeaveRequestApproved" : "LeaveRequestRejected",
            Payload = $"LeaveRequestId={leaveRequest.Id};Status={leaveRequest.Status};ManagerComment={request.Comment}",
            Status = EmailStatus.Pending,
            Attempts = 0,
            CreatedAt = _clock.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException(409, "CONCURRENCY_CONFLICT", "La demande a été modifiée entre-temps. Veuillez recharger et réessayer.");
        }

        return MapToDto(leaveRequest, approvedRequests, holidays);
    }

    private static LeaveRequestDto MapToDto(
        LeaveRequest request,
        IReadOnlyList<LeaveRequest> approvedRequests,
        IReadOnlyList<Holiday> holidays)
    {
        var overlap = OverlapCalculator.Calculate(
            request.StartDate,
            request.EndDate,
            request.EmployeeId,
            request.TeamId,
            approvedRequests,
            holidays);

        return new LeaveRequestDto(
            request.Id,
            request.EmployeeId,
            request.TeamId,
            new LeaveTypeInfoDto(request.LeaveType.Id, request.LeaveType.Code, request.LeaveType.Label),
            request.StartDate,
            request.EndDate,
            request.WorkingDays,
            request.Status.ToString().ToUpperInvariant(),
            request.EmployeeComment,
            request.SubmittedAt,
            request.DeciderId,
            request.DecidedAt,
            request.ManagerComment,
            overlap.HasWarning,
            overlap.Ratio,
            overlap.Details.Select(d => new OverlapDetailDto(d.Date, d.AbsentEmployees)).ToList(),
            request.AcknowledgedOverlapBy,
            request.AcknowledgedOverlapAt);
    }
}
