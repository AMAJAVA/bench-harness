using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;
using TeamLeave.Domain.Services;
using TeamLeave.Infrastructure.Audit;
using TeamLeave.Infrastructure.Mapping;
using TeamLeave.Infrastructure.Persistence;

namespace TeamLeave.Infrastructure.Services;

public sealed class ManagerService(
    TeamLeaveDbContext db,
    IClock clock,
    BalanceQueryService balanceQuery,
    OverlapService overlapService,
    AuditWriter auditWriter) : IManagerService
{
    public async Task<Result<PagedResult<LeaveRequestListItem>>> ListQueueAsync(
        Guid managerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paging = Pagination.Normalize(page, pageSize);
        if (!paging.IsSuccess)
        {
            return Result<PagedResult<LeaveRequestListItem>>.Fail(paging.Error!);
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

        var reports = db.Users
            .Where(u => u.ManagerId == managerId && u.IsActive && u.Id != managerId)
            .Select(u => u.Id);

        var query = db.LeaveRequests
            .AsNoTracking()
            .Include(r => r.LeaveType)
            .Where(r => reports.Contains(r.EmployeeId));

        if (statusFilter is not null)
        {
            query = query.Where(r => r.Status == statusFilter);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.SubmittedAt)
            .ThenByDescending(r => r.Id)
            .Skip(paging.Value!.Skip)
            .Take(paging.Value.PageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<LeaveRequestListItem>>.Ok(new PagedResult<LeaveRequestListItem>(
            items.Select(r => ContractMapper.ToListItem(r)).ToList(),
            paging.Value.Page,
            paging.Value.PageSize,
            total));
    }

    public async Task<Result<LeaveRequestDetail>> GetForDecisionAsync(
        Guid managerId,
        Guid requestId,
        IReadOnlyList<string> callerRoles,
        CancellationToken cancellationToken = default)
    {
        var request = await LoadDetailQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (request is null)
        {
            return Result<LeaveRequestDetail>.Fail(ErrorFactory.NotFound("Demande introuvable."));
        }

        var authority = CheckAuthority(request, managerId, callerRoles);
        if (authority is not null)
        {
            return Result<LeaveRequestDetail>.Fail(authority);
        }

        var overlap = await overlapService.EvaluateAsync(
            request,
            OverlapTriggerKind.CONSULTATION,
            managerId,
            persist: false,
            cancellationToken);

        AcknowledgementDto? acknowledgement = null;
        if (request.AcknowledgedBy is not null && request.AcknowledgedAt is not null)
        {
            acknowledgement = new AcknowledgementDto(
                ContractMapper.Person(request.AcknowledgedBy),
                request.AcknowledgedAt.Value);
        }

        var overlapDto = ContractMapper.ToOverlapDto(
            overlap.Result,
            overlap.TeamId,
            overlap.EvaluatedAt,
            acknowledgement);

        EmployeeBalanceDto? employeeBalance = null;
        var firstAllocation = request.Allocations.OrderBy(a => a.Year).FirstOrDefault();
        if (firstAllocation is not null)
        {
            var balance = await db.AnnualBalances.AsNoTracking().SingleOrDefaultAsync(
                b => b.EmployeeId == request.EmployeeId &&
                     b.LeaveTypeId == request.LeaveTypeId &&
                     b.Year == firstAllocation.Year,
                cancellationToken);

            employeeBalance = balance is null
                ? new EmployeeBalanceDto(firstAllocation.Year, request.LeaveTypeId, 0, 0, 0)
                : ContractMapper.ToEmployeeBalance(balance);
        }

        var detail = ContractMapper.ToDetail(request, overlapDto, employeeBalance);
        return Result<LeaveRequestDetail>.Ok(detail);
    }

    public async Task<Result<PagedResult<ReportDto>>> ListReportsAsync(
        Guid managerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paging = Pagination.Normalize(page, pageSize);
        if (!paging.IsSuccess)
        {
            return Result<PagedResult<ReportDto>>.Fail(paging.Error!);
        }

        var query = db.Users
            .AsNoTracking()
            .Include(u => u.Team)
            .Where(u => u.ManagerId == managerId && u.IsActive && u.Id != managerId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ThenBy(u => u.Id)
            .Skip(paging.Value!.Skip)
            .Take(paging.Value.PageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<ReportDto>>.Ok(new PagedResult<ReportDto>(
            items.Select(u => new ReportDto(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.Team is null ? null : new TeamSummary(u.Team.Id, u.Team.Name),
                u.IsActive)).ToList(),
            paging.Value.Page,
            paging.Value.PageSize,
            total));
    }

    public async Task<Result<BalancesResponse>> GetReportBalancesAsync(
        Guid managerId,
        Guid employeeId,
        int? year,
        CancellationToken cancellationToken = default)
    {
        var employee = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == employeeId, cancellationToken);

        if (employee is null || employee.ManagerId != managerId)
        {
            return Result<BalancesResponse>.Fail(ErrorFactory.NotFound("Employé introuvable dans votre périmètre."));
        }

        var targetYear = year ?? clock.TodayInParis.Year;
        if (targetYear < 2000 || targetYear > 2100)
        {
            return Result<BalancesResponse>.Fail(
                ErrorFactory.Validation("L'année doit être comprise entre 2000 et 2100."));
        }

        var balances = await balanceQuery.GetAsync(employeeId, targetYear, false, cancellationToken);
        return Result<BalancesResponse>.Ok(balances);
    }

    public async Task<Result<LeaveRequestDetail>> ApproveAsync(
        Guid managerId,
        Guid requestId,
        ApproveLeaveRequestRequest request,
        IReadOnlyList<string> callerRoles,
        CancellationToken cancellationToken = default)
    {
        if (request.Comment is { Length: > 1000 })
        {
            return Result<LeaveRequestDetail>.Fail(
                ErrorFactory.Validation("Le commentaire ne peut pas dépasser 1 000 caractères."));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var preloaded = await db.LeaveRequests
            .AsNoTracking()
            .Include(r => r.Employee)
            .SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (preloaded?.Employee is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<LeaveRequestDetail>.Fail(ErrorFactory.NotFound("Demande introuvable."));
        }

        await AcquireAdvisoryLocksAsync(
            preloaded.EmployeeId,
            preloaded.Employee.TeamId,
            cancellationToken);

        await LockRequestAsync(requestId, cancellationToken);

        var entity = await LoadDetailQuery()
            .SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (entity is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<LeaveRequestDetail>.Fail(ErrorFactory.NotFound("Demande introuvable."));
        }

        var authority = CheckAuthority(entity, managerId, callerRoles);
        if (authority is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<LeaveRequestDetail>.Fail(authority);
        }

        var overlap = await overlapService.EvaluateAsync(
            entity,
            OverlapTriggerKind.DECISION,
            managerId,
            persist: false,
            cancellationToken);

        var decision = DecisionPolicy.Evaluate(
            entity.EmployeeId,
            managerId,
            entity.Employee?.ManagerId,
            callerRoles.Contains(RoleNames.MANAGER, StringComparer.Ordinal),
            callerRoles.Contains(RoleNames.ADMIN_RH, StringComparer.Ordinal),
            entity.Status,
            overlap.Result.Active,
            request.OverlapAcknowledged,
            isRejection: false,
            request.Comment);

        if (!decision.Allowed)
        {
            var error = ErrorFactory.Conflict(
                decision.Code!,
                decision.Code == ErrorCodes.OVERLAP_ACK_REQUIRED ? "Acquittement requis" : "Transition invalide",
                decision.Code == ErrorCodes.OVERLAP_ACK_REQUIRED
                    ? "Le signal de chevauchement est actif ; l'approbation exige un acquittement explicite."
                    : "Cette demande ne peut pas être approuvée dans son état courant.");

            if (decision.Code == ErrorCodes.OVERLAP_ACK_REQUIRED)
            {
                error = error.With("overlap", new
                {
                    applicable = overlap.Result.Applicable,
                    active = overlap.Result.Active,
                    teamSize = overlap.TeamSize,
                    maxRate = overlap.Result.MaxRate,
                    breachingHalfDays = overlap.Result.BreachingHalfDays
                        .Select(s => new
                        {
                            date = s.Date.ToString("yyyy-MM-dd"),
                            halfDay = ContractMapper.FormatHalfDay(s.HalfDay)
                        }).ToList()
                });
            }

            await transaction.RollbackAsync(cancellationToken);
            return Result<LeaveRequestDetail>.Fail(error);
        }

        var years = entity.Allocations.Select(a => a.Year).ToList();
        var exists = await db.AnnualBalances
            .Where(b => b.EmployeeId == entity.EmployeeId &&
                        b.LeaveTypeId == entity.LeaveTypeId &&
                        years.Contains(b.Year))
            .Select(b => b.Year)
            .ToListAsync(cancellationToken);

        foreach (var allocation in entity.Allocations.OrderBy(a => a.Year))
        {
            if (!exists.Contains(allocation.Year))
            {
                db.AnnualBalances.Add(new AnnualBalance
                {
                    EmployeeId = entity.EmployeeId,
                    LeaveTypeId = entity.LeaveTypeId,
                    Year = allocation.Year,
                    GrantedHalfDays = 0,
                    ConsumedHalfDays = 0,
                    Version = 0,
                    CreatedAt = clock.UtcNow,
                    UpdatedAt = clock.UtcNow
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var balances = new List<AnnualBalance>();
        foreach (var allocation in entity.Allocations.OrderBy(a => a.Year))
        {
            var balance = await LockBalanceAsync(
                entity.EmployeeId,
                entity.LeaveTypeId,
                allocation.Year,
                cancellationToken);

            if (balance is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<LeaveRequestDetail>.Fail(
                    ErrorFactory.Conflict(
                        ErrorCodes.INSUFFICIENT_BALANCE_AT_APPROVAL,
                        "Solde insuffisant",
                        "Le solde d'une des années concernées est introuvable."));
            }

            balances.Add(balance);
        }

        var shortages = entity.Allocations
            .OrderBy(a => a.Year)
            .Where(a => balances.Single(b => b.Year == a.Year).AvailableHalfDays < a.HalfDays)
            .Select(a => new
            {
                year = a.Year,
                requestedDays = a.HalfDays / 2m,
                availableDays = balances.Single(b => b.Year == a.Year).AvailableHalfDays / 2m,
                missingDays = (a.HalfDays - balances.Single(b => b.Year == a.Year).AvailableHalfDays) / 2m
            })
            .ToList();

        if (shortages.Count > 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<LeaveRequestDetail>.Fail(
                ErrorFactory.Conflict(
                        ErrorCodes.INSUFFICIENT_BALANCE_AT_APPROVAL,
                        "Solde insuffisant à l'approbation",
                        "Le solde disponible ne permet pas d'approuver cette demande ; aucun jour n'a été débité.")
                    .With("balanceShortages", shortages));
        }

        var now = clock.UtcNow;

        foreach (var allocation in entity.Allocations.OrderBy(a => a.Year))
        {
            var balance = balances.Single(b => b.Year == allocation.Year);
            balance.Consume(allocation.HalfDays);
            balance.UpdatedAt = now;

            db.BalanceMovements.Add(new BalanceMovement
            {
                EmployeeId = entity.EmployeeId,
                LeaveTypeId = entity.LeaveTypeId,
                Year = allocation.Year,
                DeltaHalfDays = -allocation.HalfDays,
                Nature = BalanceMovementNature.CONSOMMATION_APPROBATION,
                LeaveRequestId = entity.Id,
                ActorId = managerId,
                OccurredAt = now
            });
        }

        entity.Approve(
            managerId,
            now,
            request.Comment,
            overlap.Result.Active,
            request.OverlapAcknowledged);

        entity.DecidedBy = await db.Users.SingleAsync(u => u.Id == managerId, cancellationToken);
        if (entity.AcknowledgedById is not null)
        {
            entity.AcknowledgedBy = entity.DecidedBy;
        }

        db.OverlapEvaluations.Add(new OverlapEvaluation
        {
            LeaveRequestId = entity.Id,
            TeamId = overlap.TeamId,
            TeamSize = overlap.TeamSize,
            MaxRate = overlap.Result.MaxRate ?? 0m,
            BreachingHalfDaysJson = ContractMapper.BreachingJson(overlap.Result),
            TriggerKind = OverlapTriggerKind.DECISION,
            EvaluatedById = managerId,
            EvaluatedAt = now
        });

        auditWriter.Add(
            managerId,
            "LEAVE_REQUEST_APPROVED",
            "LEAVE_REQUEST",
            entity.Id,
            before: new { status = LeaveStatus.PENDING.ToString() },
            after: new
            {
                status = LeaveStatus.APPROVED.ToString(),
                employeeId = entity.EmployeeId,
                totalHalfDays = entity.TotalHalfDays,
                overlapFlagged = overlap.Result.Active,
                overlapAcknowledged = request.OverlapAcknowledged && overlap.Result.Active
            });

        if (overlap.Result.Active && request.OverlapAcknowledged)
        {
            auditWriter.Add(
                managerId,
                "OVERLAP_ACKNOWLEDGED",
                "LEAVE_REQUEST",
                entity.Id,
                after: new
                {
                    teamId = overlap.TeamId,
                    teamSize = overlap.TeamSize,
                    maxRate = overlap.Result.MaxRate
                });
        }

        db.Notifications.Add(new Notification
        {
            EventKey = $"DECISION:{entity.Id}",
            Type = NotificationType.DECISION,
            LeaveRequestId = entity.Id,
            RecipientUserId = entity.EmployeeId,
            RecipientEmail = entity.Employee?.Email ?? string.Empty,
            Status = NotificationStatus.PENDING,
            NextAttemptAt = now,
            CreatedAt = now
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (DbErrorHelper.IsCheckViolation(exception) ||
                                                   DbErrorHelper.IsExclusionViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<LeaveRequestDetail>.Fail(
                ErrorFactory.Conflict(
                    ErrorCodes.INSUFFICIENT_BALANCE_AT_APPROVAL,
                    "Solde insuffisant à l'approbation",
                    "Le solde disponible ne permet pas d'approuver cette demande ; aucun jour n'a été débité."));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var acknowledgement = entity.AcknowledgedBy is not null && entity.AcknowledgedAt is not null
            ? new AcknowledgementDto(ContractMapper.Person(entity.AcknowledgedBy), entity.AcknowledgedAt.Value)
            : null;

        var responseOverlap = ContractMapper.ToOverlapDto(
            overlap.Result,
            overlap.TeamId,
            now,
            acknowledgement);

        return Result<LeaveRequestDetail>.Ok(ContractMapper.ToDetail(entity, responseOverlap));
    }

    public async Task<Result<LeaveRequestDetail>> RejectAsync(
        Guid managerId,
        Guid requestId,
        RejectLeaveRequestRequest request,
        IReadOnlyList<string> callerRoles,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            return Result<LeaveRequestDetail>.Fail(
                ErrorFactory.Unprocessable(
                    ErrorCodes.COMMENT_REQUIRED,
                    "Commentaire obligatoire",
                    "Un commentaire est obligatoire pour refuser une demande."));
        }

        if (request.Comment.Length > 1000)
        {
            return Result<LeaveRequestDetail>.Fail(
                ErrorFactory.Validation("Le commentaire ne peut pas dépasser 1 000 caractères."));
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var preloaded = await db.LeaveRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (preloaded is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<LeaveRequestDetail>.Fail(ErrorFactory.NotFound("Demande introuvable."));
        }

        await LockRequestAsync(requestId, cancellationToken);

        var entity = await LoadDetailQuery()
            .SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (entity is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<LeaveRequestDetail>.Fail(ErrorFactory.NotFound("Demande introuvable."));
        }

        var authority = CheckAuthority(entity, managerId, callerRoles);
        if (authority is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<LeaveRequestDetail>.Fail(authority);
        }

        var decision = DecisionPolicy.Evaluate(
            entity.EmployeeId,
            managerId,
            entity.Employee?.ManagerId,
            callerRoles.Contains(RoleNames.MANAGER, StringComparer.Ordinal),
            callerRoles.Contains(RoleNames.ADMIN_RH, StringComparer.Ordinal),
            entity.Status,
            overlapActive: false,
            overlapAcknowledged: true,
            isRejection: true,
            request.Comment);

        if (!decision.Allowed)
        {
            await transaction.RollbackAsync(cancellationToken);

            if (decision.Code == ErrorCodes.INVALID_STATE)
            {
                return Result<LeaveRequestDetail>.Fail(ErrorFactory.InvalidState());
            }

            return Result<LeaveRequestDetail>.Fail(
                ErrorFactory.Unprocessable(
                    decision.Code ?? ErrorCodes.COMMENT_REQUIRED,
                    "Décision invalide",
                    "Le refus ne peut pas être enregistré."));
        }

        var now = clock.UtcNow;
        entity.Reject(managerId, now, request.Comment);
        entity.DecidedBy = await db.Users.SingleAsync(u => u.Id == managerId, cancellationToken);

        auditWriter.Add(
            managerId,
            "LEAVE_REQUEST_REJECTED",
            "LEAVE_REQUEST",
            entity.Id,
            before: new { status = LeaveStatus.PENDING.ToString() },
            after: new { status = LeaveStatus.REJECTED.ToString(), employeeId = entity.EmployeeId });

        db.Notifications.Add(new Notification
        {
            EventKey = $"DECISION:{entity.Id}",
            Type = NotificationType.DECISION,
            LeaveRequestId = entity.Id,
            RecipientUserId = entity.EmployeeId,
            RecipientEmail = entity.Employee?.Email ?? string.Empty,
            Status = NotificationStatus.PENDING,
            NextAttemptAt = now,
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<LeaveRequestDetail>.Ok(ContractMapper.ToDetail(entity));
    }

    private IQueryable<LeaveRequest> LoadDetailQuery() =>
        db.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.LeaveType)
            .Include(r => r.Allocations)
            .Include(r => r.DecidedBy)
            .Include(r => r.CancelledBy)
            .Include(r => r.AcknowledgedBy);

    private static AppError? CheckAuthority(
        LeaveRequest request,
        Guid managerId,
        IReadOnlyList<string> callerRoles)
    {
        if (request.EmployeeId == managerId)
        {
            return ErrorFactory.Forbidden("Un utilisateur ne peut pas décider de sa propre demande.");
        }

        var currentManagerId = request.Employee?.ManagerId;
        if (currentManagerId == managerId)
        {
            return null;
        }

        var hasManagerRole = callerRoles.Contains(RoleNames.MANAGER, StringComparer.Ordinal);
        var hasAdminRole = callerRoles.Contains(RoleNames.ADMIN_RH, StringComparer.Ordinal);

        if (hasAdminRole && !hasManagerRole)
        {
            return ErrorFactory.Forbidden("L'Admin RH ne peut pas décider au titre de son rôle RH.");
        }

        return hasManagerRole
            ? ErrorFactory.NotFound("Demande introuvable dans votre périmètre.")
            : ErrorFactory.Forbidden();
    }

    private async Task AcquireAdvisoryLocksAsync(
        Guid employeeId,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        if (db.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            return;
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended('employee:' || {employeeId.ToString()}, 0))",
            cancellationToken);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended('team:' || {teamId.ToString()}, 0))",
            cancellationToken);
    }

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
