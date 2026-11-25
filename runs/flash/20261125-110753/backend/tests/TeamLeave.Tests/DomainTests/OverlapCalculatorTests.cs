using FluentAssertions;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;
using TeamLeave.Domain.Services;
using Xunit;

namespace TeamLeave.Tests.DomainTests;

public class OverlapCalculatorTests
{
    private static LeaveRequest CreateApproved(Guid employeeId, Guid teamId, DateOnly start, DateOnly end)
    {
        return new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TeamId = teamId,
            StartDate = start,
            EndDate = end,
            Status = LeaveRequestStatus.Approved,
            Employee = new User { FirstName = "A", LastName = "B" }
        };
    }

    [Fact]
    public void Calculate_WhenOverlapIs60Percent_ShouldWarn()
    {
        var employeeId = Guid.NewGuid();
        var otherEmployeeId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var start = new DateOnly(2025, 7, 14);
        var end = new DateOnly(2025, 7, 18);
        var approved = new[]
        {
            CreateApproved(otherEmployeeId, teamId, new DateOnly(2025, 7, 15), new DateOnly(2025, 7, 17))
        };
        var result = OverlapCalculator.Calculate(start, end, employeeId, teamId, approved, Enumerable.Empty<Holiday>());
        result.WorkingDays.Should().Be(5);
        result.OverlapDays.Should().Be(3);
        result.Ratio.Should().Be(0.6m);
        result.HasWarning.Should().BeTrue();
    }

    [Fact]
    public void Calculate_WhenOverlapIsExactly50Percent_ShouldNotWarn()
    {
        var employeeId = Guid.NewGuid();
        var otherEmployeeId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var start = new DateOnly(2025, 7, 14);
        var end = new DateOnly(2025, 7, 17);
        var approved = new[]
        {
            CreateApproved(otherEmployeeId, teamId, new DateOnly(2025, 7, 14), new DateOnly(2025, 7, 15))
        };
        var result = OverlapCalculator.Calculate(start, end, employeeId, teamId, approved, Enumerable.Empty<Holiday>());
        result.WorkingDays.Should().Be(4);
        result.OverlapDays.Should().Be(2);
        result.Ratio.Should().Be(0.5m);
        result.HasWarning.Should().BeFalse();
    }

    [Fact]
    public void Calculate_ShouldIgnoreOwnLeave()
    {
        var employeeId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var start = new DateOnly(2025, 7, 14);
        var end = new DateOnly(2025, 7, 18);
        var approved = new[]
        {
            CreateApproved(employeeId, teamId, start, end)
        };
        var result = OverlapCalculator.Calculate(start, end, employeeId, teamId, approved, Enumerable.Empty<Holiday>());
        result.OverlapDays.Should().Be(0);
        result.HasWarning.Should().BeFalse();
    }

    [Fact]
    public void Calculate_ShouldIgnorePendingAndOtherTeam()
    {
        var employeeId = Guid.NewGuid();
        var otherEmployeeId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var start = new DateOnly(2025, 7, 14);
        var end = new DateOnly(2025, 7, 18);
        var pending = new LeaveRequest
        {
            EmployeeId = otherEmployeeId,
            TeamId = teamId,
            StartDate = start,
            EndDate = end,
            Status = LeaveRequestStatus.Pending,
            Employee = new User { FirstName = "P", LastName = "Pending" }
        };
        var otherTeam = CreateApproved(otherEmployeeId, otherTeamId, start, end);
        var result = OverlapCalculator.Calculate(start, end, employeeId, teamId, new[] { pending, otherTeam }, Enumerable.Empty<Holiday>());
        result.OverlapDays.Should().Be(0);
        result.HasWarning.Should().BeFalse();
    }
}
