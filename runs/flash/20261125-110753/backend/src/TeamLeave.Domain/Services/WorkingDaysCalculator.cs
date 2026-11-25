using TeamLeave.Domain.Entities;

namespace TeamLeave.Domain.Services;

public static class WorkingDaysCalculator
{
    public static int Calculate(DateOnly start, DateOnly end, IEnumerable<Holiday> holidays)
    {
        if (start > end) return 0;
        var holidaySet = holidays.Where(h => h.IsActive).Select(h => h.Date).ToHashSet();
        var count = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (holidaySet.Contains(date)) continue;
            count++;
        }
        return count;
    }

    public static IEnumerable<DateOnly> GetWorkingDays(DateOnly start, DateOnly end, IEnumerable<Holiday> holidays)
    {
        if (start > end) yield break;
        var holidaySet = holidays.Where(h => h.IsActive).Select(h => h.Date).ToHashSet();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (holidaySet.Contains(date)) continue;
            yield return date;
        }
    }
}
