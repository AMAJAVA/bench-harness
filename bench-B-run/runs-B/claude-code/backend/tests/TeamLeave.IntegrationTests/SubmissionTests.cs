using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Notifications;
using TeamLeave.Domain.Users;
using TeamLeave.IntegrationTests.Support;

namespace TeamLeave.IntegrationTests;

/// <summary>US-02, US-03, US-04, US-13 : soldes, simulation, soumission, email au manager.</summary>
[Trait("Rule", "RB-05")]
[Trait("Rule", "RB-06")]
[Trait("Rule", "RB-07")]
[Trait("Rule", "RB-08")]
[Trait("Rule", "RB-09")]
[Trait("Rule", "RB-10")]
[Trait("Rule", "RB-21")]
public sealed class SubmissionTests : IntegrationTest
{
    private static readonly Guid Cp = LeaveType.CpId;

    private async Task<(User Manager, User Alice, HttpClient Client)> AliceWithManagerAsync(int cpDays)
    {
        var manager = await CreateUserAsync("Marc", "Durand", null, Role.Manager);
        var alice = await CreateUserAsync("Alice", "Martin", manager.Id);
        if (cpDays > 0)
        {
            await AllocateAsync(alice.Id, Cp, cpDays);
        }

        return (manager, alice, await ClientForAsync(alice));
    }

    [Fact]
    public async Task CA_02_1_and_02_2_balances_show_amount_pending_available_and_hide_untracked_types()
    {
        var (_, _, client) = await AliceWithManagerAsync(25);
        await SubmitOkAsync(client, Cp, "2026-03-09", "2026-03-11");

        var balances = await (await client.GetAsync("/api/v1/me/balances")).AssertStatus(HttpStatusCode.OK);

        var cp = balances.EnumerateArray().Single(b => b.GetProperty("leaveType").GetProperty("code").GetString() == "CP");
        Assert.Equal(25m, cp.GetProperty("amount").GetDecimal());
        Assert.Equal(3m, cp.GetProperty("pending").GetDecimal());
        Assert.Equal(22m, cp.GetProperty("available").GetDecimal());
        Assert.DoesNotContain(balances.EnumerateArray(), b => b.GetProperty("leaveType").GetProperty("code").GetString() == "MALADIE");
    }

    [Fact]
    public async Task CA_02_3_employee_cannot_read_balances_of_another_employee()
    {
        var (_, _, client) = await AliceWithManagerAsync(0);
        var bob = await CreateUserAsync("Bob", "Petit");

        await (await client.GetAsync($"/api/v1/users/{bob.Id}/balances")).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task CA_03_1_simulation_counts_working_days_without_side_effects()
    {
        var (_, _, client) = await AliceWithManagerAsync(25);

        var sim = await (await client.PostAsJsonAsync("/api/v1/leave-requests/simulate", new { leaveTypeId = Cp, startDate = "2026-03-06", endDate = "2026-03-10" }))
            .AssertStatus(HttpStatusCode.OK);

        Assert.Equal(3, sim.GetProperty("daysCount").GetInt32());
        Assert.Equal(["2026-03-06", "2026-03-09", "2026-03-10"], sim.GetProperty("workingDays").EnumerateArray().Select(d => d.GetString()));
        Assert.Equal(22m, sim.GetProperty("balance").GetProperty("availableAfter").GetDecimal());
        Assert.True(sim.GetProperty("isSubmittable").GetBoolean());
        // CA-03.4 : aucune demande, aucun email. CA-03.5 : aucune donnée de collègue.
        Assert.Equal(0, await QueryDbAsync(db => db.LeaveRequests.CountAsync()));
        Assert.Equal(0, await QueryDbAsync(db => db.Notifications.CountAsync()));
        Assert.False(sim.TryGetProperty("overlap", out _));
    }

    [Fact]
    public async Task CA_03_2_easter_monday_holiday_is_excluded_from_simulation()
    {
        var hr = await CreateUserAsync("Helene", "Rh", null, Role.HrAdmin);
        var (_, _, client) = await AliceWithManagerAsync(25);
        var hrClient = await ClientForAsync(hr);
        await (await hrClient.PostAsJsonAsync("/api/v1/holidays", new { date = "2026-04-06", label = "Lundi de Pâques" })).AssertStatus(HttpStatusCode.Created);

        var sim = await (await client.PostAsJsonAsync("/api/v1/leave-requests/simulate", new { leaveTypeId = Cp, startDate = "2026-04-06", endDate = "2026-04-08" }))
            .AssertStatus(HttpStatusCode.OK);

        Assert.Equal(2, sim.GetProperty("daysCount").GetInt32());
    }

    [Fact]
    public async Task CA_03_3_weekend_simulation_is_422_no_working_day()
    {
        var (_, _, client) = await AliceWithManagerAsync(25);

        await (await client.PostAsJsonAsync("/api/v1/leave-requests/simulate", new { leaveTypeId = Cp, startDate = "2026-03-07", endDate = "2026-03-08" }))
            .AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.NoWorkingDay);
    }

