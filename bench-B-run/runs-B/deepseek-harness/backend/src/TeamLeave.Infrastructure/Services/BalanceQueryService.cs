using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Contracts;
using TeamLeave.Infrastructure.Mapping;
using TeamLeave.Infrastructure.Persistence;

namespace TeamLeave.Infrastructure.Services;

public sealed class BalanceQueryService(TeamLeaveDbContext db)
{
    public async Task<BalancesResponse> GetAsync(
        Guid employeeId,
        int year,
        bool includeInactiveWithoutBalance,
        CancellationToken cancellationToken)
    {
        var types = await db.LeaveTypes
            .AsNoTracking()
            .OrderBy(t => t.Label)
            .ToListAsync(cancellationToken);

        var balances = await db.AnnualBalances
            .AsNoTracking()
            .Where(b => b.EmployeeId == employeeId && b.Year == year)
            .ToDictionaryAsync(
                b => b.LeaveTypeId,
                b => b,
                cancellationToken);

        var items = types
            .Where(t => includeInactiveWithoutBalance || t.IsActive || balances.ContainsKey(t.Id))
            .Select(t => ContractMapper.ToBalance(t, balances.GetValueOrDefault(t.Id)))
            .ToList();

        return new BalancesResponse(year, items);
    }

    public async Task<Dictionary<int, int>> GetAvailableHalfDaysAsync(
        Guid employeeId,
        Guid leaveTypeId,
        IReadOnlyCollection<int> years,
        CancellationToken cancellationToken)
    {
        var balances = await db.AnnualBalances
            .AsNoTracking()
            .Where(b => b.EmployeeId == employeeId &&
                        b.LeaveTypeId == leaveTypeId &&
                        years.Contains(b.Year))
            .ToDictionaryAsync(b => b.Year, b => b.AvailableHalfDays, cancellationToken);

        return years.ToDictionary(y => y, y => balances.GetValueOrDefault(y));
    }
}
