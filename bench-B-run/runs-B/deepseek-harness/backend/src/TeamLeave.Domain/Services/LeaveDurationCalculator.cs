using TeamLeave.Domain.Enums;
using TeamLeave.Domain.ValueObjects;

namespace TeamLeave.Domain.Services;

public sealed record LeaveDurationResult(
    bool IsValid,
    string? ErrorCode,
    string? ErrorDetail,
    int HalfDays,
    IReadOnlyList<LeaveHalfDay> Slots)
{
    public static LeaveDurationResult Invalid(string code, string detail) =>
        new(false, code, detail, 0, Array.Empty<LeaveHalfDay>());

    public static LeaveDurationResult Valid(int halfDays, IReadOnlyList<LeaveHalfDay> slots) =>
        new(true, null, null, halfDays, slots);
}

/// <summary>RB-06 : décompte des jours ouvrés à la demi-journée, week-ends exclus.</summary>
public static class LeaveDurationCalculator
{
    public static LeaveDurationResult Calculate(
        DateOnly startDate,
        HalfDay startHalfDay,
        DateOnly endDate,
        HalfDay endHalfDay)
    {
        if (!IsWeekday(startDate) || !IsWeekday(endDate))
        {
            return LeaveDurationResult.Invalid("INVALID_DATES", "Les dates de début et de fin doivent être des jours ouvrés.");
        }

        var start = new LeaveHalfDay(startDate, startHalfDay);
        var end = new LeaveHalfDay(endDate, endHalfDay);

        if (end.Ordinal < start.Ordinal)
        {
            return LeaveDurationResult.Invalid("INVALID_DATES", "La date de fin doit être postérieure ou égale à la date de début.");
        }

        var slots = EnumerateSlots(start.Ordinal, end.Ordinal);
        if (slots.Count == 0)
        {
            return LeaveDurationResult.Invalid("INVALID_DATES", "La demande ne contient aucune demi-journée ouvrée.");
        }

        return LeaveDurationResult.Valid(slots.Count, slots);
    }

    public static IReadOnlyList<LeaveHalfDay> EnumerateSlots(int startOrdinal, int endOrdinal)
    {
        var result = new List<LeaveHalfDay>();
        for (var ordinal = startOrdinal; ordinal <= endOrdinal; ordinal++)
        {
            var slot = LeaveHalfDay.FromOrdinal(ordinal);
            if (IsWeekday(slot.Date))
            {
                result.Add(slot);
            }
        }

        return result;
    }

    public static bool IsWeekday(DateOnly date) =>
        date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);
}
