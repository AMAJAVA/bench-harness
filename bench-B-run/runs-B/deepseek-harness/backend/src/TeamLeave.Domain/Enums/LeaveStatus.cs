namespace TeamLeave.Domain.Enums;

/// <summary>Statuts d'une demande. Les noms sont volontairement identiques au contrat API.</summary>
public enum LeaveStatus
{
    PENDING = 0,
    APPROVED = 1,
    REJECTED = 2,
    CANCELLED = 3
}
