using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Users;

namespace TeamLeave.Application.Admin;

/// <summary>Gestion des utilisateurs et des soldes par l'admin RH (US-16, US-17 ; RB-01 à RB-04, RB-27 ; GAP-04, GAP-06, GAP-12).</summary>
public sealed class UserAdminService(
    ITeamLeaveDbContext db,
    ICurrentUser currentUser,
    Clock clock,
    UseCaseExecutor executor,
    IPasswordHasher hasher,
    AuditWriter audit,
    BalanceService balanceService)
{
    // ------------------------------------------------------------------ Utilisateurs

    public async Task<PagedResult<UserDto>> ListAsync(UserListQuery query, CancellationToken ct)
    {
        var source = db.Users.AsNoTracking();
        if (query.IsActive is { } active)
        {
            source = source.Where(u => u.IsActive == active);
        }

        if (query.ManagerId is { } managerId)
        {
            source = source.Where(u => u.ManagerId == managerId);
        }

        if (query.Role is { } role)
        {
            source = source.Where(u => u.Roles.Any(r => r.Role == role));
        }

        if (query.HasManager is { } hasManager)
        {
            source = source.Where(u => (u.ManagerId != null) == hasManager);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToUpperInvariant();
            source = source.Where(u => u.NormalizedEmail.Contains(term)
                || u.FirstName.ToUpper().Contains(term)
                || u.LastName.ToUpper().Contains(term));
        }

        var page = await source.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).Select(u => u.Id).ToPageAsync(query.Page, query.PageSize, ct);
        var dtos = await ToDtosAsync(page.Items, ct);
        return new PagedResult<UserDto>(dtos, page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<UserDto> GetAsync(Guid id, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(u => u.Id == id, ct))
        {
            throw new NotFoundException();
        }

        return (await ToDtosAsync([id], ct))[0];
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest input, CancellationToken ct)
    {
        if (PasswordPolicy.Validate(input.TemporaryPassword, input.Email) is { } policyError)
        {
            throw new ValidationException("temporaryPassword", policyError);
        }

        var id = await executor.ExecuteAsync(
            async token =>
            {
                var normalized = User.Normalize(input.Email!);
                if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalized, token))
                {
                    throw new DomainException(ErrorCodes.EmailAlreadyUsed, "Cette adresse email est déjà utilisée.");
                }

                var userId = Guid.CreateVersion7();
                if (input.ManagerId is { } managerId)
                {
                    await ValidateAndTouchHierarchyAsync(userId, managerId, token);
                }

                var user = User.Create(
                    userId,
                    input.Email!,
                    input.FirstName!,
                    input.LastName!,
                    input.ManagerId,
                    input.Roles ?? [],
                    hasher.Hash(input.TemporaryPassword!),
                    mustChangePassword: true,
                    clock.UtcNow);
                db.Users.Add(user);

                // GAP-15 : un solde à 0 par type soumis à décompte.
                var trackedTypes = await db.LeaveTypes.Where(t => t.IsTracked).Select(t => t.Id).ToListAsync(token);
                foreach (var typeId in trackedTypes)
                {
                    db.Balances.Add(Balance.CreateEmpty(Guid.CreateVersion7(), userId, typeId));
                }

                audit.Write("UserCreated", "User", userId.ToString(), null, Snapshot(user));
                return userId;
            },
            ct);
        return await GetAsync(id, ct);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest input, CancellationToken ct)
    {
        await executor.ExecuteAsync(
            async token =>
            {
                var user = await db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id, token) ?? throw new NotFoundException();
                var before = Snapshot(user);

                var normalized = User.Normalize(input.Email!);
                if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalized && u.Id != id, token))
                {
                    throw new DomainException(ErrorCodes.EmailAlreadyUsed, "Cette adresse email est déjà utilisée.");
                }

                if (input.ManagerId != user.ManagerId && input.ManagerId is { } managerId)
                {
                    await ValidateAndTouchHierarchyAsync(id, managerId, token);
                }

                var newRoles = new HashSet<Role>(input.Roles!) { Role.Employee };
                if (user.HasRole(Role.Manager) && !newRoles.Contains(Role.Manager)
                    && await db.Users.AnyAsync(u => u.ManagerId == id && u.IsActive, token))
                {
                    throw new DomainException(ErrorCodes.HasActiveReports, "Ce manager a des subordonnés actifs : rattachez-les d'abord à un autre manager.");
                }

                if (user.IsActive && user.HasRole(Role.HrAdmin) && !newRoles.Contains(Role.HrAdmin))
                {
                    await EnsureAnotherActiveHrAdminAsync(id, token);
                }

                user.SetProfile(input.Email!, input.FirstName!, input.LastName!);
                user.SetManager(input.ManagerId);
                var rolesChanged = user.ReplaceRoles(newRoles);
                user.Touch();

                var action = rolesChanged ? "RolesChanged" : before.ManagerId != input.ManagerId ? "ManagerChanged" : "UserUpdated";
                audit.Write(action, "User", id.ToString(), before, Snapshot(user));
            },
            ct);
        return await GetAsync(id, ct);
    }

    /// <summary>RB-27, CA-17.4, CA-17.5 : demandes en attente annulées par le Système, jetons révoqués (sst).</summary>
    public async Task<UserDto> DeactivateAsync(Guid id, CancellationToken ct)
    {
        await executor.ExecuteAsync(
            async token =>
            {
                var user = await db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id, token) ?? throw new NotFoundException();
                if (!user.IsActive)
                {
                    throw new DomainException(ErrorCodes.UserAlreadyInactive, "Le compte est déjà désactivé.");
                }

                if (await db.Users.AnyAsync(u => u.ManagerId == id && u.IsActive, token))
                {
                    throw new DomainException(ErrorCodes.HasActiveReports, "Cet utilisateur a des subordonnés actifs.");
                }

                if (user.HasRole(Role.HrAdmin))
                {
                    await EnsureAnotherActiveHrAdminAsync(id, token);
                }

                var now = clock.UtcNow;
                var pending = await db.LeaveRequests.Where(r => r.RequesterId == id && r.Status == LeaveRequestStatus.Pending).ToListAsync(token);
                foreach (var request in pending)
                {
                    request.CancelBySystem(LeaveRequest.DeactivationReason, now);
                }

                user.Deactivate(now);
                audit.Write("UserDeactivated", "User", id.ToString(), new { isActive = true }, new { isActive = false, cancelledPendingRequests = pending.Count });
            },
            ct);
        return await GetAsync(id, ct);
    }

    /// <summary>GAP-06 : mot de passe temporaire transmis hors bande, changement obligatoire, verrouillage levé.</summary>
    public async Task ResetPasswordAsync(Guid id, ResetPasswordRequest input, CancellationToken ct)
    {
        await executor.ExecuteAsync(
            async token =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, token) ?? throw new NotFoundException();
                if (!user.IsActive)
                {
                    throw new DomainException(ErrorCodes.UserAlreadyInactive, "Le compte est désactivé.");
                }

                if (PasswordPolicy.Validate(input.TemporaryPassword, user.Email) is { } policyError)
                {
                    throw new ValidationException("temporaryPassword", policyError);
                }

                user.ResetPassword(hasher.Hash(input.TemporaryPassword!));
                audit.Write("PasswordReset", "User", id.ToString(), null, new { mustChangePassword = true });
            },
            ct);
    }

    /// <summary>Export RGPD (ENF-RGPD5), tracé dans le journal (GAP-13).</summary>
    public async Task<object> ExportAsync(Guid id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id, ct) ?? throw new NotFoundException();
        var requests = await db.LeaveRequests.AsNoTracking().Where(r => r.RequesterId == id).OrderBy(r => r.SubmittedAtUtc).ToListAsync(ct);
        var requestIds = requests.Select(r => r.Id).ToList();
        var history = await db.LeaveRequestHistory.AsNoTracking().Where(h => requestIds.Contains(h.LeaveRequestId)).OrderBy(h => h.Id).ToListAsync(ct);
        var balances = await db.Balances.AsNoTracking().Where(b => b.UserId == id).ToListAsync(ct);
        var balanceIds = balances.Select(b => b.Id).ToList();
        var movements = await db.BalanceMovements.AsNoTracking().Where(m => balanceIds.Contains(m.BalanceId)).OrderBy(m => m.Id).ToListAsync(ct);
        var notifications = await db.Notifications.AsNoTracking()
            .Where(n => n.RecipientEmail == user.Email)
            .OrderBy(n => n.CreatedAtUtc)
            .Select(n => new { n.Id, n.EventType, n.LeaveRequestId, n.Status, n.Attempts, n.CreatedAtUtc, n.SentAtUtc })
            .ToListAsync(ct);
        var entityId = id.ToString();
        var auditEntries = await db.AuditLog.AsNoTracking()
            .Where(a => a.EntityType == "User" && a.EntityId == entityId)
            .OrderBy(a => a.Id)
            .Select(a => new { a.Id, a.Action, a.AuthorId, a.OccurredAtUtc, a.Details })
            .ToListAsync(ct);

        await executor.ExecuteAsync(
            _ =>
            {
                audit.Write("UserDataExported", "User", entityId, null, new { exportedAtUtc = clock.UtcNow });
                return Task.CompletedTask;
            },
            ct);

        return new
        {
            exportedAtUtc = clock.UtcNow,
            profile = new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.ManagerId,
                user.IsActive,
                user.CreatedAtUtc,
                user.DeactivatedAtUtc,
                user.MustChangePassword,
            },
            roles = user.RoleList(),
            leaveRequests = requests.Select(r => new
            {
                r.Id,
                r.LeaveTypeId,
                r.StartDate,
                r.EndDate,
                r.DaysCount,
                comment = r.RequesterComment,
                r.Status,
                r.SubmittedAtUtc,
                r.DecidedById,
                r.DecidedAtUtc,
                r.DecisionComment,
                r.CancelledAtUtc,
                r.CancellationReason,
                history = history.Where(h => h.LeaveRequestId == r.Id)
                    .Select(h => new { h.FromStatus, h.ToStatus, h.AuthorId, h.OccurredAtUtc, h.Comment }),
            }),
            balances = balances.Select(b => new
            {
                b.LeaveTypeId,
                amount = DayAmount.ToDecimal(b.AmountHundredths),
                movements = movements.Where(m => m.BalanceId == b.Id).Select(m => new
                {
                    m.Id,
                    m.Kind,
                    quantity = DayAmount.ToDecimal(m.QuantityHundredths),
                    balanceAfter = DayAmount.ToDecimal(m.BalanceAfterHundredths),
                    m.Reason,
                    m.LeaveRequestId,
                    m.AuthorId,
                    m.OccurredAtUtc,
                }),
            }),
            notifications,
            auditEntries,
        };
    }

    // ------------------------------------------------------------------ Soldes (US-16)

    public async Task<IReadOnlyList<BalanceDto>> GetBalancesAsync(Guid userId, CancellationToken ct)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await balanceService.GetBalancesAsync(userId, ct);
    }

    public async Task<PagedResult<MovementDto>> GetMovementsAsync(Guid userId, Guid leaveTypeId, PageQuery page, CancellationToken ct)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await balanceService.GetMovementsAsync(userId, leaveTypeId, page.Page, page.PageSize, ct);
    }

    /// <summary>Allocation ou ajustement (RB-11, RB-25, CA-16.1 à CA-16.4).</summary>
    public async Task<MovementDto> AddMovementAsync(Guid userId, Guid leaveTypeId, CreateMovementRequest input, CancellationToken ct)
    {
        var kind = input.Kind!.Value;
        if (kind is not (MovementKind.Allocation or MovementKind.Adjustment))
        {
            throw new ValidationException("kind", "Seuls les mouvements Allocation et Adjustment sont autorisés.");
        }

        if (!DayAmount.TryFromDecimal(input.Quantity!.Value, out var quantity) || quantity == 0 || (kind == MovementKind.Allocation && quantity < 0))
        {
            throw new ValidationException("quantity", "Quantité invalide : non nulle, 2 décimales au plus, positive pour une allocation.");
        }

        var movement = await executor.ExecuteAsync(
            async token =>
            {
                await EnsureUserExistsAsync(userId, token);
                var type = await db.LeaveTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Id == leaveTypeId, token) ?? throw new NotFoundException();
                if (!type.IsTracked)
                {
                    throw new DomainException(ErrorCodes.LeaveTypeNotTracked, "Ce type de congé n'est pas soumis à décompte.");
                }

                var balance = await balanceService.GetOrCreateAsync(userId, leaveTypeId, token);
                var pending = await balanceService.PendingHundredthsAsync(userId, leaveTypeId, token);
                var before = DayAmount.ToDecimal(balance.AmountHundredths);
                var created = balance.ApplyHrMovement(kind, quantity, input.Reason!, pending, currentUser.Id, clock.UtcNow);
                db.BalanceMovements.Add(created);
                audit.Write(
                    kind == MovementKind.Allocation ? "BalanceAllocated" : "BalanceAdjusted",
                    "Balance",
                    balance.Id.ToString(),
                    new { userId, leaveTypeId, amount = before },
                    new { userId, leaveTypeId, amount = DayAmount.ToDecimal(balance.AmountHundredths), reason = created.Reason });
                return created;
            },
            ct);

        var author = (await db.PeopleAsync([currentUser.Id], ct))[currentUser.Id];
        return BalanceService.ToDto(movement, author);
    }

    // ------------------------------------------------------------------ Interne

    private async Task EnsureUserExistsAsync(Guid userId, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
        {
            throw new NotFoundException();
        }
    }

    /// <summary>
    /// RB-03 : valide le rattachement et « touche » le manager et ses ascendants (§6.2 M3, scénario S7).
    /// </summary>
    private async Task ValidateAndTouchHierarchyAsync(Guid userId, Guid managerId, CancellationToken ct)
    {
        var manager = await db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == managerId, ct)
            ?? throw new NotFoundException("Manager introuvable.");
        var ancestors = new Dictionary<Guid, Guid?> { [manager.Id] = manager.ManagerId };
        manager.Touch();
        var cursor = manager.ManagerId;
        while (cursor is { } current && current != userId && !ancestors.ContainsKey(current))
        {
            var ancestor = await db.Users.FirstOrDefaultAsync(u => u.Id == current, ct);
            if (ancestor is null)
            {
                break;
            }

            ancestor.Touch();
            ancestors[ancestor.Id] = ancestor.ManagerId;
            cursor = ancestor.ManagerId;
        }

        HierarchyValidator.Validate(
            userId,
            new ManagerCandidate(manager.Id, manager.IsActive, manager.HasRole(Role.Manager), manager.ManagerId),
            id => ancestors.GetValueOrDefault(id));
    }

    /// <summary>GAP-12 : il reste au moins un admin RH actif ; les admins RH actifs sont touchés (INV-8).</summary>
    private async Task EnsureAnotherActiveHrAdminAsync(Guid excludedUserId, CancellationToken ct)
    {
        var others = await db.Users
            .Where(u => u.IsActive && u.Id != excludedUserId && u.Roles.Any(r => r.Role == Role.HrAdmin))
            .ToListAsync(ct);
        if (others.Count == 0)
        {
            throw new DomainException(ErrorCodes.LastHrAdmin, "Il doit rester au moins un admin RH actif.");
        }

        foreach (var other in others)
        {
            other.Touch();
        }
    }

    private async Task<List<UserDto>> ToDtosAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking().Include(u => u.Roles).Where(u => ids.Contains(u.Id)).ToListAsync(ct);
        var managerIds = users.Where(u => u.ManagerId.HasValue).Select(u => u.ManagerId!.Value);
        var people = await db.PeopleAsync(managerIds, ct);
        var reports = await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.ManagerId != null && ids.Contains(u.ManagerId.Value))
            .GroupBy(u => u.ManagerId!.Value)
            .Select(g => new { ManagerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ManagerId, x => x.Count, ct);
        var byId = users.ToDictionary(u => u.Id);
        return ids.Where(byId.ContainsKey).Select(id =>
        {
            var u = byId[id];
            return new UserDto(
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.RoleList(),
                u.ManagerId is { } m ? people.GetValueOrDefault(m) : null,
                u.IsActive,
                u.MustChangePassword,
                reports.GetValueOrDefault(u.Id),
                u.CreatedAtUtc,
                u.DeactivatedAtUtc);
        }).ToList();
    }

    private static UserSnapshot Snapshot(User u) =>
        new(u.Email, u.FirstName, u.LastName, u.RoleList(), u.ManagerId, u.IsActive);

    private sealed record UserSnapshot(string Email, string FirstName, string LastName, IReadOnlyList<Role> Roles, Guid? ManagerId, bool IsActive);
}
