using FluentAssertions;
using TeamLeave.Domain.Services;
using Xunit;

namespace TeamLeave.Tests.DomainTests;

public class BalanceCalculatorTests
{
    [Fact]
    public void Available_ShouldComputeAcquiredPlusAdjustmentMinusUsed()
    {
        BalanceCalculator.Available(20, 2, 5).Should().Be(17);
    }

    [Theory]
    [InlineData(20, 0, 5, 15, true)]
    [InlineData(20, 0, 5, 16, false)]
    [InlineData(0, 0, 0, 0, true)]
    public void IsSufficient_ShouldReturnExpected(decimal acquired, decimal adjustment, decimal used, int requested, bool expected)
    {
        BalanceCalculator.IsSufficient(acquired, adjustment, used, requested).Should().Be(expected);
    }

    [Fact]
    public void WouldBeNegative_ShouldReturnTrueWhenAvailableNegative()
    {
        BalanceCalculator.WouldBeNegative(10, 0, 11).Should().BeTrue();
        BalanceCalculator.WouldBeNegative(10, 0, 10).Should().BeFalse();
    }
}
