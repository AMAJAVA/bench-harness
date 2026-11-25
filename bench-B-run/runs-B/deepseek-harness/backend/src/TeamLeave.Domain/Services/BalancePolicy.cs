using TeamLeave.Domain.ValueObjects;

namespace TeamLeave.Domain.Services;

/// <summary>RB-05/RB-19 : contrôles de solde, sans I/O.</summary>
public static class BalancePolicy
{
    public static IReadOnlyList<BalanceShortage> CheckSubmission(
        IReadOnlyDictionary<int, int> requestedHalfDaysByYear,
        IReadOnlyDictionary<int, int> availableHalfDaysByYear)
    {
        var shortages = new List<BalanceShortage>();
        foreach (var (year, requested) in requestedHalfDaysByYear.OrderBy(x => x.Key))
        {
            var available = availableHalfDaysByYear.GetValueOrDefault(year);
            if (available < requested)
            {
                shortages.Add(new BalanceShortage(year, requested, available));
            }
        }

        return shortages;
    }

    public static bool CanAdjust(int grantedHalfDays, int consumedHalfDays, int deltaHalfDays)
    {
        if (deltaHalfDays == 0)
        {
            return false;
        }

        var newGranted = grantedHalfDays + deltaHalfDays;
        return newGranted >= 0 && newGranted >= consumedHalfDays;
    }

    public static bool IsMultipleOfHalfDay(decimal days) =>
        days * 2m == decimal.Truncate(days * 2m);

    public static int ToHalfDays(decimal days) => checked((int)(days * 2m));

    public static decimal ToDays(int halfDays) => halfDays / 2m;
}
