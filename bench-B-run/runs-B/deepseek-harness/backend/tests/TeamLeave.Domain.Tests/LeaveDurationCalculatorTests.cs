using TeamLeave.Domain.Enums;
using TeamLeave.Domain.Services;
using Xunit;
using static TeamLeave.Domain.Tests.TestData;

namespace TeamLeave.Domain.Tests;

public sealed class LeaveDurationCalculatorTests
{
    [Fact]
    public void Monday_am_to_monday_pm_is_one_day()
    {
        var result = LeaveDurationCalculator.Calculate(
            Date(2026, 11, 23), HalfDay.AM,
            Date(2026, 11, 23), HalfDay.PM);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.HalfDays);
        Assert.Equal(1.0m, result.HalfDays / 2m);
    }

    [Fact]
    public void Monday_pm_to_tuesday_am_is_one_day()
    {
        var result = LeaveDurationCalculator.Calculate(
            Date(2026, 11, 23), HalfDay.PM,
            Date(2026, 11, 24), HalfDay.AM);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.HalfDays);
    }

    [Fact]
    public void Monday_am_to_tuesday_pm_is_two_days()
    {
        var result = LeaveDurationCalculator.Calculate(
            Date(2026, 11, 23), HalfDay.AM,
            Date(2026, 11, 24), HalfDay.PM);

        Assert.True(result.IsValid);
        Assert.Equal(4, result.HalfDays);
    }

    [Fact]
    public void Friday_pm_to_monday_am_is_one_day()
    {
        var result = LeaveDurationCalculator.Calculate(
            Date(2026, 11, 27), HalfDay.PM,
            Date(2026, 11, 30), HalfDay.AM);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.HalfDays);
    }

    [Fact]
    public void Friday_pm_to_monday_pm_is_one_and_a_half_days()
    {
        var result = LeaveDurationCalculator.Calculate(
            Date(2026, 11, 27), HalfDay.PM,
            Date(2026, 11, 30), HalfDay.PM);

        Assert.True(result.IsValid);
        Assert.Equal(3, result.HalfDays);
    }

    [Fact]
    public void Weekend_dates_are_rejected()
    {
        var saturday = LeaveDurationCalculator.Calculate(
            Date(2026, 11, 28), HalfDay.AM,
            Date(2026, 11, 30), HalfDay.PM);

        var sunday = LeaveDurationCalculator.Calculate(
            Date(2026, 11, 29), HalfDay.AM,
            Date(2026, 11, 30), HalfDay.PM);

        Assert.False(saturday.IsValid);
        Assert.Equal("INVALID_DATES", saturday.ErrorCode);
        Assert.False(sunday.IsValid);
    }

    [Fact]
    public void Same_day_pm_to_am_is_rejected()
    {
        var result = LeaveDurationCalculator.Calculate(
            Date(2026, 11, 23), HalfDay.PM,
            Date(2026, 11, 23), HalfDay.AM);

        Assert.False(result.IsValid);
        Assert.Equal("INVALID_DATES", result.ErrorCode);
    }

    [Fact]
    public void End_before_start_is_rejected()
    {
        var result = LeaveDurationCalculator.Calculate(
            Date(2026, 11, 24), HalfDay.AM,
            Date(2026, 11, 23), HalfDay.PM);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Public_holiday_is_counted_as_a_working_day()
    {
        // Noël 2026 tombe un vendredi et n'est pas géré en v1.
        var result = LeaveDurationCalculator.Calculate(
            Date(2026, 12, 25), HalfDay.AM,
            Date(2026, 12, 25), HalfDay.PM);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.HalfDays);
    }

    [Fact]
    public void Leap_day_is_a_working_day()
    {
        var result = LeaveDurationCalculator.Calculate(
            Date(2028, 2, 29), HalfDay.AM,
            Date(2028, 2, 29), HalfDay.PM);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.HalfDays);
    }

    [Fact]
    public void Weekend_days_inside_a_period_are_excluded()
    {
        var result = LeaveDurationCalculator.Calculate(
            Date(2026, 11, 27), HalfDay.AM,
            Date(2026, 11, 30), HalfDay.PM);

        Assert.True(result.IsValid);
        Assert.Equal(4, result.HalfDays);
        Assert.All(result.Slots, slot => Assert.True(LeaveDurationCalculator.IsWeekday(slot.Date)));
    }
}
