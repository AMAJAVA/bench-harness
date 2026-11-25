using TeamLeave.Domain.Common;

namespace TeamLeave.Domain.Leave;

public enum MovementKind
{
    Allocation,
    Adjustment,
    Debit,
    Recredit,
}

/// <summary>Solde d'un utilisateur pour un type soumis à décompte (RB-09 à RB-12, RB-25). Montant en centièmes.</summary>
public sealed class Balance : IVersioned
{
    private Balance()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid LeaveTypeId { get; private set; }

    public int AmountHundredths { get; private set; }

    public int Version { get; private set; }

    public static Balance CreateEmpty(Guid id, Guid userId, Guid leaveTypeId) =>
        new() { Id = id, UserId = userId, LeaveTypeId = leaveTypeId, AmountHundredths = 0 };

    /// <summary>Disponible = solde − jours en attente (RB-10).</summary>
    public int AvailableHundredths(int pendingHundredths) => AmountHundredths - pendingHundredths;

    /// <summary>Débit à l'approbation, garde-fou RB-12 : refus si le solde devient négatif.</summary>
    public BalanceMovement Debit(int days, Guid leaveRequestId, Guid authorId, DateTime nowUtc)
    {
        var quantity = DayAmount.FromDays(days);
        if (AmountHundredths < quantity)
        {
            throw new DomainException(ErrorCodes.InsufficientBalance, "Le solde est insuffisant pour approuver cette demande.");
        }

        return Apply(MovementKind.Debit, -quantity, null, leaveRequestId, authorId, nowUtc);
    }

    /// <summary>Recrédit à l'annulation d'une demande approuvée (RB-14).</summary>
    public BalanceMovement Recredit(int days, Guid leaveRequestId, Guid authorId, DateTime nowUtc) =>
        Apply(MovementKind.Recredit, DayAmount.FromDays(days), null, leaveRequestId, authorId, nowUtc);

    /// <summary>Allocation ou ajustement RH (RB-11, RB-25) : le disponible ne peut pas devenir négatif.</summary>
    public BalanceMovement ApplyHrMovement(
        MovementKind kind,
        int quantityHundredths,
        string reason,
        int pendingHundredths,
        Guid authorId,
        DateTime nowUtc)
    {
        if (kind is not (MovementKind.Allocation or MovementKind.Adjustment))
        {
            throw new ValidationException("kind", "Seuls les mouvements Allocation et Adjustment sont autorisés.");
        }

        if (quantityHundredths == 0 || (kind == MovementKind.Allocation && quantityHundredths < 0))
        {
            throw new ValidationException("quantity", "La quantité doit être non nulle, et positive pour une allocation.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationException("reason", "Le motif est obligatoire.");
        }

        var newAmount = AmountHundredths + quantityHundredths;
        if (newAmount < 0 || newAmount - pendingHundredths < 0)
        {
            throw new DomainException(
                ErrorCodes.BalanceBelowPending,
                "Le mouvement rendrait le solde ou le disponible négatif (jours en attente compris).");
        }

        return Apply(kind, quantityHundredths, reason.Trim(), null, authorId, nowUtc);
    }

    public void Touch() => Version++;

    private BalanceMovement Apply(MovementKind kind, int quantity, string? reason, Guid? leaveRequestId, Guid authorId, DateTime nowUtc)
    {
        AmountHundredths += quantity;
        return BalanceMovement.Create(Id, kind, quantity, AmountHundredths, reason, leaveRequestId, authorId, nowUtc);
    }
}

/// <summary>Mouvement de solde, en ajout seul (RB-25, RB-26).</summary>
public sealed class BalanceMovement
{
    private BalanceMovement()
    {
    }

    public long Id { get; private set; }

    public Guid BalanceId { get; private set; }

    public MovementKind Kind { get; private set; }

    public int QuantityHundredths { get; private set; }

    public int BalanceAfterHundredths { get; private set; }

    public string? Reason { get; private set; }

    public Guid? LeaveRequestId { get; private set; }

    public Guid AuthorId { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    internal static BalanceMovement Create(
        Guid balanceId,
        MovementKind kind,
        int quantity,
        int balanceAfter,
        string? reason,
        Guid? leaveRequestId,
        Guid authorId,
        DateTime nowUtc) =>
        new()
        {
            BalanceId = balanceId,
            Kind = kind,
            QuantityHundredths = quantity,
            BalanceAfterHundredths = balanceAfter,
            Reason = reason,
            LeaveRequestId = leaveRequestId,
            AuthorId = authorId,
            OccurredAtUtc = nowUtc,
        };
}
