using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;
using TeamLeave.Domain.Exceptions;
using TeamLeave.Domain.Services;
using TeamLeave.Domain.ValueObjects;
using TeamLeave.Infrastructure.Audit;
using TeamLeave.Infrastructure.Mapping;
using TeamLeave.Infrastructure.Persistence;

namespace TeamLeave.Infrastructure.Services;

public sealed class LeaveRequestService(
    TeamLeaveDbContext db,
    IClock clock,
    BalanceQueryService balanceQuery,
    OverlapService overlapService,
    AuditWriter auditWriter) : ILeaveRequestService
{
    public async Task<Result<LeaveRequestDetail>> SubmitAsync(
        Guid employeeId,
        SubmitLeaveRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ContractMapper.TryParseHalfDay(request.StartHalfDay, out var startHalfDay) ||
            !ContractMapper.TryParseHalfDay(request.EndHalfDay, out var endHalfDay))
        {
            return Result<LeaveRequestDetail>.Fail(
                ErrorCodes.INVALID_HALF_DAY,
                "Demi-journée invalide",
                422,
                "Les demi-journées doivent valoir AM ou PM.");
        }

        if (request.Comment is { Length: > 1000 })
        {
            return Result<LeaveRequestDetail>.Fail(
                ErrorCodes.VALIDATION_ERROR,
                "Validation invalide",
                422,
                "Le commentaire ne peut pas dépasser 1 000 caractères.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await AcquireEmployeeLockAsync(employeeId, cancellationToken);

        var employee = await db.Users
            .Include(u => u.Manager)
            .SingleOrDefaultAsync(u => u.Id == employeeId, cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            return Result<LeaveRequestDetail>.Fail(
                ErrorCodes.NOT_FOUND,
                "Ressource introuvable",
                404,
                "Utilisateur introuvable.");
        }

        if (employee.Manager is null || !employee.Manager.IsActive || employee.ManagerId == employee.Id)
        {
            return Result<LeaveRequestDetail>.Fail(
                ErrorCodes.NO_APPROVER,
                "Aucun manager",
                409,
                "Aucun manager direct actif n'est désigné pour ce compte.");
        }

        var leaveType = await db.LeaveTypes
            .SingleOrDefaultAsync(t => t.Id == request.LeaveTypeId, cancellationToken);

        if (leaveType is null || !leaveType.IsActive)
        {
            return Result<LeaveRequestDetail>.Fail(
                ErrorCodes.INACTIVE_LEAVE_TYPE,
                "Type de congé inactif",
                422,
                "Le type de congé demandé n'existe pas ou n'est plus actif.");
        }

        if (request.StartDate < clock.TodayInParis)
        {
            return Result<LeaveRequestDetail>.Fail(
                ErrorCodes.INVALID_DATES,
                "Dates invalides",
                422,
                "La date de début doit être aujourd'hui ou une date future.");
        }

        var duration = LeaveDurationCalculator.Calculate(
            request.StartDate,
            startHalfDay,
            request.EndDate,
            endHalfDay);

        if (!duration.IsValid)
        {
            return Result<LeaveRequestDetail>.Fail(
                ErrorCodes.INVALID_DATES,
                "Dates invalides",
                422,
                duration.ErrorDetail ?? "Les dates sont invalides.");
        }

        var allocations = LeaveYearSplitter.Split(duration.Slots);
        var requestedByYear = allocations.ToDictionary(x => x.Key, x => x.Value);

        var available = await balanceQuery.GetAvailableHalfDaysAsync(
            employeeId,
            leaveType.Id,
            requestedByYear.Keys.ToList(),
            cancellationToken);

        var shortages = BalancePolicy.CheckSubmission(requestedByYear, available);
        if (shortages.Count > 0)
        {
            var details = shortages.Select(s => new
            {
                leaveTypeId = leaveType.Id,
                code = leaveType.Code,
                year = s.Year,
                requestedDays = s.RequestedHalfDays / 2m,
                availableDays = s.AvailableHalfDays / 2m,
                missingDays = s.MissingHalfDays / 2m
            }).ToList();

            var error = ErrorFactory.Unprocessable(
                    ErrorCodes.INSUFFICIENT_BALANCE,
                    "Solde insuffisant",
                    "Le solde disponible est insuffisant pour au moins une des années concernées.")
                .With("balanceShortages", details);

            return Result<LeaveRequestDetail>.Fail(error);
        }

        var activeRequests = await db.LeaveRequests
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId &&
                        (r.Status == LeaveStatus.PENDING || r.Status == LeaveStatus.APPROVED) &&
                        r.StartDate <= request.EndDate &&
                        r.EndDate >= request.StartDate)
            .Select(r => new
            {
                r.Id,
                r.StartDate,
                r.StartHalfDay,
                r.EndDate,
                r.EndHalfDay
            })
            .ToListAsync(cancellationToken);

        foreach (var existing in activeRequests)
        {
            var existingSlots = LeaveDurationCalculator.EnumerateSlots(
                new LeaveHalfDay(existing.StartDate, existing.StartHalfDay).Ordinal,
                new LeaveHalfDay(existing.EndDate, existing.EndHalfDay).Ordinal);

            var conflict = existingSlots
                .Intersect(duration.Slots)
                .OrderBy(s => s)
                .FirstOrDefault();

            if (duration.Slots.Contains(conflict) && conflict != default)
            {
                var moment = conflict.HalfDay == HalfDay.AM ? "matin" : "après-midi";
                var error = ErrorFactory.Conflict(
                        ErrorCodes.PERSONAL_OVERLAP,
                        "Chevauchement personnel",
                        $"Une demande active couvre déjà le {conflict.Date:dd/MM/yyyy} ({moment}).")
                    .With("conflict", new
                    {
                        date = conflict.Date.ToString("yyyy-MM-dd"),
                        halfDay = ContractMapper.FormatHalfDay(conflict.HalfDay)
                    });

                return Result<LeaveRequestDetail>.Fail(error);
            }
        }

        var now = clock.UtcNow;
        var entity = new LeaveRequest
        {
            EmployeeId = employee.Id,
            Employee = employee,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = request.StartDate,
            StartHalfDay = startHalfDay,
            EndDate = request.EndDate,
            EndHalfDay = endHalfDay,
            TotalHalfDays = duration.HalfDays,
            Status = LeaveStatus.PENDING,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            Allocations = allocations
                .Select(a => new LeaveRequestAllocation
                {
                    LeaveRequestId = Guid.Empty,
                    Year = a.Key,
                    HalfDays = a.Value
                })
                .ToList()
        };

        foreach (var allocation in entity.Allocations)
        {
            allocation.LeaveRequestId = entity.Id;
        }

        db.LeaveRequests.Add(entity);

        var overlap = await overlapService.EvaluateAsync(
            entity,
            OverlapTriggerKind.SUBMISSION,
            employee.Id,
            persist: true,
            cancellationToken);

        entity.RecordOverlap(overlap.Result.Active, overlap.EvaluatedAt);

        db.Notifications.Add(new Notification
        {
            EventKey = $"SUBMISSION:{entity.Id}",
            Type = NotificationType.SOUMISSION,
            LeaveRequestId = entity.Id,
            RecipientUserId = employee.Manager.Id,
            RecipientEmail = employee.Manager.Email,
            Status = NotificationStatus.PENDING,
            NextAttemptAt = now,
            CreatedAt = now
        });

        auditWriter.Add(
            employee.Id,
            "LEAVE_REQUEST_SUBMITTED",
            "LEAVE_REQUEST",
            entity.Id,
            after: new
            {
                employeeId = employee.Id,
                leaveTypeId = leaveType.Id,
                startDate = entity.StartDate,
                endDate = entity.EndDate,
                totalHalfDays = entity.TotalHalfDays,
                status = entity.Status.ToString()
            });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (DbErrorHelper.IsExclusionViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<LeaveRequestDetail>.Fail(
                ErrorFactory.Conflict(
                    ErrorCodes.PERSONAL_OVERLAP,
                    "Chevauchement personnel",
                    "Une demande active couvre déjà une des demi-journées demandées."));
        }
        catch (DbUpdateException exception) when (DbErrorHelper.IsUniqueViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<LeaveRequestDetail>.Fail(
                ErrorFactory.Conflict(
                    ErrorCodes.PERSONAL_OVERLAP,
                    "Chevauchement personnel",
                    "Une demande active couvre déjà une des demi-journées demandées."));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return Result<LeaveRequestDetail>.Ok(ContractMapper.ToDetail(entity));
    }

    public async Task<Result<PagedResult<LeaveRequestListItem>>> ListMineAsync(
        Guid employeeId,
        string? status,
        int? year,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paging = Pagination.Normalize(page, pageSize);
        if (!paging.IsSuccess)
        {
            return Result<PagedResult<LeaveRequestListItem>>.Fail(paging.Error!);
        }

        if (year is not null && (year < 2000 || year > 2100))
        {
            return Result<PagedResult<LeaveRequestListItem>>.Fail(
                ErrorFactory.Validation("L'année doit être comprise entre 2000 et 2100."));
        }

        LeaveStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!ContractMapper.TryParseStatus(status, out var parsed))
            {
                return Result<PagedResult<LeaveRequestListItem>>.Fail(
                    ErrorFactory.Validation("Le statut demandé est invalide."));
            }

            statusFilter = parsed;
        }

        var query = db.LeaveRequests
            .AsNoTracking()
            .Include(r => r.LeaveType)
            .Where(r => r.EmployeeId == employeeId);

        if (statusFilter is not null)
        {
            query = query.Where(r => r.Status == statusFilter);
        }

        if (year is not null)
        {
            var yearValue = year.Value;
            query = query.Where(r => r.StartDate.Year == yearValue || r.EndDate.Year == yearValue);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.SubmittedAt)
            .ThenByDescending(r => r.Id)
            .Skip(paging.Value!.Skip)
            .Take(paging.Value.PageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<LeaveRequestListItem>>.Ok(new PagedResult<LeaveRequestListItem>(
            items.Select(r => ContractMapper.ToListItem(r, includeOverlapFlag: false)).ToList(),
            paging.Value.Page,
            paging.Value.PageSize,
            total));
    }

    public async Task<Result<LeaveRequestDetail>> GetMineAsync(
        Guid employeeId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadDetailQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == requestId && r.EmployeeId == employeeId, cancellationToken);

        if (request is null)
        {
            return Result<LeaveRequestDetail>.Fail(ErrorFactory.NotFound("Demande introuvable."));
        }

        return Result<LeaveRequestDetail>.Ok(ContractMapper.ToDetail(request));
    }

    public async Task<Result<LeaveRequestDetail>> CancelAsync(
        Guid employeeId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await LockRequestAsync(requestId, cancellationToken);

        var request = await LoadDetailQuery()
            .SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (request is null || request.EmployeeId != employeeId)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<LeaveRequestDetail>.Fail(ErrorFactory.NotFound("Demande introuvable."));
        }

        var outcome = CancellationPolicy.Evaluate(
            request.EmployeeId,
            employeeId,
            request.Status,
            request.StartDate,
            clock.TodayInParis);

        if (!outcome.Allowed)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<LeaveRequestDetail>.Fail(outcome.Code == ErrorCodes.NOT_FOUND
                ? ErrorFactory.NotFound("Demande introuvable.")
                : ErrorFactory.InvalidState());
        }

        var now = clock.UtcNow;

        if (outcome.RestoreBalance)
        {
            foreach (var allocation in request.Allocations.OrderBy(a => a.Year))
            {
                var balance = await LockBalanceAsync(
                    request.EmployeeId,
                    request.LeaveTypeId,
                    allocation.Year,
                    cancellationToken);

                if (balance is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<LeaveRequestDetail>.Fail(ErrorFactory.InvalidState("Solde introuvable pour la restitution."));
                }

                balance.Restore(allocation.HalfDays);
                balance.UpdatedAt = now;

                db.BalanceMovements.Add(new BalanceMovement
                {
                    EmployeeId = request.EmployeeId,
                    LeaveTypeId = request.LeaveTypeId,
                    Year = allocation.Year,
                    DeltaHalfDays = allocation.HalfDays,
                    Nature = BalanceMovementNature.RESTITUTION_ANNULATION,
                    LeaveRequestId = request.Id,
                    ActorId = employeeId,
                    OccurredAt = now
                });
            }
        }

        request.Cancel(employeeId, now, CancellationReasons.EMPLOYEE_CANCELLATION);
        request.CancelledBy = await db.Users.SingleAsync(u => u.Id == employeeId, cancellationToken);

        auditWriter.Add(
            employeeId,
            "LEAVE_REQUEST_CANCELLED",
            "LEAVE_REQUEST",
            request.Id,
            before: new { status = outcome.RestoreBalance ? LeaveStatus.APPROVED.ToString() : LeaveStatus.PENDING.ToString() },
            after: new { status = LeaveStatus.CANCELLED.ToString() });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<LeaveRequestDetail>.Ok(ContractMapper.ToDetail(request));
    }

    public async Task<Result<BalancesResponse>> GetMyBalancesAsync(
        Guid employeeId,
        int? year,
        CancellationToken cancellationToken = default)
    {
        var targetYear = year ?? clock.TodayInParis.Year;
        if (targetYear < 2000 || targetYear > 2100)
        {
            return Result<BalancesResponse>.Fail(
                ErrorFactory.Validation("L'année doit être comprise entre 2000 et 2100."));
        }

        var exists = await db.Users.AnyAsync(u => u.Id == employeeId, cancellationToken);
        if (!exists)
        {
            return Result<BalancesResponse>.Fail(ErrorFactory.NotFound("Utilisateur introuvable."));
        }

        var balances = await balanceQuery.GetAsync(employeeId, targetYear, false, cancellationToken);
        return Result<BalancesResponse>.Ok(balances);
    }

    private IQueryable<LeaveRequest> LoadDetailQuery() =>
        db.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.LeaveType)
            .Include(r => r.Allocations)
            .Include(r => r.DecidedBy)
            .Include(r => r.CancelledBy)
            .Include(r => r.AcknowledgedBy);

    private async Task LockRequestAsync(Guid requestId, CancellationToken cancellationToken)
    {
        if (db.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            return;
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM leave_requests WHERE id = {requestId} FOR UPDATE",
            cancellationToken);
    }

    private async Task AcquireEmployeeLockAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        if (db.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            return;
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended('employee:' || {employeeId.ToString()}, 0))",
            cancellationToken);
    }

    private async Task<AnnualBalance?> LockBalanceAsync(
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        CancellationToken cancellationToken)
    {
        if (db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            return await db.AnnualBalances
                .FromSqlInterpolated(
                    $"SELECT * FROM annual_balances WHERE employee_id = {employeeId} AND leave_type_id = {leaveTypeId} AND year = {year} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
        }

        return await db.AnnualBalances.SingleOrDefaultAsync(
            b => b.EmployeeId == employeeId &&
                 b.LeaveTypeId == leaveTypeId &&
                 b.Year == year,
            cancellationToken);
    }
}
