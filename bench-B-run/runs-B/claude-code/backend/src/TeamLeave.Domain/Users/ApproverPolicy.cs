using TeamLeave.Domain.Common;

namespace TeamLeave.Domain.Users;

/// <summary>Approbateur d'une demande (RB-04, H-22, H-31).</summary>
public static class ApproverPolicy
{
    public static bool IsApprover(Guid callerId, bool callerIsHrAdmin, Guid requesterId, Guid? requesterManagerId)
    {
        if (callerId == requesterId)
        {
            return false;
        }

        return requesterManagerId is { } managerId
            ? managerId == callerId
            : callerIsHrAdmin;
    }

    /// <summary>Lève 403 SELF_APPROVAL_FORBIDDEN ou NOT_APPROVER si l'appelant ne peut pas décider.</summary>
    public static void EnsureCanDecide(Guid callerId, bool callerIsHrAdmin, Guid requesterId, Guid? requesterManagerId)
    {
        if (callerId == requesterId)
        {
            throw new ForbiddenException(ErrorCodes.SelfApprovalForbidden, "Vous ne pouvez pas décider de votre propre demande.");
        }

        if (!IsApprover(callerId, callerIsHrAdmin, requesterId, requesterManagerId))
        {
            throw new ForbiddenException(ErrorCodes.NotApprover, "Vous n'êtes pas l'approbateur de cette demande.");
        }
    }
}
