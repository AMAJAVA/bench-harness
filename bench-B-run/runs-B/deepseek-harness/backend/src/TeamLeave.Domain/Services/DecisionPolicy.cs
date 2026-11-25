using TeamLeave.Domain.Enums;
using TeamLeave.Domain.ValueObjects;

namespace TeamLeave.Domain.Services;

/// <summary>RB-09 à RB-13 : autorité de décision, statut, commentaire et acquittement.</summary>
public static class DecisionPolicy
{
    public static DecisionOutcome Evaluate(
        Guid requesterId,
        Guid callerId,
        Guid? currentManagerId,
        bool callerHasManagerRole,
        bool callerHasAdminRole,
        LeaveStatus status,
        bool overlapActive,
        bool overlapAcknowledged,
        bool isRejection,
        string? comment)
    {
        // CA-07.8 : l'auto-décision est contrôlée avant tout contrôle de périmètre.
        if (requesterId == callerId)
        {
            return DecisionOutcome.Refuse("FORBIDDEN", 403);
        }

        if (currentManagerId != callerId)
        {
            // Un Admin RH qui n'est pas manager direct reçoit 403 ; un manager hors périmètre 404.
            if (callerHasAdminRole && !callerHasManagerRole)
            {
                return DecisionOutcome.Refuse("FORBIDDEN", 403);
            }

            return callerHasManagerRole
                ? DecisionOutcome.Refuse("NOT_FOUND", 404)
                : DecisionOutcome.Refuse("FORBIDDEN", 403);
        }

        if (status != LeaveStatus.PENDING)
        {
            return DecisionOutcome.Refuse("INVALID_STATE", 409);
        }

        if (isRejection && string.IsNullOrWhiteSpace(comment))
        {
            return DecisionOutcome.Refuse("COMMENT_REQUIRED", 422);
        }

        if (!isRejection && overlapActive && !overlapAcknowledged)
        {
            return DecisionOutcome.Refuse("OVERLAP_ACK_REQUIRED", 409);
        }

        return DecisionOutcome.Allow();
    }
}
