using TeamLeave.Domain.Calendar;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Overlap;

namespace TeamLeave.Domain.Tests;

/// <summary>RB-16, RB-20 : exemples de référence EX-1 à EX-7 de la spécification (§5.4).</summary>
[Trait("Rule", "RB-16")]
[Trait("Rule", "RB-20")]
public sealed class OverlapAnalyzerTests
{
    // Semaine de référence : lundi 2026-03-09 → vendredi 2026-03-13.
    private static readonly DateOnly Mon = new(2026, 3, 9);
    private static readonly DateOnly Tue = Mon.AddDays(1);
    private static readonly DateOnly Wed = Mon.AddDays(2);
    private static readonly DateOnly Thu = Mon.AddDays(3);
    private static readonly DateOnly Fri = Mon.AddDays(4);

    private static readonly Colleague A = new(Guid.NewGuid(), "Alice A");
    private static readonly Colleague B = new(Guid.NewGuid(), "Bruno B");
    private static readonly Colleague C = new(Guid.NewGuid(), "Chloé C");
    private static readonly Colleague D = new(Guid.NewGuid(), "David D");
    private static readonly Colleague E = new(Guid.NewGuid(), "Emma E");

    private static IReadOnlyList<DateOnly> WorkingDays(DateOnly start, DateOnly end) =>
        WorkingDayCalculator.WorkingDays(new DateRange(start, end), new HashSet<DateOnly>());

    private static TeamAbsence Approved(Colleague c, DateOnly start, DateOnly? end = null) =>
        new(c.Id, start, end ?? start, LeaveRequestStatus.Approved);

    [Fact]
    public void EX1_two_of_three_colleagues_absent_on_tuesday_raises_alert_at_67_percent()
    {
        var result = OverlapAnalyzer.Analyze(
            A.Id, true, WorkingDays(Mon, Wed), [A, B, C, D], [Approved(B, Tue), Approved(C, Tue)]);

        Assert.True(result.Applicable);
        Assert.Equal(3, result.TeamSize);
        Assert.True(result.IsAlert);
        Assert.Equal(new Rate(2, 3), result.MaxRate);
        Assert.Equal(67, result.MaxRate!.Value.Percent);
        var day = Assert.Single(result.DaysOverThreshold);
        Assert.Equal(Tue, day.Date);
        Assert.Equal(["Bruno B", "Chloé C"], day.AbsentColleagues.Select(c => c.FullName));
    }

    [Fact]
    public void EX2_exactly_fifty_percent_is_not_an_alert()
    {
        var result = OverlapAnalyzer.Analyze(
            A.Id, true, WorkingDays(Mon, Mon), [A, B, C, D, E], [Approved(B, Mon), Approved(C, Mon)]);

        Assert.Equal(4, result.TeamSize);
        Assert.False(result.IsAlert);
        Assert.Equal(new Rate(2, 4), result.MaxRate);
        Assert.Equal(50, result.MaxRate!.Value.Percent);
        Assert.Empty(result.DaysOverThreshold);
    }

    [Fact]
    public void EX3_single_colleague_absent_is_one_hundred_percent()
    {
        var result = OverlapAnalyzer.Analyze(A.Id, true, WorkingDays(Mon, Fri), [A, B], [Approved(B, Thu)]);

        Assert.Equal(1, result.TeamSize);
        Assert.True(result.IsAlert);
        Assert.Equal(Thu, Assert.Single(result.DaysOverThreshold).Date);
        Assert.Equal(100, result.MaxRate!.Value.Percent);
    }

    [Fact]
    public void EX4_requester_alone_in_team_is_not_applicable()
    {
        var result = OverlapAnalyzer.Analyze(A.Id, true, WorkingDays(Mon, Fri), [A], []);

        Assert.False(result.Applicable);
        Assert.False(result.IsAlert);
        Assert.Null(result.MaxRate);
    }

    [Fact]
    public void EX4bis_requester_without_manager_is_not_applicable()
    {
        var result = OverlapAnalyzer.Analyze(A.Id, false, WorkingDays(Mon, Fri), [A, B], [Approved(B, Mon, Fri)]);

        Assert.False(result.Applicable);
        Assert.False(result.IsAlert);
    }

    [Fact]
    public void EX5_pending_requests_of_colleagues_are_ignored()
    {
        var pendingC = new TeamAbsence(C.Id, Mon, Mon, LeaveRequestStatus.Pending);
        var result = OverlapAnalyzer.Analyze(A.Id, true, WorkingDays(Mon, Mon), [A, B, C], [Approved(B, Mon), pendingC]);

        Assert.Equal(2, result.TeamSize);
        Assert.False(result.IsAlert);
        Assert.Equal(new Rate(1, 2), result.MaxRate);
    }

    [Fact]
    public void EX6_weekend_only_request_has_no_working_day()
    {
        var saturday = new DateOnly(2026, 3, 7);
        var days = WorkingDays(saturday, saturday.AddDays(1));

        Assert.Empty(days);
        var ex = Assert.Throws<DomainException>(() => LeaveRequest.Submit(
            Guid.NewGuid(), A.Id, LeaveType.CpId, new DateRange(saturday, saturday.AddDays(1)), days.Count, null,
            OverlapAnalysis.NotApplicable, DateTime.UtcNow));
        Assert.Equal(ErrorCodes.NoWorkingDay, ex.Code);
    }

    [Fact]
    public void EX7_evaluation_is_day_by_day_not_over_the_period()
    {
        var result = OverlapAnalyzer.Analyze(A.Id, true, WorkingDays(Mon, Tue), [A, B, C], [Approved(B, Mon), Approved(C, Tue)]);

        Assert.Equal(2, result.TeamSize);
        Assert.False(result.IsAlert);
        Assert.Equal(new Rate(1, 2), result.MaxRate);
    }

    [Fact]
    public void Rejected_and_cancelled_requests_are_ignored_and_absent_colleague_counted_once()
    {
        var absences = new[]
        {
            new TeamAbsence(B.Id, Mon, Mon, LeaveRequestStatus.Rejected),
            new TeamAbsence(C.Id, Mon, Mon, LeaveRequestStatus.Cancelled),
            Approved(D, Mon),
            Approved(D, Mon),
        };

        var result = OverlapAnalyzer.Analyze(A.Id, true, WorkingDays(Mon, Mon), [A, B, C, D], absences);

        Assert.Equal(new Rate(1, 3), result.MaxRate);
        Assert.Equal(33, result.MaxRate!.Value.Percent);
        Assert.False(result.IsAlert);
    }

    [Fact]
    public void Absences_of_people_outside_the_active_team_are_ignored()
    {
        var outsider = new Colleague(Guid.NewGuid(), "Hors équipe");
        var result = OverlapAnalyzer.Analyze(A.Id, true, WorkingDays(Mon, Mon), [A, B, C], [Approved(outsider, Mon), Approved(A, Mon)]);

        Assert.Equal(new Rate(0, 2), result.MaxRate);
        Assert.False(result.IsAlert);
    }

    [Theory]
    [InlineData(2, 3, true, 67)]
    [InlineData(1, 3, false, 33)]
    [InlineData(2, 4, false, 50)]
    [InlineData(3, 5, true, 60)]
    [InlineData(1, 1, true, 100)]
    [InlineData(0, 4, false, 0)]
    public void Rate_threshold_uses_exact_fraction_and_percent_is_display_only(int absent, int teamSize, bool exceeds, int percent)
    {
        var rate = new Rate(absent, teamSize);

        Assert.Equal(exceeds, rate.ExceedsHalf);
        Assert.Equal(percent, rate.Percent);
    }
}
