using TeamLeave.Domain.Common;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Overlap;

namespace TeamLeave.Domain.Tests;

[Trait("Rule", "RB-13")]
[Trait("Rule", "RB-14")]
[Trait("Rule", "RB-15")]
[Trait("Rule", "RB-18")]
public sealed class LeaveRequestStateMachineTests
{
    private static readonly DateTime Now = new(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 3, 2);
    private static readonly Guid Requester = Guid.NewGuid();
    private static readonly Guid Manager = Guid.NewGuid();

    private static readonly OverlapAnalysis Alert =
        new(true, 3, true, new Rate(2, 3), [new OverlapDay(new(2026, 3, 10), new Rate(2, 3), [])]);

    private static readonly OverlapAnalysis NoAlert = new(true, 3, false, new Rate(1, 3), []);

    private static LeaveRequest Pending(DateOnly? start = null)
    {
        var s = start ?? new DateOnly(2026, 3, 9);
        return LeaveRequest.Submit(Guid.NewGuid(), Requester, LeaveType.CpId, new DateRange(s, s.AddDays(2)), 3, null, NoAlert, Now);
    }

    private static LeaveRequest WithStatus(LeaveRequestStatus status)
    {
        var request = Pending();
        switch (status)
        {
            case LeaveRequestStatus.Approved:
                request.Approve(Manager, null, NoAlert, false, Now);
                break;
            case LeaveRequestStatus.Rejected:
                request.Reject(Manager, "Non", Now);
                break;
            case LeaveRequestStatus.Cancelled:
                request.CancelByRequester(Requester, Today, Now);
                break;
        }

        return request;
    }

    [Fact]
    public void Submission_creates_pending_request_with_creation_history()
    {
        var request = Pending();

        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
        var entry = Assert.Single(request.History);
        Assert.Null(entry.FromStatus);
        Assert.Equal(LeaveRequestStatus.Pending, entry.ToStatus);
        Assert.Equal(Requester, entry.AuthorId);
    }

