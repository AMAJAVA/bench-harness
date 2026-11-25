using TeamLeave.Domain.Enums;
using TeamLeave.Domain.ValueObjects;

namespace TeamLeave.Domain.Services;

/// <summary>RB-14 : annulation par le titulaire, jamais après le début d'un congé approuvé.</summary>
public static class CancellationPolicy
{
    public static CancellationOutcome Evaluate(
        Guid requesterId,
        Guid callerId,
        LeaveStatus status,
        DateOnly startDate,
        DateOnly today)
    {
        if (requesterId != callerId)
        {
            return CancellationOutcome.Refuse("NOT_FOUND", 404);
        }

        return status switch
        {
            LeaveStatus.PENDING => CancellationOutcome.Allow(restoreBalance: false),
            LeaveStatus.APPROVED when startDate > today => CancellationOutcome.Allow(restoreBalance: true),
            LeaveStatus.APPROVED => CancellationOutcome.Refuse("INVALID_STATE", 409),
            _ => CancellationOutcome.Refuse("INVALID_STATE", 409)
        };
    }
}
