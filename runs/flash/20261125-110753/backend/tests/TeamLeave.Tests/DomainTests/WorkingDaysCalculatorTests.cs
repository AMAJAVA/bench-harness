using FluentAssertions;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Services;
using Xunit;

namespace TeamLeave.Tests.DomainTests;

public class WorkingDaysCalculatorTests
{
    [Fact]
    public void Calculate_WithoutHolidays_ShouldCountWeekdays()
    {
        var start = new DateOnly(2025, 7, 14); // Monday
        var end = new DateOnly(2025, 7, 18); // Friday
        WorkingDaysCalculator.Calculate(start, end, Enumerable.Empty<Holiday>()).Should().Be(5);
    }

    [Fact]
    public void Calculate_ShouldExcludeWeekend()
    {
        var start = new DateOnly(2025, 7, 18); // Friday
        var end = new DateOnly(2025, 7, 21); // Monday
        WorkingDaysCalculator.Calculate(start, end, Enumerable.Empty<Holiday>()).Should().Be(2);
    }

    [Fact]
    public void Calculate_ShouldExcludeActiveHolidays()
    {
        var start = new DateOnly(2025, 7, 14);
        var end = new DateOnly(2025, 7, 18);
        var holidays = new[]
        {
            new Holiday { Date = new DateOnly(2025, 7, 15), IsActive = true },
            new Holiday { Date = new DateOnly(2025, 7, 16), IsActive = false }
        };
        WorkingDaysCalculator.Calculate(start, end, holidays).Should().Be(4);
    }
}
