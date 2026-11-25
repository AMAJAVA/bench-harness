using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;
using TeamLeave.Domain.Exceptions;
using Xunit;

namespace TeamLeave.Domain.Tests;

public sealed class LeaveRequestStateTests
{
    private static LeaveRequest Pending() => new()
    {
        EmployeeId = Guid.NewGuid(),
        LeaveTypeId = Guid.NewGuid(),
        StartDate = new DateOnly(2026, 12, 7),
        StartHalfDay = HalfDay.AM,
        EndDate = new DateOnly(2026, 12, 8),
        EndHalfDay = HalfDay.PM,
        TotalHalfDays = 4,
        Status = LeaveStatus.PENDING,
        SubmittedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public void Approval_sets_decider_status_and_acknowledgement()
    {
        var request = Pending();
        var managerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        request.Approve(managerId, now, "Bon congé", overlapActive: true, overlapAcknowledged: true);

        Assert.Equal(LeaveStatus.APPROVED, request.Status);
        Assert.Equal(managerId, request.DecidedById);
        Assert.Equal("Bon congé", request.DecisionComment);
        Assert.True(request.OverlapFlagged);
        Assert.Equal(managerId, request.AcknowledgedById);
        Assert.Equal(now, request.AcknowledgedAt);
        Assert.Equal(1, request.Version);
    }

    [Fact]
    public void Approval_without_acknowledgement_is_refused_when_overlap_is_active()
    {
        var request = Pending();

        var exception = Assert.Throws<DomainException>(() =>
            request.Approve(Guid.NewGuid(), DateTimeOffset.UtcNow, null, true, false));

        Assert.Equal("OVERLAP_ACK_REQUIRED", exception.Code);
        Assert.Equal(LeaveStatus.PENDING, request.Status);
    }

    [Fact]
    public void Double_approval_is_refused()
    {
        var request = Pending();
        var managerId = Guid.NewGuid();
        request.Approve(managerId, DateTimeOffset.UtcNow, null, false, false);

        var exception = Assert.Throws<DomainException>(() =>
            request.Approve(managerId, DateTimeOffset.UtcNow, null, false, false));

        Assert.Equal("INVALID_STATE", exception.Code);
    }

    [Fact]
    public void Rejection_requires_a_comment()
    {
        var request = Pending();

        var exception = Assert.Throws<DomainException>(() =>
            request.Reject(Guid.NewGuid(), DateTimeOffset.UtcNow, "   "));

        Assert.Equal("COMMENT_REQUIRED", exception.Code);
        Assert.Equal(LeaveStatus.PENDING, request.Status);
    }

    [Fact]
    public void Rejection_sets_status_without_balance_effect()
    {
        var request = Pending();

        request.Reject(Guid.NewGuid(), DateTimeOffset.UtcNow, "Pic d'activité");

        Assert.Equal(LeaveStatus.REJECTED, request.Status);
        Assert.Equal("Pic d'activité", request.DecisionComment);
    }

    [Fact]
    public void Cancellation_of_a_terminal_request_is_refused()
    {
        var request = Pending();
        request.Cancel(Guid.NewGuid(), DateTimeOffset.UtcNow, "EMPLOYEE_CANCELLATION");

        var exception = Assert.Throws<DomainException>(() =>
            request.Cancel(Guid.NewGuid(), DateTimeOffset.UtcNow, "EMPLOYEE_CANCELLATION"));

        Assert.Equal("INVALID_STATE", exception.Code);
    }

    [Fact]
    public void Balance_never_becomes_negative()
    {
        var balance = new AnnualBalance { GrantedHalfDays = 4, ConsumedHalfDays = 3 };

        Assert.Throws<DomainException>(() => balance.Consume(2));
        Assert.Equal(1, balance.AvailableHalfDays);
    }

    [Fact]
    public void Balance_restore_and_adjustment_are_bounded()
    {
        var balance = new AnnualBalance { GrantedHalfDays = 4, ConsumedHalfDays = 3 };

        balance.Restore(1);
        Assert.Equal(2, balance.ConsumedHalfDays);

        Assert.Throws<DomainException>(() => balance.AdjustGranted(-3));
        Assert.Equal(4, balance.GrantedHalfDays);
    }
}
