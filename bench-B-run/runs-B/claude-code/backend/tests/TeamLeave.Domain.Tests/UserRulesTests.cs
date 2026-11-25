using TeamLeave.Domain.Common;
using TeamLeave.Domain.Notifications;
using TeamLeave.Domain.Users;

namespace TeamLeave.Domain.Tests;

[Trait("Rule", "RB-04")]
public sealed class ApproverPolicyTests
{
    private static readonly Guid Requester = Guid.NewGuid();
    private static readonly Guid Manager = Guid.NewGuid();
    private static readonly Guid OtherManager = Guid.NewGuid();
    private static readonly Guid Hr = Guid.NewGuid();

    [Fact]
    public void Direct_manager_is_approver()
    {
        Assert.True(ApproverPolicy.IsApprover(Manager, false, Requester, Manager));
    }

    [Fact]
    public void CA_10_5_manager_of_another_team_is_not_approver()
    {
        var ex = Assert.Throws<ForbiddenException>(() => ApproverPolicy.EnsureCanDecide(OtherManager, false, Requester, Manager));

        Assert.Equal(ErrorCodes.NotApprover, ex.Code);
    }

    [Fact]
    public void H_31_hr_admin_is_not_approver_when_requester_has_a_manager()
    {
        var ex = Assert.Throws<ForbiddenException>(() => ApproverPolicy.EnsureCanDecide(Hr, true, Requester, Manager));

        Assert.Equal(ErrorCodes.NotApprover, ex.Code);
    }

    [Fact]
    public void H_03_hr_admin_approves_requester_without_manager()
    {
        Assert.True(ApproverPolicy.IsApprover(Hr, true, Requester, null));
    }

    [Fact]
    public void Non_hr_cannot_approve_requester_without_manager()
    {
        Assert.False(ApproverPolicy.IsApprover(Manager, false, Requester, null));
    }

    [Fact]
    public void Self_approval_is_forbidden_even_for_hr_admin_without_manager()
    {
        var ex = Assert.Throws<ForbiddenException>(() => ApproverPolicy.EnsureCanDecide(Hr, true, Hr, null));

        Assert.Equal(ErrorCodes.SelfApprovalForbidden, ex.Code);
    }
}

[Trait("Rule", "RB-03")]
public sealed class HierarchyValidatorTests
{
    private static readonly Guid A = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();
    private static readonly Guid C = Guid.NewGuid();

    [Fact]
    public void Own_manager_is_a_cycle()
    {
        var ex = Assert.Throws<DomainException>(() =>
            HierarchyValidator.Validate(A, new ManagerCandidate(A, true, true, null), _ => null));

        Assert.Equal(ErrorCodes.HierarchyCycle, ex.Code);
    }

    [Fact]
    public void CA_17_3_two_level_cycle_is_refused_even_if_candidate_lacks_manager_role()
    {
        // M (= A) est manager de N (= B) ; on veut faire de N le manager de M.
        var ex = Assert.Throws<DomainException>(() =>
            HierarchyValidator.Validate(A, new ManagerCandidate(B, true, false, A), id => id == B ? A : null));

        Assert.Equal(ErrorCodes.HierarchyCycle, ex.Code);
    }

    [Fact]
    public void Three_level_cycle_is_refused()
    {
        // B → A (manager de B = A), C → B ; rattacher A à C crée A → C → B → A.
        var managers = new Dictionary<Guid, Guid?> { [B] = A, [C] = B, [A] = null };

        var ex = Assert.Throws<DomainException>(() =>
            HierarchyValidator.Validate(A, new ManagerCandidate(C, true, true, B), id => managers[id]));

        Assert.Equal(ErrorCodes.HierarchyCycle, ex.Code);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Inactive_or_non_manager_candidate_is_invalid(bool active, bool hasRole)
    {
        var ex = Assert.Throws<DomainException>(() =>
            HierarchyValidator.Validate(A, new ManagerCandidate(B, active, hasRole, null), _ => null));

        Assert.Equal(ErrorCodes.InvalidManager, ex.Code);
    }

    [Fact]
    public void Valid_manager_is_accepted()
    {
        HierarchyValidator.Validate(A, new ManagerCandidate(B, true, true, C), _ => null);
    }
}

[Trait("Rule", "RB-01")]
[Trait("Rule", "RB-02")]
public sealed class UserTests
{
    private static readonly DateTime Now = new(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);

