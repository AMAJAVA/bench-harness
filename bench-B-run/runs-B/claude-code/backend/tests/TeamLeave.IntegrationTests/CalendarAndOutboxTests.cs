using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Notifications;
using TeamLeave.Domain.Users;
using TeamLeave.IntegrationTests.Support;

namespace TeamLeave.IntegrationTests;

/// <summary>US-07, US-12, US-19 : calendrier selon le rôle (RB-23).</summary>
[Trait("Rule", "RB-03")]
[Trait("Rule", "RB-23")]
public sealed class CalendarTests : IntegrationTest
{
    private const string March = "from=2026-03-01&to=2026-03-31";

    [Fact]
    public async Task CA_07_1_and_07_2_team_calendar_shows_approved_absences_without_type()
    {
        var m = await CreateUserAsync("Marc", "Durand", null, Role.Manager);
        var a = await CreateUserAsync("Alice", "Martin", m.Id);
        var b = await CreateUserAsync("Bruno", "Petit", m.Id);
        var c = await CreateUserAsync("Chloe", "Roux", m.Id);
        var outsider = await CreateUserAsync("Oscar", "Dehors");
        await CreateApprovedAsync(a.Id, m.Id, new(2026, 3, 4), new(2026, 3, 4));
        await CreateApprovedAsync(b.Id, m.Id, new(2026, 3, 10), new(2026, 3, 12), LeaveType.SickId);
        await CreateApprovedAsync(outsider.Id, m.Id, new(2026, 3, 10), new(2026, 3, 10));
        var cClient = await ClientForAsync(c);
        await SubmitOkAsync(cClient, LeaveType.SickId, "2026-03-16", "2026-03-16"); // en attente : invisible

        var calendar = await (await cClient.GetAsync($"/api/v1/calendar?scope=team&{March}")).AssertStatus(HttpStatusCode.OK);

        var absences = calendar.GetProperty("absences").EnumerateArray().ToList();
        Assert.Equal(["Alice Martin", "Bruno Petit"], absences.Select(x => x.GetProperty("user").GetProperty("fullName").GetString()));
        Assert.All(absences, x => Assert.Equal(System.Text.Json.JsonValueKind.Null, x.GetProperty("leaveType").ValueKind));
        Assert.False(absences[0].TryGetProperty("id", out _));
    }

    [Fact]
    public async Task CA_07_3_range_longer_than_93_days_is_400()
    {
        var a = await CreateUserAsync("Alice", "Martin");
        var client = await ClientForAsync(a);

        await (await client.GetAsync("/api/v1/calendar?scope=team&from=2026-03-01&to=2026-06-02")).AssertProblem(HttpStatusCode.BadRequest, ErrorCodes.RangeTooLong);
        await (await client.GetAsync("/api/v1/calendar?scope=team&from=2026-03-10&to=2026-03-01")).AssertProblem(HttpStatusCode.BadRequest, ErrorCodes.ValidationError);
        await (await client.GetAsync("/api/v1/calendar?scope=team")).AssertProblem(HttpStatusCode.BadRequest, ErrorCodes.ValidationError);
    }