    [Fact]
    public async Task GAP_10_simulation_reports_insufficient_balance_as_blocking_reason()
    {
        var (_, _, client) = await AliceWithManagerAsync(2);

        var sim = await (await client.PostAsJsonAsync("/api/v1/leave-requests/simulate", new { leaveTypeId = Cp, startDate = "2026-03-09", endDate = "2026-03-11" }))
            .AssertStatus(HttpStatusCode.OK);

        Assert.False(sim.GetProperty("isSubmittable").GetBoolean());
        Assert.Equal([ErrorCodes.InsufficientBalance], sim.GetProperty("blockingReasons").EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    [Trait("Rule", "RB-26")]
    public async Task CA_04_1_submission_creates_pending_request_and_reserves_balance()
    {
        var (manager, alice, client) = await AliceWithManagerAsync(10);

        var response = await SubmitAsync(client, Cp, "2026-03-09", "2026-03-13", "Vacances scolaires");

        var body = await response.AssertStatus(HttpStatusCode.Created);
        Assert.Equal($"/api/v1/leave-requests/{body.GetProperty("id").GetGuid()}", response.Headers.Location?.ToString());
        Assert.Equal("Pending", body.GetProperty("status").GetString());
        Assert.Equal(5, body.GetProperty("daysCount").GetInt32());
        Assert.Equal("Vacances scolaires", body.GetProperty("comment").GetString());
        Assert.Equal(JsonValueKindNull, body.GetProperty("overlap").ValueKind);
        // CA-04.9 : historique « création → En attente » avec l'auteur.
        var history = Assert.Single(body.GetProperty("history").EnumerateArray());
        Assert.Equal("Pending", history.GetProperty("toStatus").GetString());
        Assert.Equal(alice.Id, history.GetProperty("author").GetProperty("id").GetGuid());

        var cp = (await (await client.GetAsync("/api/v1/me/balances")).Json()).EnumerateArray().Single(b => b.GetProperty("leaveType").GetProperty("code").GetString() == "CP");
        Assert.Equal(10m, cp.GetProperty("amount").GetDecimal());
        Assert.Equal(5m, cp.GetProperty("available").GetDecimal());

        // CA-13.1 : email au manager, dans la même transaction (outbox).
        var notification = await QueryDbAsync(db => db.Notifications.SingleAsync());
        Assert.Equal(NotificationEventType.Submission, notification.EventType);
        Assert.Equal(manager.Email, notification.RecipientEmail);
        Assert.Contains("Alice Martin", notification.Body);
        Assert.Contains("Jours décomptés : 5", notification.Body);
        Assert.Contains($"/demandes/{body.GetProperty("id").GetGuid()}", notification.Body);
    }

    [Fact]
    public async Task CA_04_2_insufficient_balance_is_422_and_creates_nothing()
    {
        var (_, _, client) = await AliceWithManagerAsync(2);

        await (await SubmitAsync(client, Cp, "2026-03-09", "2026-03-11")).AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.InsufficientBalance);

        Assert.Equal(0, await QueryDbAsync(db => db.LeaveRequests.CountAsync()));
        Assert.Equal(0, await QueryDbAsync(db => db.Notifications.CountAsync()));
    }

    [Fact]
    public async Task CA_04_3_end_before_start_is_400()
    {
        var (_, _, client) = await AliceWithManagerAsync(10);

        var body = await (await SubmitAsync(client, Cp, "2026-03-10", "2026-03-09")).AssertProblem(HttpStatusCode.BadRequest, ErrorCodes.ValidationError);

        Assert.True(body.GetProperty("errors").TryGetProperty("endDate", out _));
    }

    [Fact]
    public async Task CA_04_4_past_date_for_non_retroactive_type_is_refused()
    {
        var (_, _, client) = await AliceWithManagerAsync(10);

        await (await SubmitAsync(client, Cp, "2026-02-27", "2026-03-03")).AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.PastDateNotAllowed);
    }

