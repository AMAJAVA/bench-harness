using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Exceptions;
using TeamLeave.Application.Interfaces;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Services;

namespace TeamLeave.Application.Services;

public class BalanceService
{
    private readonly ITeamLeaveDbContext _db;
    private readonly IClock _clock;

    public BalanceService(ITeamLeaveDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<BalanceDto>> GetBalancesAsync(Guid userId, int year, CancellationToken cancellationToken = default)
    {
        var userExists = await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists) throw new AppException(404, "USER_NOT_FOUND", "Utilisateur introuvable.");

        var leaveTypes = await _db.LeaveTypes
            .Where(t => t.IsActive && t.TrackBalance)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync(cancellationToken);

        var balances = await _db.LeaveBalances
            .Where(b => b.UserId == userId && b.Year == year)
            .ToDictionaryAsync(b => b.LeaveTypeId, cancellationToken);

        var result = new List<BalanceDto>();
        foreach (var type in leaveTypes)
        {
            if (balances.TryGetValue(type.Id, out var balance))
            {
                result.Add(new BalanceDto(
                    type.Id,
                    type.Code,
                    type.Label,
                    year,
                    balance.AcquiredDays,
                    balance.AdjustmentDays,
                    balance.UsedDays,
                    balance.AvailableDays));
            }
            else
            {
                result.Add(new BalanceDto(type.Id, type.Code, type.Label, year, 0, 0, 0, 0));
            }
        }

        return result;
    }

    public async Task AdjustBalanceAsync(Guid userId, Guid leaveTypeId, AdjustmentRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        _ = await _db.Users.FindAsync(new object?[] { userId }, cancellationToken)
            ?? throw new AppException(404, "USER_NOT_FOUND", "Utilisateur introuvable.");
        var leaveType = await _db.LeaveTypes.FindAsync(new object?[] { leaveTypeId }, cancellationToken)
            ?? throw new AppException(404, "LEAVE_TYPE_NOT_FOUND", "Type de congé introuvable.");
        if (!leaveType.IsActive) throw new AppException(422, "LEAVE_TYPE_INACTIVE", "Le type de congé est inactif.");
        if (!leaveType.TrackBalance) throw new AppException(422, "LEAVE_TYPE_NOT_TRACKED", "Ce type de congé ne suit pas de solde.");

        var balance = await _db.LeaveBalances
            .SingleOrDefaultAsync(b => b.UserId == userId && b.LeaveTypeId == leaveTypeId && b.Year == request.Year, cancellationToken);

        decimal before = 0;
        if (balance == null)
        {
            balance = new LeaveBalance
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LeaveTypeId = leaveTypeId,
                Year = request.Year,
                AcquiredDays = 0,
                AdjustmentDays = 0,
                UsedDays = 0,
                Version = 0
            };
            _db.LeaveBalances.Add(balance);
        }
        else
        {
            before = balance.AvailableDays;
        }

        var newAdjustment = balance.AdjustmentDays + request.AdjustmentDays;
        var available = BalanceCalculator.Available(balance.AcquiredDays, newAdjustment, balance.UsedDays);
        if (available < 0)
        {
            throw new AppException(409, "BALANCE_NEGATIVE", $"L'ajustement rendrait le solde négatif (disponible actuel: {before}, après: {available}).");
        }

        balance.AdjustmentDays = newAdjustment;
        balance.Version++;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = "BALANCE_ADJUSTMENT",
            EntityType = nameof(LeaveBalance),
            EntityId = balance.Id,
            OccurredAt = _clock.UtcNow,
            Details = $"UserId={userId};LeaveTypeId={leaveTypeId};Year={request.Year};Adjustment={request.AdjustmentDays};Before={before};After={available}",
            Reason = request.Reason
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetBalanceAsync(Guid userId, Guid leaveTypeId, int year, SetBalanceRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        _ = await _db.Users.FindAsync(new object?[] { userId }, cancellationToken)
            ?? throw new AppException(404, "USER_NOT_FOUND", "Utilisateur introuvable.");
        var leaveType = await _db.LeaveTypes.FindAsync(new object?[] { leaveTypeId }, cancellationToken)
            ?? throw new AppException(404, "LEAVE_TYPE_NOT_FOUND", "Type de congé introuvable.");
        if (!leaveType.IsActive) throw new AppException(422, "LEAVE_TYPE_INACTIVE", "Le type de congé est inactif.");
        if (!leaveType.TrackBalance) throw new AppException(422, "LEAVE_TYPE_NOT_TRACKED", "Ce type de congé ne suit pas de solde.");

        var balance = await _db.LeaveBalances
            .SingleOrDefaultAsync(b => b.UserId == userId && b.LeaveTypeId == leaveTypeId && b.Year == year, cancellationToken);

        decimal before = 0;
        if (balance == null)
        {
            balance = new LeaveBalance
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LeaveTypeId = leaveTypeId,
                Year = year,
                AcquiredDays = 0,
                AdjustmentDays = 0,
                UsedDays = 0,
                Version = 0
            };
            _db.LeaveBalances.Add(balance);
        }
        else
        {
            before = balance.AvailableDays;
        }

        var available = BalanceCalculator.Available(request.AcquiredDays, request.AdjustmentDays, balance.UsedDays);
        if (available < 0)
        {
            throw new AppException(409, "BALANCE_NEGATIVE", $"Le solde résultant serait négatif (disponible actuel: {before}, après: {available}).");
        }

        balance.AcquiredDays = request.AcquiredDays;
        balance.AdjustmentDays = request.AdjustmentDays;
        balance.Version++;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = "BALANCE_SET",
            EntityType = nameof(LeaveBalance),
            EntityId = balance.Id,
            OccurredAt = _clock.UtcNow,
            Details = $"UserId={userId};LeaveTypeId={leaveTypeId};Year={year};Acquired={request.AcquiredDays};Adjustment={request.AdjustmentDays};Before={before};After={available}"
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
