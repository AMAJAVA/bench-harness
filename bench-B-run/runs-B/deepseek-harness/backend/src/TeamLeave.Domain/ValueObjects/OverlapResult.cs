namespace TeamLeave.Domain.ValueObjects;

public sealed record OverlapResult(
    bool Applicable,
    bool Active,
    int TeamSize,
    decimal? MaxRate,
    IReadOnlyList<LeaveHalfDay> BreachingHalfDays)
{
    public static OverlapResult NotApplicable(int teamSize) =>
        new(false, false, teamSize, null, Array.Empty<LeaveHalfDay>());

    public static OverlapResult None(int teamSize) =>
        new(true, false, teamSize, 0m, Array.Empty<LeaveHalfDay>());
}
