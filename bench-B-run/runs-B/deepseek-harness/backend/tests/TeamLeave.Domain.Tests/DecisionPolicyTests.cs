using TeamLeave.Domain.Enums;
using TeamLeave.Domain.Services;
using Xunit;

namespace TeamLeave.Domain.Tests;

public sealed class DecisionPolicyTests
{
    private static readonly Guid Requester = Guid.NewGuid();
    private static readonly Guid Manager = Guid.NewGuid();
    private static readonly Guid OtherManager = Guid.NewGuid();
    private static readonly Guid Admin = Guid.NewGuid();

    [Fact]
    public void Direct_manager_can_approve_a_pending_request()
    {
        var outcome = Evaluate(managerId: Manager, callerHasManagerRole: true);

        Assert.True(outcome.Allowed);
    }

    [Fact]
    public void Self_decision_is_forbidden_even_for_an_admin()
    {
        var outcome = DecisionPolicy.Evaluate(
            Requester,
            Requester,
            Requester,
            callerHasManagerRole: true,
            callerHasAdminRole: true,
            LeaveStatus.PENDING,
            overlapActive: false,
            overlapAcknowledged: false,
            isRejection: false,
            comment: null);

        Assert.False(outcome.Allowed);
        Assert.Equal("FORBIDDEN", outcome.Code);
        Assert.Equal(403, outcome.HttpStatus);
    }

    [Fact]
    public void Manager_outside_scope_gets_not_found()
    {
        var outcome = DecisionPolicy.Evaluate(
            Requester,
            OtherManager,
            Manager,
            callerHasManagerRole: true,
            callerHasAdminRole: false,
            LeaveStatus.PENDING,
            overlapActive: false,
            overlapAcknowledged: false,
            isRejection: false,
            comment: null);

        Assert.False(outcome.Allowed);
        Assert.Equal("NOT_FOUND", outcome.Code);
        Assert.Equal(404, outcome.HttpStatus);
    }

    [Fact]
    public void Admin_without_manager_role_gets_forbidden()
    {
        var outcome = DecisionPolicy.Evaluate(
            Requester,
            Admin,
            Manager,
            callerHasManagerRole: false,
            callerHasAdminRole: true,
            LeaveStatus.PENDING,
            overlapActive: false,
            overlapAcknowledged: false,
            isRejection: false,
            comment: null);

        Assert.False(outcome.Allowed);
        Assert.Equal("FORBIDDEN", outcome.Code);
        Assert.Equal(403, outcome.HttpStatus);
    }

    [Fact]
    public void A_second_decision_is_a_state_conflict()
    {
        var outcome = Evaluate(managerId: Manager, status: LeaveStatus.APPROVED);

        Assert.False(outcome.Allowed);
        Assert.Equal("INVALID_STATE", outcome.Code);
        Assert.Equal(409, outcome.HttpStatus);
    }

    [Fact]
    public void Rejection_requires_a_non_blank_comment()
    {
        var outcome = Evaluate(managerId: Manager, isRejection: true, comment: "   ");

        Assert.False(outcome.Allowed);
        Assert.Equal("COMMENT_REQUIRED", outcome.Code);
        Assert.Equal(422, outcome.HttpStatus);
    }

    [Fact]
    public void Active_overlap_requires_acknowledgement_before_approval()
    {
        var outcome = Evaluate(
            managerId: Manager,
            overlapActive: true,
            overlapAcknowledged: false);

        Assert.False(outcome.Allowed);
        Assert.Equal("OVERLAP_ACK_REQUIRED", outcome.Code);
        Assert.Equal(409, outcome.HttpStatus);
    }

    [Fact]
    public void Acknowledged_overlap_allows_approval()
    {
        var outcome = Evaluate(
            managerId: Manager,
            overlapActive: true,
            overlapAcknowledged: true);

        Assert.True(outcome.Allowed);
    }

    [Fact]
    public void Rejection_does_not_require_an_overlap_acknowledgement()
    {
        var outcome = Evaluate(
            managerId: Manager,
            overlapActive: true,
            overlapAcknowledged: false,
            isRejection: true,
            comment: "Effectif insuffisant");

        Assert.True(outcome.Allowed);
    }

    private static Domain.ValueObjects.DecisionOutcome Evaluate(
        Guid managerId,
        bool callerHasManagerRole = true,
        LeaveStatus status = LeaveStatus.PENDING,
        bool overlapActive = false,
        bool overlapAcknowledged = false,
        bool isRejection = false,
        string? comment = "OK")
    {
        return DecisionPolicy.Evaluate(
            Requester,
            managerId,
            Manager,
            callerHasManagerRole,
            callerHasAdminRole: false,
            status,
            overlapActive,
            overlapAcknowledged,
            isRejection,
            comment);
    }
}
