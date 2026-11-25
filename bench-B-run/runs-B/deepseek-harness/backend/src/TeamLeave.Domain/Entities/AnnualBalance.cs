using TeamLeave.Domain.Exceptions;

namespace TeamLeave.Domain.Entities;

public sealed class AnnualBalance
{
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int Year { get; set; }
    public int GrantedHalfDays { get; set; }
    public int ConsumedHalfDays { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public int AvailableHalfDays => GrantedHalfDays - ConsumedHalfDays;

    public void Consume(int halfDays)
    {
        if (halfDays <= 0)
        {
            throw new DomainException("INVALID_DAYS", "Le nombre de demi-journées doit être strictement positif.");
        }

        if (ConsumedHalfDays + halfDays > GrantedHalfDays)
        {
            throw new DomainException("INSUFFICIENT_BALANCE_AT_APPROVAL", "Le solde disponible est insuffisant.");
        }

        ConsumedHalfDays += halfDays;
        Version++;
    }

    public void Restore(int halfDays)
    {
        if (halfDays <= 0)
        {
            throw new DomainException("INVALID_DAYS", "Le nombre de demi-journées doit être strictement positif.");
        }

        if (ConsumedHalfDays - halfDays < 0)
        {
            throw new DomainException("INVALID_STATE", "La restitution dépasserait la consommation.");
        }

        ConsumedHalfDays -= halfDays;
        Version++;
    }

    public void AdjustGranted(int deltaHalfDays)
    {
        if (deltaHalfDays == 0)
        {
            throw new DomainException("INVALID_DAYS_STEP", "L'ajustement ne peut pas être nul.");
        }

        var newGranted = GrantedHalfDays + deltaHalfDays;
        if (newGranted < 0 || newGranted < ConsumedHalfDays)
        {
            throw new DomainException("BALANCE_WOULD_BE_NEGATIVE", "Le solde ne peut pas devenir négatif.");
        }

        GrantedHalfDays = newGranted;
        Version++;
    }
}
