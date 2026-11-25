namespace TeamLeave.Domain.ValueObjects;

public sealed record BalanceShortage(
    int Year,
    int RequestedHalfDays,
    int AvailableHalfDays)
{
    public int MissingHalfDays => Math.Max(0, RequestedHalfDays - AvailableHalfDays);
}
