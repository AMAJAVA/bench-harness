using TeamLeave.Domain.Enums;
using TeamLeave.Domain.Services;
using Xunit;
using static TeamLeave.Domain.Tests.TestData;

namespace TeamLeave.Domain.Tests;

public sealed class CancellationPolicyTests
{
    private static readonly Guid Employee = Guid.NewGuid();
    private static readonly DateOnly Today = Date(2026, 11, 25);

    [Fact]
    public void Pending_request_can_be_cancelled_without_balance_impact()
    {
        var outcome = CancellationPolicy.Evaluate(Employee, Employee, LeaveStatus.PENDING, Today, Today);

        Assert.True(outcome.Allowed);
        Assert.False(outcome.RestoreBalance);
    }

    [Fact]
    public void Future_approved_request_can_be_cancelled_with_balance_restoration()
    {
        var outcome = CancellationPolicy.Evaluate(
            Employee, Employee, LeaveStatus.APPROVED, Today.AddDays(1), Today);

        Assert.True(outcome.Allowed);
        Assert.True(outcome.RestoreBalance);
    }

    [Fact]
    public void Approved_request_starting_today_cannot_be_cancelled()
    {
        var outcome = CancellationPolicy.Evaluate(
            Employee, Employee, LeaveStatus.APPROVED, Today, Today);

        Assert.False(outcome.Allowed);
        Assert.Equal("INVALID_STATE", outcome.Code);
        Assert.Equal(409, outcome.HttpStatus);
    }

    [Fact]
    public void Approved_request_started_in_the_past_cannot_be_cancelled()
    {
        var outcome = CancellationPolicy.Evaluate(
            Employee, Employee, LeaveStatus.APPROVED, Today.AddDays(-1), Today);

        Assert.False(outcome.Allowed);
    }

    [Theory]
    [InlineData(LeaveStatus.REJECTED)]
    [InlineData(LeaveStatus.CANCELLED)]
    public void Terminal_statuses_cannot_be_cancelled(LeaveStatus status)
    {
        var outcome = CancellationPolicy.Evaluate(Employee, Employee, status, Today.AddDays(1), Today);

        Assert.False(outcome.Allowed);
        Assert.Equal("INVALID_STATE", outcome.Code);
    }

    [Fact]
    public void Another_employee_gets_not_found()
    {
        var outcome = CancellationPolicy.Evaluate(
            Employee, Guid.NewGuid(), LeaveStatus.PENDING, Today, Today);

        Assert.False(outcome.Allowed);
        Assert.Equal("NOT_FOUND", outcome.Code);
        Assert.Equal(404, outcome.HttpStatus);
    }
}