    [Fact]
    public async Task CA_04_5_sick_leave_can_be_retroactive()
    {
        var (_, _, client) = await AliceWithManagerAsync(0);

        var body = await (await SubmitAsync(client, LeaveType.SickId, "2026-02-26", "2026-02-27")).AssertStatus(HttpStatusCode.Created);

        Assert.Equal("Pending", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task CA_04_6_overlap_with_own_approved_request_is_refused()
    {
        var (manager, alice, client) = await AliceWithManagerAsync(10);
        await CreateApprovedAsync(alice.Id, manager.Id, new(2026, 3, 11), new(2026, 3, 11), LeaveType.RttId);

        await (await SubmitAsync(client, Cp, "2026-03-10", "2026-03-12")).AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.OverlapsOwnRequest);
    }

    [Fact]
    public async Task CA_04_7_inactive_leave_type_is_refused()
    {
        var (_, _, client) = await AliceWithManagerAsync(10);
        await WithDbAsync(async db =>
        {
            var other = await db.LeaveTypes.FirstAsync(t => t.Id == LeaveType.OtherId);
            other.Update(other.Label, other.IsTracked, other.AllowsRetroactive, isActive: false);
        });

        await (await SubmitAsync(client, LeaveType.OtherId, "2026-03-09", "2026-03-09")).AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.LeaveTypeInactive);
    }

    [Fact]
    public async Task Period_longer_than_366_days_is_refused_and_unknown_type_is_404()
    {
        var (_, _, client) = await AliceWithManagerAsync(10);

        await (await SubmitAsync(client, LeaveType.SickId, "2026-03-02", "2027-03-04")).AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.PeriodTooLong);
        await (await SubmitAsync(client, Guid.NewGuid(), "2026-03-09", "2026-03-09")).AssertProblem(HttpStatusCode.NotFound, ErrorCodes.NotFound);
    }

    [Fact]
    [Trait("Rule", "RB-11")]
    public async Task CA_04_10_concurrent_submissions_one_wins_the_other_gets_insufficient_balance()
    {
        var (_, alice, client) = await AliceWithManagerAsync(5);
        var secondClient = await ClientForAsync(alice);
        HttpResponseMessage? concurrent = null;

        // Entrelacement : T1 a tout lu et contrôlé ; T2 s'exécute entièrement avant la validation de T1.
        Factory.Interleaver.BeforeNextSave(async () => concurrent = await SubmitAsync(secondClient, Cp, "2026-03-16", "2026-03-18"));
        var first = await SubmitAsync(client, Cp, "2026-03-09", "2026-03-11");

        await concurrent!.AssertStatus(HttpStatusCode.Created);
        await first.AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.InsufficientBalance);
        Assert.Equal(1, await QueryDbAsync(db => db.LeaveRequests.CountAsync()));
        var cp = (await (await client.GetAsync("/api/v1/me/balances")).Json()).EnumerateArray().Single(b => b.GetProperty("leaveType").GetProperty("code").GetString() == "CP");
        Assert.Equal(2m, cp.GetProperty("available").GetDecimal());
    }

    [Fact]
    public async Task CA_13_3_without_manager_submission_email_goes_to_all_active_hr_admins_except_requester()
    {
        var hr1 = await CreateUserAsync("Helene", "Rh", null, Role.HrAdmin);
        var hr2 = await CreateUserAsync("Hugo", "Rh", null, Role.HrAdmin);
        var client = await ClientForAsync(hr1);

        await SubmitOkAsync(client, LeaveType.SickId, "2026-03-09", "2026-03-09");

        var recipients = await QueryDbAsync(db => db.Notifications.Select(n => n.RecipientEmail).ToListAsync());
        Assert.Equal([hr2.Email], recipients);
    }

    [Fact]
    [Trait("Rule", "RB-16")]
    [Trait("Rule", "RB-17")]
    public async Task CA_04_8_and_13_2_submission_overlap_is_recorded_and_mentioned_in_manager_email()
    {
        var manager = await CreateUserAsync("Marc", "Durand", null, Role.Manager);
        var alice = await CreateUserAsync("Alice", "Martin", manager.Id);
        var bruno = await CreateUserAsync("Bruno", "Petit", manager.Id);
        await CreateApprovedAsync(bruno.Id, manager.Id, new(2026, 3, 10), new(2026, 3, 10));
        var client = await ClientForAsync(alice);

        var id = await SubmitOkAsync(client, LeaveType.SickId, "2026-03-09", "2026-03-11");

        var request = await QueryDbAsync(db => db.LeaveRequests.SingleAsync(r => r.Id == id));
        Assert.True(request.SubmissionOverlapAlert);
        Assert.Equal(1, request.SubmissionOverlapMaxAbsent);
        Assert.Equal(1, request.SubmissionOverlapTeamSize);
        var email = await QueryDbAsync(db => db.Notifications.SingleAsync());
        Assert.Contains("Chevauchement d'équipe supérieur à 50 %", email.Body);
    }

    private const System.Text.Json.JsonValueKind JsonValueKindNull = System.Text.Json.JsonValueKind.Null;
}