    private static User NewUser(params Role[] roles) =>
        User.Create(Guid.NewGuid(), "Alice.Martin@pme.fr", "Alice", "Martin", null, roles, "hash", false, Now);

    [Fact]
    public void Employee_role_is_always_present_and_email_normalized()
    {
        var user = NewUser(Role.Manager);

        Assert.Equal([Role.Employee, Role.Manager], user.RoleList());
        Assert.Equal("ALICE.MARTIN@PME.FR", user.NormalizedEmail);
    }

    [Fact]
    public void Changing_roles_regenerates_security_stamp()
    {
        var user = NewUser();
        var stamp = user.SecurityStamp;

        Assert.True(user.ReplaceRoles([Role.HrAdmin]));
        Assert.NotEqual(stamp, user.SecurityStamp);
        Assert.False(user.ReplaceRoles([Role.Employee, Role.HrAdmin]));
    }

    [Fact]
    public void CA_01_4_five_failures_within_fifteen_minutes_lock_the_account()
    {
        var user = NewUser();
        for (var i = 0; i < 4; i++)
        {
            user.RegisterFailedLogin(Now.AddMinutes(i));
        }

        Assert.False(user.IsLockedOut(Now.AddMinutes(4)));
        user.RegisterFailedLogin(Now.AddMinutes(4));
        Assert.True(user.IsLockedOut(Now.AddMinutes(5)));
        Assert.False(user.IsLockedOut(Now.AddMinutes(19).AddSeconds(1)));
    }

    [Fact]
    public void Failures_outside_the_window_restart_the_counter()
    {
        var user = NewUser();
        for (var i = 0; i < 4; i++)
        {
            user.RegisterFailedLogin(Now);
        }

        user.RegisterFailedLogin(Now.AddMinutes(16));

        Assert.False(user.IsLockedOut(Now.AddMinutes(16)));
        Assert.Equal(1, user.FailedLoginCount);
    }

    [Fact]
    public void RB_27_deactivation_regenerates_stamp_and_twice_is_conflict()
    {
        var user = NewUser();
        var stamp = user.SecurityStamp;

        user.Deactivate(Now);

        Assert.False(user.IsActive);
        Assert.NotEqual(stamp, user.SecurityStamp);
        Assert.Equal(ErrorCodes.UserAlreadyInactive, Assert.Throws<DomainException>(() => user.Deactivate(Now)).Code);
    }

    [Theory]
    [InlineData("short-pass1", false)]
    [InlineData("long-enough-1", true)]
    [InlineData("alice.martin@pme.fr", false)]
    [InlineData("password1234", false)]
    public void Password_policy(string password, bool accepted)
    {
        Assert.Equal(accepted, PasswordPolicy.Validate(password, "alice.martin@pme.fr") is null);
    }
}

[Trait("Rule", "RB-28")]
public sealed class NotificationTests
{
    private static readonly DateTime T0 = new(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Failed_attempts_follow_schedule_then_mark_failed_after_fourth()
    {
        var n = Notification.Queue(Guid.NewGuid(), NotificationEventType.Submission, Guid.NewGuid(), "m@pme.fr", "Sujet", "Corps", T0);

        n.MarkAttemptFailed("SMTP down", T0);
        Assert.Equal(T0.AddMinutes(5), n.NextAttemptAtUtc);
        n.MarkAttemptFailed("SMTP down", T0.AddMinutes(5));
        Assert.Equal(T0.AddMinutes(20), n.NextAttemptAtUtc);
        n.MarkAttemptFailed("SMTP down", T0.AddMinutes(20));
        Assert.Equal(T0.AddMinutes(65), n.NextAttemptAtUtc);
        Assert.Equal(NotificationStatus.Queued, n.Status);

        n.MarkAttemptFailed("SMTP down", T0.AddMinutes(65));

        Assert.Equal(NotificationStatus.Failed, n.Status);
        Assert.Equal(4, n.Attempts);
        Assert.Null(n.NextAttemptAtUtc);
    }

    [Fact]
    public void Subject_line_breaks_are_removed()
    {
        var n = Notification.Queue(Guid.NewGuid(), NotificationEventType.Submission, Guid.NewGuid(), "m@pme.fr", "A\r\nB", "Corps", T0);

        Assert.Equal("A  B", n.Subject);
    }
}
