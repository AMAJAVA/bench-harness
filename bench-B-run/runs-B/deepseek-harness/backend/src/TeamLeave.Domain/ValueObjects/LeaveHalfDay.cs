using TeamLeave.Domain.Enums;

namespace TeamLeave.Domain.ValueObjects;

/// <summary>Une demi-journée civile : une date et AM/PM. L'ordre est stable et total.</summary>
public readonly record struct LeaveHalfDay(DateOnly Date, HalfDay HalfDay) : IComparable<LeaveHalfDay>
{
    public int Ordinal => Date.DayNumber * 2 + (HalfDay == HalfDay.PM ? 1 : 0);

    public static LeaveHalfDay FromOrdinal(int ordinal) =>
        new(DateOnly.FromDayNumber(ordinal / 2), ordinal % 2 == 0 ? HalfDay.AM : HalfDay.PM);

    public int CompareTo(LeaveHalfDay other) => Ordinal.CompareTo(other.Ordinal);

    public override string ToString() => $"{Date:yyyy-MM-dd} {HalfDay}";
}
