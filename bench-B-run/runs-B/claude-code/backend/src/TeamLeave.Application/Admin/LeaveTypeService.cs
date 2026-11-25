using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Users;

namespace TeamLeave.Application.Admin;

/// <summary>Gestion des types de congé (US-15, RB-08, RB-09, RB-24, GAP-05, GAP-15).</summary>
public sealed class LeaveTypeService(ITeamLeaveDbContext db, ICurrentUser currentUser, UseCaseExecutor executor, AuditWriter audit)
{
    public async Task<IReadOnlyList<LeaveTypeDto>> ListAsync(bool includeInactive, CancellationToken ct)
    {
        // CA-15.4 : les non-RH ne voient que les types actifs, quel que soit le paramètre.
        var all = includeInactive && currentUser.IsInRole(Role.HrAdmin);
        var types = await db.LeaveTypes.AsNoTracking().Where(t => all || t.IsActive).OrderBy(t => t.Label).ToListAsync(ct);
        return types.Select(t => t.ToDto()).ToList();
    }

    public async Task<LeaveTypeDto> CreateAsync(CreateLeaveTypeRequest input, CancellationToken ct)
    {
        var type = await executor.ExecuteAsync(
            async token =>
            {
                if (await db.LeaveTypes.AnyAsync(t => t.Code == input.Code, token))
                {
                    throw new DomainException(ErrorCodes.LeaveTypeCodeAlreadyUsed, $"Le code {input.Code} est déjà utilisé.");
                }

                var created = LeaveType.Create(Guid.CreateVersion7(), input.Code!, input.Label!, input.IsTracked, input.AllowsRetroactive);
                db.LeaveTypes.Add(created);
                if (created.IsTracked)
                {
                    await CreateMissingBalancesAsync(created.Id, token);
                }

                audit.Write("LeaveTypeCreated", "LeaveType", created.Id.ToString(), null, created.ToDto());
                return created;
            },
            ct);
        return type.ToDto();
    }

    public async Task<LeaveTypeDto> UpdateAsync(Guid id, UpdateLeaveTypeRequest input, CancellationToken ct)
    {
        var type = await executor.ExecuteAsync(
            async token =>
            {
                var existing = await db.LeaveTypes.FirstOrDefaultAsync(t => t.Id == id, token) ?? throw new NotFoundException();
                var before = existing.ToDto();
                var newTracked = input.IsTracked!.Value;
                if (newTracked != existing.IsTracked)
                {
                    if (await db.LeaveRequests.AnyAsync(r => r.LeaveTypeId == id && r.Status == LeaveRequestStatus.Pending, token))
                    {
                        throw new DomainException(
                            ErrorCodes.LeaveTypeHasPendingRequests,
                            "Le décompte ne peut pas changer tant que des demandes de ce type sont en attente.");
                    }

                    if (!newTracked && await db.Balances.AnyAsync(b => b.LeaveTypeId == id && b.AmountHundredths != 0, token))
                    {
                        throw new DomainException(
                            ErrorCodes.LeaveTypeHasBalances,
                            "Des soldes non nuls existent pour ce type : il ne peut pas devenir non soumis à décompte.");
                    }
                }

                existing.Update(input.Label!, newTracked, input.AllowsRetroactive!.Value, input.IsActive!.Value);
                existing.Touch();
                if (newTracked && !before.IsTracked)
                {
                    await CreateMissingBalancesAsync(id, token);
                }

                var action = before.IsActive && !existing.IsActive ? "LeaveTypeDeactivated" : "LeaveTypeUpdated";
                audit.Write(action, "LeaveType", id.ToString(), before, existing.ToDto());
                return existing;
            },
            ct);
        return type.ToDto();
    }

    /// <summary>
    /// Suppression d'un type jamais utilisé (CA-15.3). Les soldes à 0 sans mouvement créés d'office (GAP-15) sont supprimés avec lui.
    /// </summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await executor.ExecuteAsync(
            async token =>
            {
                var type = await db.LeaveTypes.FirstOrDefaultAsync(t => t.Id == id, token) ?? throw new NotFoundException();
                if (type.IsSystem)
                {
                    throw new DomainException(ErrorCodes.SystemLeaveType, "Les types initiaux ne peuvent pas être supprimés ; désactivez-les.");
                }

                var used = await db.LeaveRequests.AnyAsync(r => r.LeaveTypeId == id, token)
                    || await (from m in db.BalanceMovements join b in db.Balances on m.BalanceId equals b.Id where b.LeaveTypeId == id select m.Id).AnyAsync(token);
                if (used)
                {
                    throw new DomainException(ErrorCodes.LeaveTypeInUse, "Ce type est utilisé : seule la désactivation est possible.");
                }

                var balances = await db.Balances.Where(b => b.LeaveTypeId == id).ToListAsync(token);
                db.Balances.RemoveRange(balances);
                db.LeaveTypes.Remove(type);
                audit.Write("LeaveTypeDeleted", "LeaveType", id.ToString(), type.ToDto(), null);
            },
            ct);
    }

    private async Task CreateMissingBalancesAsync(Guid leaveTypeId, CancellationToken ct)
    {
        var usersWithBalance = db.Balances.Where(b => b.LeaveTypeId == leaveTypeId).Select(b => b.UserId);
        var userIds = await db.Users.Where(u => !usersWithBalance.Contains(u.Id)).Select(u => u.Id).ToListAsync(ct);
        foreach (var userId in userIds)
        {
            db.Balances.Add(Balance.CreateEmpty(Guid.CreateVersion7(), userId, leaveTypeId));
        }
    }
}
