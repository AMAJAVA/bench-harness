using TeamLeave.Domain.Calendar;
using TeamLeave.Domain.Common;

namespace TeamLeave.Domain.Tests;

[Trait("Rule", "RB-06")]
[Trait("Rule", "RB-07")]
public sealed class WorkingDayCalculatorTests
{
    private static readonly HashSet<DateOnly> NoHolidays = [];

    [Fact]
    public void CA_03_1_friday_to_tuesday_counts_three_days()
    {
        var days = WorkingDayCalculator.WorkingDays(new DateRange(new(2026, 3, 6), new(2026, 3, 10)), NoHolidays);

        Assert.Equal([new(2026, 3, 6), new(2026, 3, 9), new(2026, 3, 10)], days);
    }

    [Fact]
    public void CA_03_2_easter_monday_holiday_is_not_counted()
    {
        var holidays = new HashSet<DateOnly> { new(2026, 4, 6) };

        var days = WorkingDayCalculator.WorkingDays(new DateRange(new(2026, 4, 6), new(2026, 4, 8)), holidays);

        Assert.Equal(2, days.Count);
    }

    [Fact]
    public void CA_03_3_weekend_has_no_working_day()
    {
        var days = WorkingDayCalculator.WorkingDays(new DateRange(new(2026, 3, 7), new(2026, 3, 8)), NoHolidays);

        Assert.Empty(days);
    }

    [Fact]
    public void CA_18_1_may_first_holiday_leaves_one_day()
    {
        var holidays = new HashSet<DateOnly> { new(2026, 5, 1) };

        var days = WorkingDayCalculator.WorkingDays(new DateRange(new(2026, 4, 30), new(2026, 5, 1)), holidays);

        Assert.Equal([new DateOnly(2026, 4, 30)], days);
    }

    [Fact]
    public void Single_day_period_on_a_weekday_counts_one()
    {
        var day = new DateOnly(2026, 3, 2);

        Assert.Single(WorkingDayCalculator.WorkingDays(new DateRange(day, day), NoHolidays));
    }

    [Fact]
    public void End_before_start_is_a_validation_error()
    {
        var ex = Assert.Throws<ValidationException>(() => new DateRange(new(2026, 3, 10), new(2026, 3, 9)));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
        Assert.True(ex.Errors.ContainsKey("endDate"));
    }

    [Theory]
    [InlineData(2026, 4, 5)]
    [InlineData(2027, 3, 28)]
    [InlineData(2024, 3, 31)]
    public void Easter_sunday_is_computed(int year, int month, int day)
    {
        Assert.Equal(new DateOnly(year, month, day), FrenchPublicHolidays.EasterSunday(year));
    }

    [Fact]
    public void French_holidays_contain_eleven_days()
    {
        var holidays = FrenchPublicHolidays.For(2026);

        Assert.Equal(11, holidays.Count);
        Assert.Contains(holidays, h => h.Date == new DateOnly(2026, 4, 6));
        Assert.Contains(holidays, h => h.Date == new DateOnly(2026, 5, 14));
        Assert.Contains(holidays, h => h.Date == new DateOnly(2026, 5, 25));
    }
}
