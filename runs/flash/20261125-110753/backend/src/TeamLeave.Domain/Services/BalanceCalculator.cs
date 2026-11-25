namespace TeamLeave.Domain.Services;

public static class BalanceCalculator
{
    public static decimal Available(decimal acquired, decimal adjustment, decimal used)
        => acquired + adjustment - used;

    public static bool IsSufficient(decimal acquired, decimal adjustment, decimal used, int requestedDays)
        => Available(acquired, adjustment, used) >= requestedDays;

    public static bool WouldBeNegative(decimal acquired, decimal adjustment, decimal used)
        => Available(acquired, adjustment, used) < 0;
}
