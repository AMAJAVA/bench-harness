using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;
using TeamLeave.Domain.Services;
using TeamLeave.Infrastructure.Audit;
using TeamLeave.Infrastructure.Identity;
using TeamLeave.Infrastructure.Mapping;
using TeamLeave.Infrastructure.Persistence;

namespace TeamLeave.Infrastructure.Services;

public sealed partial class AdminService(
    TeamLeaveDbContext db,
    IClock clock,
    IPasswordHasher passwordHasher,
    BalanceQueryService balanceQuery,
    AuditWriter auditWriter) : IAdminService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [GeneratedRegex("^[A-Z][A-Z0-9_]{1,31}$")]
    private static partial Regex LeaveTypeCodeRegex();

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex ColorRegex();

    public async Task<Result<PagedResult<LeaveTypeDto>>> ListLeaveTypesAsync(
        bool includeInactive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paging = Pagination.Normalize(page, pageSize);
        if (!paging.IsSuccess)
        {
            return Result<PagedResult<LeaveTypeDto>>.Fail(paging.Error!);
        }

        var query = db.LeaveTypes.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(t => t.Label)
            .ThenBy(t => t.Id)
            .Skip(paging.Value!.Skip)
            .Take(paging.Value.PageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<LeaveTypeDto>>.Ok(new PagedResult<LeaveTypeDto>(
            items.Select(ToLeaveTypeDto).ToList(),
            paging.Value.Page,
            paging.Value.PageSize,
            total));
    }

    public async Task<Result<LeaveTypeDto>> CreateLeaveTypeAsync(
        Guid actorId,
        CreateLeaveTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateLeaveType(request.Code, request.Label, request.Color, out var code, out var label, out var color);
        if (validation is not null)
        {
            return Result<LeaveTypeDto>.Fail(validation);
        }

        var duplicate = await db.LeaveTypes.AnyAsync(t => t.Code == code, cancellationToken);
        if (duplicate)
        {
            return Result<LeaveTypeDto>.Fail(
                ErrorFactory.Conflict(ErrorCodes.DUPLICATE_CODE, "Code déjà utilisé", "Un type de congé possède déjà ce code."));
        }

        var now = clock.UtcNow;
        var entity = new LeaveType
        {
            Code = code,
            Label = label,
            Color = color,
            IsActive = request.Active,
            IsSystem = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.LeaveTypes.Add(entity);
        auditWriter.Add(
            actorId,
            "LEAVE_TYPE_CREATED",
            "LEAVE_TYPE",
            entity.Id,
            after: new { active = entity.IsActive });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (DbErrorHelper.IsUniqueViolation(exception))
        {
            return Result<LeaveTypeDto>.Fail(
                ErrorFactory.Conflict(ErrorCodes.DUPLICATE_CODE, "Code déjà utilisé", "Un type de congé possède déjà ce code."));
        }

        return Result<LeaveTypeDto>.Ok(ToLeaveTypeDto(entity));
    }

    public async Task<Result<LeaveTypeDto>> UpdateLeaveTypeAsync(
        Guid actorId,
        Guid leaveTypeId,
        UpdateLeaveTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await db.LeaveTypes.SingleOrDefaultAsync(t => t.Id == leaveTypeId, cancellationToken);
        if (entity is null)
        {
            return Result<LeaveTypeDto>.Fail(ErrorFactory.NotFound("Type de congé introuvable."));
        }

        if (request.Code is not null)
        {
            return Result<LeaveTypeDto>.Fail(
                ErrorFactory.Unprocessable(
                    ErrorCodes.IMMUTABLE_FIELD,
                    "Champ immuable",
                    "Le code d'un type de congé ne peut pas être modifié."));
        }

        var beforeColor = entity.Color;
        var beforeActive = entity.IsActive;

        if (request.Label is not null)
        {
            var label = request.Label.Trim();
            if (label.Length is 0 or > 100)
            {
                return Result<LeaveTypeDto>.Fail(
                    ErrorFactory.Validation("Le libellé doit contenir entre 1 et 100 caractères."));
            }

            entity.Label = label;
        }

        if (request.Color is not null)
        {
            if (!ColorRegex().IsMatch(request.Color))
            {
                return Result<LeaveTypeDto>.Fail(
                    ErrorFactory.Validation("La couleur doit respecter le format #RRGGBB."));
            }

            entity.Color = request.Color;
        }

        if (request.Active is not null)
        {
            entity.IsActive = request.Active.Value;
        }

        entity.UpdatedAt = clock.UtcNow;

        auditWriter.Add(
            actorId,
            "LEAVE_TYPE_UPDATED",
            "LEAVE_TYPE",
            entity.Id,
            before: new { color = beforeColor, active = beforeActive },
            after: new { color = entity.Color, active = entity.IsActive });

        await db.SaveChangesAsync(cancellationToken);
        return Result<LeaveTypeDto>.Ok(ToLeaveTypeDto(entity));
    }

    public async Task<Result<PagedResult<UserAdminDto>>> ListUsersAsync(
        Guid? teamId,
        bool? active,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paging = Pagination.Normalize(page, pageSize);
        if (!paging.IsSuccess)
        {
            return Result<PagedResult<UserAdminDto>>.Fail(paging.Error!);
        }

        var query = db.Users
            .AsNoTracking()
            .Include(u => u.Team)
            .Include(u => u.Manager)
            .AsQueryable();

        if (teamId is not null)
        {
            var teamValue = teamId.Value;
            query = query.Where(u => u.TeamId == teamValue);
        }

        if (active is not null)
        {
            var activeValue = active.Value;
            query = query.Where(u => u.IsActive == activeValue);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ThenBy(u => u.Id)
            .Skip(paging.Value!.Skip)
            .Take(paging.Value.PageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<UserAdminDto>>.Ok(new PagedResult<UserAdminDto>(
            items.Select(ToUserDto).ToList(),
            paging.Value.Page,
            paging.Value.PageSize,
            total));
    }

    public async Task<Result<UserAdminDto>> CreateUserAsync(
        Guid actorId,
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateUserFields(request.Email, request.FirstName, request.LastName, request.Roles, out var email, out var firstName, out var lastName, out var roles);
        if (validation is not null)
        {
            return Result<UserAdminDto>.Fail(validation);
        }

        var team = await db.Teams.SingleOrDefaultAsync(t => t.Id == request.TeamId, cancellationToken);
        if (team is null || !team.IsActive)
        {
            return Result<UserAdminDto>.Fail(
                ErrorFactory.Unprocessable(ErrorCodes.TEAM_INACTIVE, "Équipe indisponible", "L'équipe demandée est inexistante ou inactive."));
        }

        User? manager = null;
        if (request.ManagerId is not null)
        {
            manager = await db.Users.SingleOrDefaultAsync(u => u.Id == request.ManagerId, cancellationToken);
            if (manager is null || !manager.IsActive)
            {
                return Result<UserAdminDto>.Fail(
                    ErrorFactory.Unprocessable(ErrorCodes.INVALID_MANAGER, "Manager invalide", "Le manager doit être un utilisateur actif."));
            }
        }

        var duplicate = await db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (duplicate)
        {
            return Result<UserAdminDto>.Fail(
                ErrorFactory.Conflict(ErrorCodes.EMAIL_ALREADY_USED, "Email déjà utilisé", "Un compte utilise déjà cet email."));
        }

        var now = clock.UtcNow;
        var placeholder = SecureTokens.CreateTemporaryPassword(32);
        var entity = new User
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = await passwordHasher.HashAsync(placeholder, cancellationToken),
            Roles = roles,
            TeamId = team.Id,
            Team = team,
            ManagerId = manager?.Id,
            Manager = manager,
            IsActive = true,
            MustChangePassword = true,
            SecurityStamp = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Users.Add(entity);
        auditWriter.Add(
            actorId,
            "USER_CREATED",
            "USER",
            entity.Id,
            after: new { email = entity.Email, teamId = entity.TeamId, managerId = entity.ManagerId, roles = entity.Roles });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (DbErrorHelper.IsUniqueViolation(exception))
        {
            return Result<UserAdminDto>.Fail(
                ErrorFactory.Conflict(ErrorCodes.EMAIL_ALREADY_USED, "Email déjà utilisé", "Un compte utilise déjà cet email."));
        }

        return Result<UserAdminDto>.Ok(ToUserDto(entity));
    }

    public async Task<Result<UserAdminDto>> UpdateUserAsync(
        Guid actorId,
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var user = await db.Users
            .Include(u => u.Team)
            .Include(u => u.Manager)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<UserAdminDto>.Fail(ErrorFactory.NotFound("Utilisateur introuvable."));
        }

        var oldRoles = user.Roles.ToList();
        var newRoles = oldRoles;

        if (request.Roles is not null)
        {
            var rolesResult = NormalizeRoles(request.Roles);
            if (!rolesResult.IsSuccess)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<UserAdminDto>.Fail(rolesResult.Error!);
            }

            newRoles = rolesResult.Value!;

            if (user.IsActive &&
                oldRoles.Contains(RoleNames.ADMIN_RH, StringComparer.Ordinal) &&
                !newRoles.Contains(RoleNames.ADMIN_RH, StringComparer.Ordinal))
            {
                await AcquireAdminLockAsync(cancellationToken);
                if (await IsLastActiveAdminAsync(user.Id, cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Result<UserAdminDto>.Fail(
                        ErrorFactory.Conflict(
                            ErrorCodes.LAST_HR_ADMIN,
                            "Dernier Admin RH",
                            "Le dernier Admin RH actif ne peut pas perdre son rôle."));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            if (!TryNormalizeEmail(request.Email, out var email))
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<UserAdminDto>.Fail(ErrorFactory.Validation("L'adresse email est invalide."));
            }

            var duplicate = await db.Users.AnyAsync(u => u.Email == email && u.Id != userId, cancellationToken);
            if (duplicate)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<UserAdminDto>.Fail(
                    ErrorFactory.Conflict(ErrorCodes.EMAIL_ALREADY_USED, "Email déjà utilisé", "Un compte utilise déjà cet email."));
            }

            user.Email = email;
        }

        if (request.FirstName is not null)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) || request.FirstName.Trim().Length > 100)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<UserAdminDto>.Fail(ErrorFactory.Validation("Le prénom est invalide."));
            }

            user.FirstName = request.FirstName.Trim();
        }

        if (request.LastName is not null)
        {
            if (string.IsNullOrWhiteSpace(request.LastName) || request.LastName.Trim().Length > 100)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<UserAdminDto>.Fail(ErrorFactory.Validation("Le nom est invalide."));
            }

            user.LastName = request.LastName.Trim();
        }

        if (request.TeamId is not null && request.TeamId != user.TeamId)
        {
            var team = await db.Teams.SingleOrDefaultAsync(t => t.Id == request.TeamId, cancellationToken);
            if (team is null || !team.IsActive)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<UserAdminDto>.Fail(
                    ErrorFactory.Unprocessable(ErrorCodes.TEAM_INACTIVE, "Équipe indisponible", "L'équipe demandée est inexistante ou inactive."));
            }

            user.TeamId = team.Id;
            user.Team = team;
        }

        if (request.ClearManager)
        {
            user.ManagerId = null;
            user.Manager = null;
        }
        else if (request.ManagerId is not null && request.ManagerId != user.ManagerId)
        {
            if (request.ManagerId == user.Id)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<UserAdminDto>.Fail(
                    ErrorFactory.Unprocessable(ErrorCodes.INVALID_MANAGER, "Manager invalide", "Un utilisateur ne peut pas être son propre manager."));
            }

            var manager = await db.Users.SingleOrDefaultAsync(u => u.Id == request.ManagerId, cancellationToken);
            if (manager is null || !manager.IsActive)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<UserAdminDto>.Fail(
                    ErrorFactory.Unprocessable(ErrorCodes.INVALID_MANAGER, "Manager invalide", "Le manager doit être un utilisateur actif."));
            }

            user.ManagerId = manager.Id;
            user.Manager = manager;
        }

        var rolesChanged = !oldRoles.SequenceEqual(newRoles, StringComparer.Ordinal);
        if (rolesChanged)
        {
            user.Roles = newRoles;
            user.RenewSecurityStamp();
            await RevokeRefreshTokensAsync(user.Id, "ROLE_CHANGED", cancellationToken);
        }

        user.UpdatedAt = clock.UtcNow;

        auditWriter.Add(
            actorId,
            "USER_UPDATED",
            "USER",
            user.Id,
            before: new
            {
                email = user.Email,
                teamId = user.TeamId,
                managerId = user.ManagerId,
                roles = oldRoles
            },
            after: new
            {
                email = user.Email,
                teamId = user.TeamId,
                managerId = user.ManagerId,
                roles = newRoles
            });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (DbErrorHelper.IsUniqueViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<UserAdminDto>.Fail(
                ErrorFactory.Conflict(ErrorCodes.EMAIL_ALREADY_USED, "Email déjà utilisé", "Un compte utilise déjà cet email."));
        }

        return Result<UserAdminDto>.Ok(ToUserDto(user));
    }

    public async Task<Result<DeactivateUserResponse>> DeactivateUserAsync(
        Guid actorId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<DeactivateUserResponse>.Fail(ErrorFactory.NotFound("Utilisateur introuvable."));
        }

        if (!user.IsActive)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<DeactivateUserResponse>.Fail(ErrorFactory.InvalidState("Ce compte est déjà désactivé."));
        }

        if (user.HasRole(RoleNames.ADMIN_RH))
        {
            await AcquireAdminLockAsync(cancellationToken);
            if (await IsLastActiveAdminAsync(user.Id, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<DeactivateUserResponse>.Fail(
                    ErrorFactory.Conflict(
                        ErrorCodes.LAST_HR_ADMIN,
                        "Dernier Admin RH",
                        "Le dernier Admin RH actif ne peut pas être désactivé."));
            }
        }

        await AcquireEmployeeLockAsync(user.Id, cancellationToken);

        var now = clock.UtcNow;
        var today = clock.TodayInParis;

        var requests = await db.LeaveRequests
            .Include(r => r.Allocations)
            .Where(r => r.EmployeeId == user.Id &&
                        (r.Status == LeaveStatus.PENDING ||
                         (r.Status == LeaveStatus.APPROVED && r.StartDate > today)))
            .OrderBy(r => r.SubmittedAt)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var cancelledPending = 0;
        var cancelledApproved = 0;
        var restoredHalfDays = 0;

        foreach (var request in requests)
        {
            if (request.Status == LeaveStatus.APPROVED)
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
                        return Result<DeactivateUserResponse>.Fail(
                            ErrorFactory.InvalidState("Un solde est introuvable ; la désactivation a été annulée."));
                    }

                    balance.Restore(allocation.HalfDays);
                    balance.UpdatedAt = now;
                    restoredHalfDays += allocation.HalfDays;

                    db.BalanceMovements.Add(new BalanceMovement
                    {
                        EmployeeId = request.EmployeeId,
                        LeaveTypeId = request.LeaveTypeId,
                        Year = allocation.Year,
                        DeltaHalfDays = allocation.HalfDays,
                        Nature = BalanceMovementNature.RESTITUTION_ANNULATION,
                        LeaveRequestId = request.Id,
                        ActorId = actorId,
                        OccurredAt = now
                    });
                }

                cancelledApproved++;
            }
            else
            {
                cancelledPending++;
            }

            request.Cancel(actorId, now, CancellationReasons.ACCOUNT_DEACTIVATION);
            auditWriter.Add(
                actorId,
                "LEAVE_REQUEST_CANCELLED_BY_DEACTIVATION",
                "LEAVE_REQUEST",
                request.Id,
                after: new
                {
                    status = LeaveStatus.CANCELLED.ToString(),
                    employeeId = request.EmployeeId,
                    reason = CancellationReasons.ACCOUNT_DEACTIVATION
                },
                reason: CancellationReasons.ACCOUNT_DEACTIVATION);
        }

        user.IsActive = false;
        user.RenewSecurityStamp();
        user.UpdatedAt = now;

        await RevokeRefreshTokensAsync(user.Id, "DEACTIVATED", cancellationToken);

        auditWriter.Add(
            actorId,
            "USER_DEACTIVATED",
            "USER",
            user.Id,
            before: new { isActive = true },
            after: new
            {
                isActive = false,
                cancelledPendingRequests = cancelledPending,
                cancelledFutureApprovedRequests = cancelledApproved,
                restoredHalfDays
            });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<DeactivateUserResponse>.Ok(new DeactivateUserResponse(
            user.Id,
            now,
            cancelledPending,
            cancelledApproved,
            restoredHalfDays / 2m));
    }

    public async Task<Result<ResetPasswordResponse>> ResetPasswordAsync(
        Guid actorId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return Result<ResetPasswordResponse>.Fail(ErrorFactory.NotFound("Utilisateur introuvable."));
        }

        if (!user.IsActive)
        {
            return Result<ResetPasswordResponse>.Fail(
                ErrorFactory.Unprocessable(ErrorCodes.USER_INACTIVE, "Compte inactif", "Un compte inactif ne peut pas être réinitialisé."));
        }

        var temporaryPassword = SecureTokens.CreateTemporaryPassword(20);
        user.PasswordHash = await passwordHasher.HashAsync(temporaryPassword, cancellationToken);
        user.MustChangePassword = true;
        user.FailedLoginCount = 0;
        user.LockoutUntil = null;
        user.RenewSecurityStamp();
        user.UpdatedAt = clock.UtcNow;

        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke("PASSWORD_CHANGED", clock.UtcNow);
        }

        auditWriter.Add(
            actorId,
            "PASSWORD_RESET",
            "USER",
            userId,
            after: new { mustChangePassword = true });

        await db.SaveChangesAsync(cancellationToken);

        return Result<ResetPasswordResponse>.Ok(new ResetPasswordResponse(
            temporaryPassword,
            true,
            tokens.Count));
    }

    public async Task<Result<PagedResult<TeamDto>>> ListTeamsAsync(
        bool? active,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paging = Pagination.Normalize(page, pageSize);
        if (!paging.IsSuccess)
        {
            return Result<PagedResult<TeamDto>>.Fail(paging.Error!);
        }

        var query = db.Teams.AsNoTracking();
        if (active is not null)
        {
            var activeValue = active.Value;
            query = query.Where(t => t.IsActive == activeValue);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .Skip(paging.Value!.Skip)
            .Take(paging.Value.PageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<TeamDto>>.Ok(new PagedResult<TeamDto>(
            items.Select(ToTeamDto).ToList(),
            paging.Value.Page,
            paging.Value.PageSize,
            total));
    }

    public async Task<Result<TeamDto>> CreateTeamAsync(
        Guid actorId,
        CreateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
        {
            return Result<TeamDto>.Fail(ErrorFactory.Validation("Le nom de l'équipe doit contenir entre 1 et 100 caractères."));
        }

        if (await db.Teams.AnyAsync(t => t.Name == name, cancellationToken))
        {
            return Result<TeamDto>.Fail(
                ErrorFactory.Conflict(ErrorCodes.DUPLICATE_NAME, "Nom déjà utilisé", "Une équipe porte déjà ce nom."));
        }

        var now = clock.UtcNow;
        var team = new Team { Name = name, IsActive = true, CreatedAt = now, UpdatedAt = now };
        db.Teams.Add(team);
        auditWriter.Add(actorId, "TEAM_CREATED", "TEAM", team.Id, after: new { name = team.Name, active = true });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (DbErrorHelper.IsUniqueViolation(exception))
        {
            return Result<TeamDto>.Fail(
                ErrorFactory.Conflict(ErrorCodes.DUPLICATE_NAME, "Nom déjà utilisé", "Une équipe porte déjà ce nom."));
        }

        return Result<TeamDto>.Ok(ToTeamDto(team));
    }

    public async Task<Result<TeamDto>> UpdateTeamAsync(
        Guid actorId,
        Guid teamId,
        UpdateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var team = await db.Teams.SingleOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
        {
            return Result<TeamDto>.Fail(ErrorFactory.NotFound("Équipe introuvable."));
        }

        if (request.Name is not null)
        {
            var name = request.Name.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            {
                return Result<TeamDto>.Fail(ErrorFactory.Validation("Le nom de l'équipe doit contenir entre 1 et 100 caractères."));
            }

            if (await db.Teams.AnyAsync(t => t.Name == name && t.Id != teamId, cancellationToken))
            {
                return Result<TeamDto>.Fail(
                    ErrorFactory.Conflict(ErrorCodes.DUPLICATE_NAME, "Nom déjà utilisé", "Une équipe porte déjà ce nom."));
            }

            team.Name = name;
        }

        if (request.Active is not null)
        {
            team.IsActive = request.Active.Value;
        }

        team.UpdatedAt = clock.UtcNow;

        auditWriter.Add(
            actorId,
            "TEAM_UPDATED",
            "TEAM",
            team.Id,
            before: new { name = team.Name, active = team.IsActive },
            after: new { name = team.Name, active = team.IsActive });

        await db.SaveChangesAsync(cancellationToken);
        return Result<TeamDto>.Ok(ToTeamDto(team));
    }

    public async Task<Result<BalancesResponse>> GetUserBalancesAsync(
        Guid userId,
        int? year,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!exists)
        {
            return Result<BalancesResponse>.Fail(ErrorFactory.NotFound("Utilisateur introuvable."));
        }

        var targetYear = year ?? clock.TodayInParis.Year;
        if (targetYear < 2000 || targetYear > 2100)
        {
            return Result<BalancesResponse>.Fail(
                ErrorFactory.Validation("L'année doit être comprise entre 2000 et 2100."));
        }

        var balances = await balanceQuery.GetAsync(userId, targetYear, true, cancellationToken);
        return Result<BalancesResponse>.Ok(balances);
    }

    public async Task<Result<PagedResult<BalanceMovementDto>>> ListBalanceAdjustmentsAsync(
        Guid userId,
        int? year,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!exists)
        {
            return Result<PagedResult<BalanceMovementDto>>.Fail(ErrorFactory.NotFound("Utilisateur introuvable."));
        }

        var paging = Pagination.Normalize(page, pageSize);
        if (!paging.IsSuccess)
        {
            return Result<PagedResult<BalanceMovementDto>>.Fail(paging.Error!);
        }

        var query = db.BalanceMovements
            .AsNoTracking()
            .Where(m => m.EmployeeId == userId &&
                        (m.Nature == BalanceMovementNature.OCTROI_INITIAL ||
                         m.Nature == BalanceMovementNature.AJUSTEMENT));

        if (year is not null)
        {
            if (year < 2000 || year > 2100)
            {
                return Result<PagedResult<BalanceMovementDto>>.Fail(
                    ErrorFactory.Validation("L'année doit être comprise entre 2000 et 2100."));
            }

            var yearValue = year.Value;
            query = query.Where(m => m.Year == yearValue);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.OccurredAt)
            .ThenByDescending(m => m.Id)
            .Skip(paging.Value!.Skip)
            .Take(paging.Value.PageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<BalanceMovementDto>>.Ok(new PagedResult<BalanceMovementDto>(
            items.Select(m => new BalanceMovementDto(
                m.Id,
                m.EmployeeId,
                m.LeaveTypeId,
                m.Year,
                m.Nature.ToString(),
                m.DeltaHalfDays / 2m,
                m.Reason,
                m.ActorId,
                m.OccurredAt)).ToList(),
            paging.Value.Page,
            paging.Value.PageSize,
            total));
    }

    public async Task<Result<AdjustBalanceResponse>> AdjustBalanceAsync(
        Guid actorId,
        Guid userId,
        AdjustBalanceRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return Result<AdjustBalanceResponse>.Fail(ErrorFactory.NotFound("Utilisateur introuvable."));
        }

        var leaveType = await db.LeaveTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Id == request.LeaveTypeId, cancellationToken);
        if (leaveType is null)
        {
            return Result<AdjustBalanceResponse>.Fail(ErrorFactory.Validation("Le type de congé est invalide."));
        }

        if (request.Year < 2000 || request.Year > 2100)
        {
            return Result<AdjustBalanceResponse>.Fail(ErrorFactory.Validation("L'année doit être comprise entre 2000 et 2100."));
        }

        if (!BalancePolicy.IsMultipleOfHalfDay(request.DaysDelta) || request.DaysDelta == 0m)
        {
            return Result<AdjustBalanceResponse>.Fail(
                ErrorFactory.Unprocessable(
                    ErrorCodes.INVALID_DAYS_STEP,
                    "Pas de 0,5 jour",
                    "L'ajustement doit être un multiple non nul de 0,5 jour."));
        }

        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 1000)
        {
            return Result<AdjustBalanceResponse>.Fail(
                ErrorFactory.Unprocessable(
                    ErrorCodes.REASON_REQUIRED,
                    "Motif obligatoire",
                    "Un motif de 1 à 1 000 caractères est obligatoire."));
        }

        var deltaHalfDays = BalancePolicy.ToHalfDays(request.DaysDelta);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await AcquireEmployeeLockAsync(userId, cancellationToken);

        var balance = await db.AnnualBalances.SingleOrDefaultAsync(
            b => b.EmployeeId == userId &&
                 b.LeaveTypeId == request.LeaveTypeId &&
                 b.Year == request.Year,
            cancellationToken);

        var wasCreated = false;
        if (balance is null)
        {
            balance = new AnnualBalance
            {
                EmployeeId = userId,
                LeaveTypeId = request.LeaveTypeId,
                Year = request.Year,
                GrantedHalfDays = 0,
                ConsumedHalfDays = 0,
                Version = 0,
                CreatedAt = clock.UtcNow,
                UpdatedAt = clock.UtcNow
            };
            db.AnnualBalances.Add(balance);
            await db.SaveChangesAsync(cancellationToken);
            wasCreated = true;
        }

        balance = await LockBalanceAsync(userId, request.LeaveTypeId, request.Year, cancellationToken);
        if (balance is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<AdjustBalanceResponse>.Fail(ErrorFactory.InvalidState("Solde introuvable."));
        }

        if (!BalancePolicy.CanAdjust(balance.GrantedHalfDays, balance.ConsumedHalfDays, deltaHalfDays))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<AdjustBalanceResponse>.Fail(
                ErrorFactory.Unprocessable(
                    ErrorCodes.BALANCE_WOULD_BE_NEGATIVE,
                    "Solde négatif interdit",
                    "Cet ajustement rendrait le solde disponible négatif."));
        }

        var beforeGranted = balance.GrantedHalfDays;
        balance.AdjustGranted(deltaHalfDays);
        balance.UpdatedAt = clock.UtcNow;

        var nature = wasCreated && deltaHalfDays > 0
            ? BalanceMovementNature.OCTROI_INITIAL
            : BalanceMovementNature.AJUSTEMENT;

        var movement = new BalanceMovement
        {
            EmployeeId = userId,
            LeaveTypeId = request.LeaveTypeId,
            Year = request.Year,
            DeltaHalfDays = deltaHalfDays,
            Nature = nature,
            Reason = reason,
            ActorId = actorId,
            OccurredAt = clock.UtcNow
        };

        db.BalanceMovements.Add(movement);

        auditWriter.Add(
            actorId,
            "BALANCE_ADJUSTED",
            "ANNUAL_BALANCE",
            null,
            before: new { grantedHalfDays = beforeGranted, consumedHalfDays = balance.ConsumedHalfDays },
            after: new
            {
                grantedHalfDays = balance.GrantedHalfDays,
                consumedHalfDays = balance.ConsumedHalfDays,
                deltaHalfDays,
                leaveTypeId = request.LeaveTypeId,
                year = request.Year
            },
            reason: reason);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result<AdjustBalanceResponse>.Ok(new AdjustBalanceResponse(
            movement.Id,
            userId,
            request.LeaveTypeId,
            request.Year,
            nature.ToString(),
            deltaHalfDays / 2m,
            reason,
            balance.GrantedHalfDays / 2m,
            balance.ConsumedHalfDays / 2m,
            balance.AvailableHalfDays / 2m,
            actorId,
            movement.OccurredAt));
    }

    public async Task<Result<PagedResult<LeaveRequestListItem>>> ListLeaveRequestsAsync(
        string? status,
        Guid? teamId,
        Guid? employeeId,
        DateOnly? from,
        DateOnly? to,
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

        var query = db.LeaveRequests
            .AsNoTracking()
            .Include(r => r.LeaveType)
            .AsQueryable();

        if (statusFilter is not null)
        {
            query = query.Where(r => r.Status == statusFilter);
        }

        if (employeeId is not null)
        {
            query = query.Where(r => r.EmployeeId == employeeId);
        }

        if (teamId is not null)
        {
            var teamValue = teamId.Value;
            query = query.Where(r => db.Users.Any(u => u.Id == r.EmployeeId && u.TeamId == teamValue));
        }

        if (from is not null)
        {
            var fromValue = from.Value;
            query = query.Where(r => r.EndDate >= fromValue);
        }

        if (to is not null)
        {
            var toValue = to.Value;
            query = query.Where(r => r.StartDate <= toValue);
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

    public async Task<Result<LeaveRequestDetail>> GetLeaveRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await db.LeaveRequests
            .AsNoTracking()
            .Include(r => r.Employee)
            .Include(r => r.LeaveType)
            .Include(r => r.Allocations)
            .Include(r => r.DecidedBy)
            .Include(r => r.CancelledBy)
            .Include(r => r.AcknowledgedBy)
            .SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (request is null)
        {
            return Result<LeaveRequestDetail>.Fail(ErrorFactory.NotFound("Demande introuvable."));
        }

        var evaluations = await db.OverlapEvaluations
            .AsNoTracking()
            .Where(e => e.LeaveRequestId == requestId)
            .OrderByDescending(e => e.EvaluatedAt)
            .ThenByDescending(e => e.Id)
            .ToListAsync(cancellationToken);

        var history = evaluations.Select(e =>
        {
            var ack = e.TriggerKind == OverlapTriggerKind.DECISION && request.AcknowledgedBy is not null
                ? request.AcknowledgedBy
                : null;
            return ContractMapper.ToOverlapDto(e, ack, request.AcknowledgedAt);
        }).ToList();

        var overlap = history.FirstOrDefault();
        return Result<LeaveRequestDetail>.Ok(ContractMapper.ToDetail(request, overlap, null, history));
    }

    public async Task<Result<PagedResult<NotificationDto>>> ListNotificationsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paging = Pagination.Normalize(page, pageSize);
        if (!paging.IsSuccess)
        {
            return Result<PagedResult<NotificationDto>>.Fail(paging.Error!);
        }

        NotificationStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.GetNames<NotificationStatus>().Contains(status, StringComparer.OrdinalIgnoreCase))
            {
                return Result<PagedResult<NotificationDto>>.Fail(ErrorFactory.Validation("Le statut de notification est invalide."));
            }

            statusFilter = Enum.Parse<NotificationStatus>(status, true);
        }

        var query = db.Notifications.AsNoTracking();
        if (statusFilter is not null)
        {
            query = query.Where(n => n.Status == statusFilter);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip(paging.Value!.Skip)
            .Take(paging.Value.PageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<NotificationDto>>.Ok(new PagedResult<NotificationDto>(
            items.Select(n => new NotificationDto(
                n.Id,
                n.Type.ToString(),
                n.LeaveRequestId,
                n.RecipientEmail,
                n.Status.ToString(),
                n.Attempts,
                n.LastError,
                n.CreatedAt,
                n.SentAt,
                n.NextAttemptAt)).ToList(),
            paging.Value.Page,
            paging.Value.PageSize,
            total));
    }

    public async Task<Result<PagedResult<AuditEntryDto>>> ListAuditEntriesAsync(
        DateOnly? from,
        DateOnly? to,
        string? entityType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paging = Pagination.Normalize(page, pageSize);
        if (!paging.IsSuccess)
        {
            return Result<PagedResult<AuditEntryDto>>.Fail(paging.Error!);
        }

        var query = db.AuditEntries.AsNoTracking().AsQueryable();

        if (from is not null)
        {
            var fromInstant = new DateTimeOffset(from.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(a => a.OccurredAt >= fromInstant);
        }

        if (to is not null)
        {
            var toExclusive = new DateTimeOffset(to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(a => a.OccurredAt < toExclusive);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        var total = await query.CountAsync(cancellationToken);
        var entries = await query
            .OrderByDescending(a => a.OccurredAt)
            .ThenByDescending(a => a.Id)
            .Skip(paging.Value!.Skip)
            .Take(paging.Value.PageSize)
            .ToListAsync(cancellationToken);

        var actorIds = entries.Where(e => e.ActorId is not null).Select(e => e.ActorId!.Value).Distinct().ToList();
        var actors = await db.Users
            .AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => new PersonSummary(u.Id, u.FirstName, u.LastName), cancellationToken);

        return Result<PagedResult<AuditEntryDto>>.Ok(new PagedResult<AuditEntryDto>(
            entries.Select(e => new AuditEntryDto(
                e.Id,
                e.ActorId is not null ? actors.GetValueOrDefault(e.ActorId.Value) : null,
                e.Action,
                e.EntityType,
                e.EntityId,
                e.OccurredAt,
                ParseJson(e.BeforeSummary),
                ParseJson(e.AfterSummary),
                e.Reason)).ToList(),
            paging.Value.Page,
            paging.Value.PageSize,
            total));
    }

    private static LeaveTypeDto ToLeaveTypeDto(LeaveType type) =>
        new(type.Id, type.Code, type.Label, type.Color, type.IsActive, type.IsSystem, type.CreatedAt, type.UpdatedAt);

    private static TeamDto ToTeamDto(Team team) =>
        new(team.Id, team.Name, team.IsActive, team.CreatedAt, team.UpdatedAt);

    private static UserAdminDto ToUserDto(User user) =>
        new(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Roles.ToList(),
            user.TeamId,
            user.Team is null ? null : new TeamSummary(user.Team.Id, user.Team.Name),
            user.ManagerId,
            user.Manager is null ? null : new PersonSummary(user.Manager.Id, user.Manager.FirstName, user.Manager.LastName),
            user.IsActive,
            user.MustChangePassword,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt);

    private static JsonElement? ParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static AppError? ValidateLeaveType(
        string? code,
        string? label,
        string? color,
        out string normalizedCode,
        out string normalizedLabel,
        out string normalizedColor)
    {
        normalizedCode = code?.Trim().ToUpperInvariant() ?? string.Empty;
        normalizedLabel = label?.Trim() ?? string.Empty;
        normalizedColor = color?.Trim() ?? string.Empty;

        if (!LeaveTypeCodeRegex().IsMatch(normalizedCode))
        {
            return ErrorFactory.Validation("Le code doit contenir 2 à 32 caractères, commencer par une lettre et ne contenir que A-Z, 0-9 et _.");
        }

        if (normalizedLabel.Length is 0 or > 100)
        {
            return ErrorFactory.Validation("Le libellé doit contenir entre 1 et 100 caractères.");
        }

        if (!ColorRegex().IsMatch(normalizedColor))
        {
            return ErrorFactory.Validation("La couleur doit respecter le format #RRGGBB.");
        }

        return null;
    }

    private static AppError? ValidateUserFields(
        string? email,
        string? firstName,
        string? lastName,
        IReadOnlyList<string>? roles,
        out string normalizedEmail,
        out string normalizedFirstName,
        out string normalizedLastName,
        out List<string> normalizedRoles)
    {
        normalizedEmail = string.Empty;
        normalizedFirstName = firstName?.Trim() ?? string.Empty;
        normalizedLastName = lastName?.Trim() ?? string.Empty;
        normalizedRoles = [];

        if (!TryNormalizeEmail(email, out normalizedEmail))
        {
            return ErrorFactory.Validation("L'adresse email est invalide.");
        }

        if (normalizedFirstName.Length is 0 or > 100 || normalizedLastName.Length is 0 or > 100)
        {
            return ErrorFactory.Validation("Le prénom et le nom doivent contenir entre 1 et 100 caractères.");
        }

        var rolesResult = NormalizeRoles(roles);
        if (!rolesResult.IsSuccess)
        {
            return rolesResult.Error;
        }

        normalizedRoles = rolesResult.Value!;
        return null;
    }

    private static bool TryNormalizeEmail(string? email, out string normalized)
    {
        normalized = email?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > 320)
        {
            return false;
        }

        return MailAddress.TryCreate(normalized, out var address) &&
               string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase);
    }

    private static Result<List<string>> NormalizeRoles(IReadOnlyList<string>? roles)
    {
        var requested = roles is null || roles.Count == 0
            ? [RoleNames.EMPLOYE]
            : roles.Select(r => r.Trim().ToUpperInvariant()).Distinct(StringComparer.Ordinal).ToList();

        if (requested.Any(r => !RoleNames.All.Contains(r)))
        {
            return Result<List<string>>.Fail(
                ErrorFactory.Validation("Un rôle demandé est inconnu."));
        }

        if (!requested.Contains(RoleNames.EMPLOYE, StringComparer.Ordinal))
        {
            requested.Insert(0, RoleNames.EMPLOYE);
        }

        return Result<List<string>>.Ok(requested);
    }

    private async Task<bool> IsLastActiveAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roles = await db.Users
            .Where(u => u.IsActive)
            .Select(u => new { u.Id, u.Roles })
            .ToListAsync(cancellationToken);

        var admins = roles
            .Where(u => u.Roles.Contains(RoleNames.ADMIN_RH, StringComparer.Ordinal))
            .Select(u => u.Id)
            .ToList();

        return admins.Count <= 1 && admins.Contains(userId);
    }

    private async Task RevokeRefreshTokensAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(reason, now);
        }
    }

    private async Task AcquireAdminLockAsync(CancellationToken cancellationToken)
    {
        if (db.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            return;
        }

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended('admin-roles', 0))",
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
