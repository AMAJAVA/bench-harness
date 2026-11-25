using TeamLeave.Domain.Enums;
using TeamLeave.Domain.Services;
using TeamLeave.Domain.ValueObjects;
using Xunit;
using static TeamLeave.Domain.Tests.TestData;

namespace TeamLeave.Domain.Tests;

public sealed class OverlapAnalyzerTests
{
    private static readonly DateOnly Day = Date(2026, 12, 8);

    [Fact]
    public void Six_team_with_three_full_day_absences_is_flagged_at_two_thirds()
    {
        var absences = Enumerable.Range(0, 3)
            .Select(_ => Absence(Guid.NewGuid(), FullDay(Day).ToArray()))
            .ToList();

        var result = OverlapAnalyzer.Analyze(6, FullDay(Day), absences);

        Assert.True(result.Applicable);
        Assert.True(result.Active);
        Assert.Equal(6, result.TeamSize);
        Assert.Equal(0.6667m, result.MaxRate);
        Assert.Equal(2, result.BreachingHalfDays.Count);
    }

    [Fact]
    public void Six_team_with_two_full_day_absences_is_exactly_fifty_percent_and_not_flagged()
    {
        var absences = Enumerable.Range(0, 2)
            .Select(_ => Absence(Guid.NewGuid(), FullDay(Day).ToArray()))
            .ToList();

        var result = OverlapAnalyzer.Analyze(6, FullDay(Day), absences);

        Assert.True(result.Applicable);
        Assert.False(result.Active);
        Assert.Equal(0.5m, result.MaxRate);
        Assert.Empty(result.BreachingHalfDays);
    }

    [Fact]
    public void Morning_can_breach_while_afternoon_stays_at_fifty_percent()
    {
        var absences = new List<ApprovedAbsence>();
        for (var i = 0; i < 2; i++)
        {
            absences.Add(Absence(Guid.NewGuid(), FullDay(Day).ToArray()));
        }

        for (var i = 0; i < 2; i++)
        {
            absences.Add(Absence(Guid.NewGuid(), Am(Day)));
        }

        var result = OverlapAnalyzer.Analyze(6, FullDay(Day), absences);

        Assert.True(result.Active);
        Assert.Equal(0.8333m, result.MaxRate);
        Assert.Single(result.BreachingHalfDays);
        Assert.Equal(HalfDay.AM, result.BreachingHalfDays[0].HalfDay);
    }

    [Fact]
    public void Afternoon_only_request_is_not_flagged_when_afternoon_is_at_fifty_percent()
    {
        var absences = new List<ApprovedAbsence>();
        for (var i = 0; i < 2; i++)
        {
            absences.Add(Absence(Guid.NewGuid(), FullDay(Day).ToArray()));
        }

        for (var i = 0; i < 2; i++)
        {
            absences.Add(Absence(Guid.NewGuid(), Am(Day)));
        }

        var result = OverlapAnalyzer.Analyze(6, [Pm(Day)], absences);

        Assert.True(result.Applicable);
        Assert.False(result.Active);
        Assert.Equal(0.5m, result.MaxRate);
    }

    [Fact]
    public void Two_member_team_with_one_absence_is_flagged_at_one_hundred_percent()
    {
        var absences = new[] { Absence(Guid.NewGuid(), FullDay(Day).ToArray()) };

        var result = OverlapAnalyzer.Analyze(2, FullDay(Day), absences);

        Assert.True(result.Active);
        Assert.Equal(1.0m, result.MaxRate);
    }

    [Fact]
    public void Single_member_team_has_no_signal()
    {
        var result = OverlapAnalyzer.Analyze(1, FullDay(Day), []);

        Assert.False(result.Applicable);
        Assert.False(result.Active);
        Assert.Null(result.MaxRate);
    }

    [Fact]
    public void A_member_is_counted_only_once_per_half_day()
    {
        var employeeId = Guid.NewGuid();
        var absences = new[]
        {
            Absence(employeeId, Am(Day)),
            Absence(employeeId, Am(Day))
        };

        var result = OverlapAnalyzer.Analyze(6, [Am(Day)], absences);

        Assert.Equal(0.3333m, result.MaxRate);
    }

    [Fact]
    public void Maximum_rate_is_computed_over_all_requested_half_days()
    {
        var nextDay = Day.AddDays(1);
        var absences = new List<ApprovedAbsence>();
        for (var i = 0; i < 3; i++)
        {
            absences.Add(Absence(Guid.NewGuid(), FullDay(nextDay).ToArray()));
        }

        var result = OverlapAnalyzer.Analyze(6, Days(Day, nextDay), absences);

        Assert.True(result.Active);
        Assert.Equal(0.6667m, result.MaxRate);
        Assert.Equal(2, result.BreachingHalfDays.Count);
    }

    [Fact]
    public void Pending_and_cancelled_requests_are_ignored_by_construction()
    {
        // L'analyseur ne reçoit que des absences approuvées ; ce test documente
        // que l'absence de lignes fournies ne produit aucun signal.
        var result = OverlapAnalyzer.Analyze(6, FullDay(Day), []);

        Assert.False(result.Active);
    }
}
