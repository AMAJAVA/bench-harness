using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Contracts;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Leave;

namespace TeamLeave.Application.Common;

/// <summary>Lecture et écriture des soldes (RB-09 à RB-11, RB-25). Disponible et en attente calculés à la demande.</summary>
public sealed class BalanceService(ITeamLeaveDbContext db)
{
    public async Task<int> PendingHundredthsAsync(Guid userId, Guid leaveTypeId, CancellationToken ct)
    {
        var days = await db.LeaveRequests
            .Where(r => r.RequesterId == userId && r.LeaveTypeId == leaveTypeId && r.Status == LeaveRequestStatus.Pending)
            .SumAsync(r => (int?)r.DaysCount, ct) ?? 0;
        return DayAmount.FromDays(days);
    }

    /// <summary>« Lire ou créer à 0 » (§6.3 S8) ; un doublon concurrent est rejoué par l'exécuteur.</summary>
    public async Task<Balance> GetOrCreateAsync(Guid userId, Guid leaveTypeId, CancellationToken ct)
    {
        var balance = await db.Balances.FirstOrDefaultAsync(b => b.UserId == userId && b.LeaveTypeId == leaveTypeId, ct);
        if (balance is null)
        {
            balance = Balance.CreateEmpty(Guid.CreateVersion7(), userId, leaveTypeId);
            db.Balances.Add(balance);
        }

        return balance;
    }

    /// <summary>Soldes des types soumis à décompte (CA-02.1, CA-02.2).</summary>
    public async Task<IReadOnlyList<BalanceDto>> GetBalancesAsync(Guid userId, CancellationToken ct)
    {
        var types = await db.LeaveTypes.AsNoTracking().Where(t => t.IsTracked).OrderBy(t => t.Code).ToListAsync(ct);
        var amounts = await db.Balances.AsNoTracking()
            .Where(b => b.UserId == userId)
            .ToDictionaryAsync(b => b.LeaveTypeId, b => b.AmountHundredths, ct);
        var pending = await db.LeaveRequests.AsNoTracking()
            .Where(r => r.RequesterId == userId && r.Status == LeaveRequestStatus.Pending)
            .GroupBy(r => r.LeaveTypeId)
            .Select(g => new { LeaveTypeId = g.Key, Days = g.Sum(r => r.DaysCount) })
            .ToDictionaryAsync(g => g.LeaveTypeId, g => g.Days, ct);

        return types.Select(t =>
        {
            var amount = amounts.GetValueOrDefault(t.Id);
            var pendingHundredths = DayAmount.FromDays(pending.GetValueOrDefault(t.Id));
            return new BalanceDto(
                Mapping.TypeRef(t),
                DayAmount.ToDecimal(amount),
                DayAmount.ToDecimal(pendingHundredths),
                DayAmount.ToDecimal(amount - pendingHundredths));
        }).ToList();
    }

    /// <summary>Relevé chronologique avec solde résultant (CA-16.5). 404 si type inconnu ou non soumis à décompte.</summary>
    public async Task<PagedResult<MovementDto>> GetMovementsAsync(Guid userId, Guid leaveTypeId, int page, int pageSize, CancellationToken ct)
    {
        var balanceId = await (
                from b in db.Balances.AsNoTracking()
                join t in db.LeaveTypes.AsNoTracking() on b.LeaveTypeId equals t.Id
                where b.UserId == userId && b.LeaveTypeId == leaveTypeId && t.IsTracked
                select (Guid?)b.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Aucun solde pour ce type.");

        var query =
            from m in db.BalanceMovements.AsNoTracking()
            join u in db.Users.AsNoTracking() on m.AuthorId equals u.Id
            where m.BalanceId == balanceId
            orderby m.Id
            select new { Movement = m, u.FirstName, u.LastName };
        var raw = await query.ToPageAsync(page, pageSize, ct);
        return new PagedResult<MovementDto>(
            raw.Items.Select(x => ToDto(x.Movement, Mapping.Person(x.Movement.AuthorId, x.FirstName, x.LastName))).ToList(),
            raw.Page,
            raw.PageSize,
            raw.TotalCount);
    }

    public static MovementDto ToDto(BalanceMovement m, PersonRef author) =>
        new(m.Id, m.Kind, DayAmount.ToDecimal(m.QuantityHundredths), DayAmount.ToDecimal(m.BalanceAfterHundredths), m.Reason, m.LeaveRequestId, author, m.OccurredAtUtc);
}
