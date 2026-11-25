namespace TeamLeave.Domain.Common;

/// <summary>Période de journées entières, bornes incluses (RB-05).</summary>
public readonly record struct DateRange
{
    public const int MaxCalendarDays = 366;

    public DateRange(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new ValidationException("endDate", "La date de fin doit être postérieure ou égale à la date de début.");
        }

        Start = start;
        End = end;
    }

    public DateOnly Start { get; }

    public DateOnly End { get; }

    public int CalendarDays => End.DayNumber - Start.DayNumber + 1;

    public bool Overlaps(DateOnly otherStart, DateOnly otherEnd) => Start <= otherEnd && otherStart <= End;

    public bool Contains(DateOnly day) => Start <= day && day <= End;

    public IEnumerable<DateOnly> Days()
    {
        for (var d = Start; d <= End; d = d.AddDays(1))
        {
            yield return d;
        }
    }
}