    [Fact]
    public void Approve_records_decider_and_history()
    {
        var request = WithStatus(LeaveRequestStatus.Approved);

        Assert.Equal(LeaveRequestStatus.Approved, request.Status);
        Assert.Equal(Manager, request.DecidedById);
        Assert.Equal(Now, request.DecidedAtUtc);
        Assert.Null(request.OverlapAcknowledgedById);
        Assert.Equal(2, request.History.Count);
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Approved)]
    [InlineData(LeaveRequestStatus.Rejected)]
    [InlineData(LeaveRequestStatus.Cancelled)]
    public void CA_10_6_decided_or_cancelled_request_cannot_be_approved_or_rejected(LeaveRequestStatus status)
    {
        var request = WithStatus(status);

        Assert.Equal(ErrorCodes.InvalidStatusTransition,
            Assert.Throws<DomainException>(() => request.Approve(Manager, null, NoAlert, false, Now)).Code);
        Assert.Equal(ErrorCodes.InvalidStatusTransition,
            Assert.Throws<DomainException>(() => request.Reject(Manager, "Non", Now)).Code);
    }

    [Theory]
    [InlineData(LeaveRequestStatus.Rejected)]
    [InlineData(LeaveRequestStatus.Cancelled)]
    public void CA_06_4_terminal_requests_cannot_be_cancelled(LeaveRequestStatus status)
    {
        var request = WithStatus(status);

        var ex = Assert.Throws<DomainException>(() => request.CancelByRequester(Requester, Today, Now));

        Assert.Equal(ErrorCodes.InvalidStatusTransition, ex.Code);
    }

    [Fact]
    public void CA_06_1_pending_request_can_be_cancelled()
    {
        var request = Pending();

        var previous = request.CancelByRequester(Requester, Today, Now);

        Assert.Equal(LeaveRequestStatus.Pending, previous);
        Assert.Equal(LeaveRequestStatus.Cancelled, request.Status);
        Assert.Equal(Requester, request.CancelledById);
    }

    [Theory]
    [InlineData(0, true)]   // début aujourd'hui
    [InlineData(-1, true)]  // début passé
    [InlineData(1, false)]  // début demain
    public void CA_06_3_approved_request_already_started_cannot_be_cancelled(int startOffset, bool refused)
    {
        var request = Pending(Today.AddDays(startOffset));
        request.Approve(Manager, null, NoAlert, false, Now);

        if (refused)
        {
            var ex = Assert.Throws<DomainException>(() => request.CancelByRequester(Requester, Today, Now));
            Assert.Equal(ErrorCodes.AlreadyStarted, ex.Code);
            Assert.Equal(LeaveRequestStatus.Approved, request.Status);
        }
        else
        {
            Assert.Equal(LeaveRequestStatus.Approved, request.CancelByRequester(Requester, Today, Now));
            Assert.Equal(LeaveRequestStatus.Cancelled, request.Status);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void CA_11_2_rejection_without_comment_is_refused_and_request_stays_pending(string? comment)
    {
        var request = Pending();

        var ex = Assert.Throws<DomainException>(() => request.Reject(Manager, comment, Now));

        Assert.Equal(ErrorCodes.RejectionCommentRequired, ex.Code);
        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void Rejection_comment_length_limit_is_1000()
    {
        Assert.Equal(1000, LeaveRequest.NormalizeRejectionComment(new string('x', 1000)).Length);
        Assert.Throws<ValidationException>(() => LeaveRequest.NormalizeRejectionComment(new string('x', 1001)));
    }

    [Fact]
    public void CA_11_1_rejection_records_trimmed_comment()
    {
        var request = Pending();

        request.Reject(Manager, "  Période de clôture  ", Now);

        Assert.Equal(LeaveRequestStatus.Rejected, request.Status);
        Assert.Equal("Période de clôture", request.DecisionComment);
    }

    [Fact]
    public void CA_09_4_approval_in_alert_without_acknowledgement_is_refused()
    {
        var request = Pending();

        var ex = Assert.Throws<DomainException>(() => request.Approve(Manager, null, Alert, false, Now));

        Assert.Equal(ErrorCodes.OverlapNotAcknowledged, ex.Code);
        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void CA_09_5_approval_in_alert_with_acknowledgement_records_rate()
    {
        var request = Pending();

        request.Approve(Manager, "OK", Alert, true, Now);

        Assert.Equal(LeaveRequestStatus.Approved, request.Status);
        Assert.Equal(Manager, request.OverlapAcknowledgedById);
        Assert.Equal(Now, request.OverlapAcknowledgedAtUtc);
        Assert.Equal(2, request.OverlapAcknowledgedAbsent);
        Assert.Equal(3, request.OverlapAcknowledgedTeamSize);
    }

    [Fact]
    public void CA_11_4_rejection_in_alert_needs_no_acknowledgement()
    {
        var request = Pending();

        request.Reject(Manager, "Trop d'absents", Now);

        Assert.Equal(LeaveRequestStatus.Rejected, request.Status);
    }

    [Fact]
    public void Acknowledgement_without_alert_is_not_recorded()
    {
        var request = Pending();

        request.Approve(Manager, null, NoAlert, true, Now);

        Assert.Null(request.OverlapAcknowledgedById);
    }

    [Fact]
    public void RB_04_requester_cannot_decide_own_request()
    {
        var request = Pending();

        var ex = Assert.Throws<ForbiddenException>(() => request.Approve(Requester, null, NoAlert, false, Now));

        Assert.Equal(ErrorCodes.SelfApprovalForbidden, ex.Code);
    }

    [Fact]
    public void RB_27_system_cancellation_has_no_author_and_reason()
    {
        var request = Pending();

        request.CancelBySystem(LeaveRequest.DeactivationReason, Now);

        Assert.Equal(LeaveRequestStatus.Cancelled, request.Status);
        Assert.Null(request.CancelledById);
        Assert.Equal("Compte désactivé", request.CancellationReason);
        Assert.Null(request.History.Last().AuthorId);
    }
}
