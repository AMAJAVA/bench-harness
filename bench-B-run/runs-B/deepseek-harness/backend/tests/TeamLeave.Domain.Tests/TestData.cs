using TeamLeave.Domain.Enums;
using TeamLeave.Domain.ValueObjects;

namespace TeamLeave.Domain.Tests;

internal static class TestData
{
    public static DateOnly Date(int year, int month, int day) => new(year, month, day);

    public static LeaveHalfDay Am(DateOnly date) => new(date, HalfDay.AM);
    public static LeaveHalfDay Pm(DateOnly date) => new(date, HalfDay.PM);

    public static IReadOnlyList<LeaveHalfDay> FullDay(DateOnly date) =>
        [Am(date), Pm(date)];

    public static IReadOnlyList<LeaveHalfDay> Days(params DateOnly[] dates) =>
        dates.SelectMany(FullDay).ToList();

    public static ApprovedAbsence Absence(Guid employeeId, params LeaveHalfDay[] halfDays) =>
        new(employeeId, halfDays);
}
