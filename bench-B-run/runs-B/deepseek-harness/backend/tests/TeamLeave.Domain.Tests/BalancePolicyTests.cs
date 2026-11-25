using TeamLeave.Domain.Services;
using Xunit;

namespace TeamLeave.Domain.Tests;

public sealed class BalancePolicyTests
{
    [Fact]
    public void Exact_available_balance_is_sufficient()
    {
        var shortages = BalancePolicy.CheckSubmission(
            new Dictionary<int, int> { [2026] = 6 },
            new Dictionary<int, int> { [2026] = 6 });

        Assert.Empty(shortages);
    }

    [Fact]
    public void Insufficient_balance_returns_missing_half_days()
    {
        var shortages = BalancePolicy.CheckSubmission(
            new Dictionary<int, int> { [2026] = 6 },
            new Dictionary<int, int> { [2026] = 3 });

        var shortage = Assert.Single(shortages);
        Assert.Equal(2026, shortage.Year);
        Assert.Equal(6, shortage.RequestedHalfDays);
        Assert.Equal(3, shortage.AvailableHalfDays);
        Assert.Equal(3, shortage.MissingHalfDays);
    }

    [Fact]
    public void Unallocated_year_is_treated_as_zero()
    {
        var shortages = BalancePolicy.CheckSubmission(
            new Dictionary<int, int> { [2026] = 2, [2027] = 2 },
            new Dictionary<int, int> { [2026] = 2 });

        var shortage = Assert.Single(shortages);
        Assert.Equal(2027, shortage.Year);
        Assert.Equal(2, shortage.MissingHalfDays);
    }

    [Fact]
    public void Negative_adjustment_is_allowed_when_available_stays_non_negative()
    {
        Assert.True(BalancePolicy.CanAdjust(40, 7, -2));
        Assert.True(BalancePolicy.CanAdjust(40, 7, -33));
    }

    [Fact]
    public void Negative_adjustment_below_consumed_is_refused()
    {
        Assert.False(BalancePolicy.CanAdjust(20, 7, -14));
    }

    [Fact]
    public void Zero_adjustment_is_refused()
    {
        Assert.False(BalancePolicy.CanAdjust(20, 7, 0));
    }

    [Theory]
    [InlineData(0.5, true)]
    [InlineData(1.0, true)]
    [InlineData(1.5, true)]
    [InlineData(1.25, false)]
    [InlineData(-1.0, true)]
    public void Half_day_step_is_enforced(decimal days, bool expected)
    {
        Assert.Equal(expected, BalancePolicy.IsMultipleOfHalfDay(days));
    }
}