    [Fact]
    public async Task CA_12_1_and_12_2_reports_calendar_shows_types_for_direct_reports_only()
    {
        var m = await CreateUserAsync("Marc", "Durand", null, Role.Manager);
        var other = await CreateUserAsync("Olga", "Autre", null, Role.Manager);
        var a = await CreateUserAsync("Alice", "Martin", m.Id);
        var x = await CreateUserAsync("Xavier", "Ailleurs", other.Id);
        await CreateApprovedAsync(a.Id, m.Id, new(2026, 3, 10), new(2026, 3, 10), LeaveType.SickId);
        await CreateApprovedAsync(x.Id, other.Id, new(2026, 3, 10), new(2026, 3, 10));
        var mClient = await ClientForAsync(m);

        var calendar = await (await mClient.GetAsync($"/api/v1/calendar?scope=reports&{March}")).AssertStatus(HttpStatusCode.OK);

        var absence = Assert.Single(calendar.GetProperty("absences").EnumerateArray());
        Assert.Equal("Alice Martin", absence.GetProperty("user").GetProperty("fullName").GetString());
        Assert.Equal("Maladie", absence.GetProperty("leaveType").GetProperty("label").GetString());

        await (await (await ClientForAsync(a)).GetAsync($"/api/v1/calendar?scope=reports&{March}")).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task CA_19_2_global_calendar_for_hr_with_manager_filter()
    {
        var hr = await CreateUserAsync("Helene", "Rh", null, Role.HrAdmin);
        var m = await CreateUserAsync("Marc", "Durand", null, Role.Manager);
        var a = await CreateUserAsync("Alice", "Martin", m.Id);
        var loner = await CreateUserAsync("Luc", "Seul");
        await CreateApprovedAsync(a.Id, m.Id, new(2026, 3, 10), new(2026, 3, 10));
        await CreateApprovedAsync(loner.Id, hr.Id, new(2026, 3, 11), new(2026, 3, 11));
        var hrClient = await ClientForAsync(hr);

        var all = await (await hrClient.GetAsync($"/api/v1/calendar?scope=all&{March}")).AssertStatus(HttpStatusCode.OK);
        var filtered = await (await hrClient.GetAsync($"/api/v1/calendar?scope=all&managerId={m.Id}&{March}")).AssertStatus(HttpStatusCode.OK);

        Assert.Equal(2, all.GetProperty("absences").GetArrayLength());
        Assert.All(all.GetProperty("absences").EnumerateArray(), x => Assert.Equal("MALADIE", x.GetProperty("leaveType").GetProperty("code").GetString()));
        Assert.Single(filtered.GetProperty("absences").EnumerateArray());
        await (await (await ClientForAsync(m)).GetAsync($"/api/v1/calendar?scope=all&{March}")).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
    }
}

/// <summary>RB-28, ADR-03 : outbox transactionnelle, envoi non bloquant, calendrier des tentatives.</summary>
[Trait("Rule", "RB-28")]
public sealed class OutboxTests : IntegrationTest
{
    [Fact]
    public async Task Dispatch_sends_queued_emails_and_marks_them_sent()
    {
        var m = await CreateUserAsync("Marc", "Durand", null, Role.Manager);
        var a = await CreateUserAsync("Alice", "Martin", m.Id);
        await SubmitOkAsync(await ClientForAsync(a), LeaveType.SickId, "2026-03-09", "2026-03-09");

        Assert.Equal(1, await DispatchOutboxAsync());

        var sent = Assert.Single(Factory.Emails.Sent);
        Assert.Equal(m.Email, sent.To);
        var notification = await QueryDbAsync(db => db.Notifications.SingleAsync());
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.Equal(1, notification.Attempts);
    }

    [Fact]
    public async Task CA_13_4_smtp_outage_does_not_block_submission_and_retries_then_fails_after_fourth_attempt()
    {
        var hr = await CreateUserAsync("Helene", "Rh", null, Role.HrAdmin);
        var m = await CreateUserAsync("Marc", "Durand", null, Role.Manager);
        var a = await CreateUserAsync("Alice", "Martin", m.Id);
        Factory.Emails.Fail = true;

        await SubmitOkAsync(await ClientForAsync(a), LeaveType.SickId, "2026-03-09", "2026-03-09");

        Assert.Equal(1, await DispatchOutboxAsync());
        var n = await QueryDbAsync(db => db.Notifications.SingleAsync());
        Assert.Equal(NotificationStatus.Queued, n.Status);
        Assert.Equal(TeamLeaveFactory.StartUtc.AddMinutes(5).UtcDateTime, n.NextAttemptAtUtc);
        Assert.Equal(0, await DispatchOutboxAsync()); // pas encore échu

        foreach (var minutes in new[] { 5, 15, 45 })
        {
            Factory.Clock.Advance(TimeSpan.FromMinutes(minutes));
            Assert.Equal(1, await DispatchOutboxAsync());
        }

        n = await QueryDbAsync(db => db.Notifications.SingleAsync());
        Assert.Equal(NotificationStatus.Failed, n.Status);
        Assert.Equal(4, n.Attempts);
        Assert.Contains("SMTP 451", n.LastError);

        // Journal consultable par l'admin RH, sans le corps de l'email (GAP-14).
        var hrClient = await ClientForAsync(hr);
        var journal = await (await hrClient.GetAsync("/api/v1/notifications?status=Failed")).AssertStatus(HttpStatusCode.OK);
        var item = Assert.Single(journal.GetProperty("items").EnumerateArray());
        Assert.Equal(4, item.GetProperty("attempts").GetInt32());
        Assert.False(item.TryGetProperty("body", out _));
    }
}
