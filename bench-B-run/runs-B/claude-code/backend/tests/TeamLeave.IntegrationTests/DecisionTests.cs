using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Notifications;
using TeamLeave.Domain.Users;
using TeamLeave.IntegrationTests.Support;

namespace TeamLeave.IntegrationTests;

/// <summary>
/// US-05, US-06, US-08 à US-11, US-14 : chevauchement à 50 %, autorisation du manager, débit, refus, annulation.
/// </summary>
[Trait("Rule", "RB-04")]
[Trait("Rule", "RB-12")]
[Trait("Rule", "RB-13")]
[Trait("Rule", "RB-15")]
[Trait("Rule", "RB-18")]
[Trait("Rule", "RB-19")]
[Trait("Rule", "RB-22")]
[Trait("Rule", "RB-25")]
public sealed class DecisionTests : IntegrationTest
{
    private static readonly Guid Cp = LeaveType.CpId;

    private sealed record Team(User Manager, User A, User B, User C, User D, HttpClient ManagerClient, HttpClient AClient);

    /// <summary>Équipe {A, B, C, D} du manager M (CA-09.1).</summary>
    private async Task<Team> TeamAsync(int aCpDays = 20)
    {
        var m = await CreateUserAsync("Marc", "Durand", null, Role.Manager);
        var a = await CreateUserAsync("Alice", "Martin", m.Id);
        var b = await CreateUserAsync("Bruno", "Petit", m.Id);
        var c = await CreateUserAsync("Chloe", "Roux", m.Id);
        var d = await CreateUserAsync("David", "Blanc", m.Id);
        await AllocateAsync(a.Id, Cp, aCpDays);
        return new Team(m, a, b, c, d, await ClientForAsync(m), await ClientForAsync(a));
    }

    private static Task<HttpResponseMessage> ApproveAsync(HttpClient client, Guid id, bool ack = false, string? comment = null) =>
        client.PostAsJsonAsync($"/api/v1/leave-requests/{id}/approve", new { comment, overlapAcknowledged = ack });

    private static Task<HttpResponseMessage> RejectAsync(HttpClient client, Guid id, string? comment) =>
        client.PostAsJsonAsync($"/api/v1/leave-requests/{id}/reject", new { comment });

    private static decimal CpAmount(JsonElement balances) =>
        balances.EnumerateArray().Single(b => b.GetProperty("leaveType").GetProperty("code").GetString() == "CP").GetProperty("amount").GetDecimal();

    // ------------------------------------------------------------------ Chevauchement (RB-16 à RB-20)

    [Fact]
    [Trait("Rule", "RB-16")]
    [Trait("Rule", "RB-17")]
    [Trait("Rule", "RB-20")]
    public async Task CA_09_1_manager_sees_alert_with_day_rate_and_absent_colleagues()
    {
        var t = await TeamAsync();
        await CreateApprovedAsync(t.B.Id, t.Manager.Id, new(2026, 3, 10), new(2026, 3, 10));
        await CreateApprovedAsync(t.C.Id, t.Manager.Id, new(2026, 3, 10), new(2026, 3, 10));
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-11");

        var detail = await (await t.ManagerClient.GetAsync($"/api/v1/leave-requests/{id}")).AssertStatus(HttpStatusCode.OK);
        var overlap = detail.GetProperty("overlap");
        Assert.True(overlap.GetProperty("isAlert").GetBoolean());
        Assert.Equal(3, overlap.GetProperty("teamSize").GetInt32());
        Assert.Equal(67, overlap.GetProperty("maxRate").GetProperty("percent").GetInt32());
        var day = Assert.Single(overlap.GetProperty("daysOverThreshold").EnumerateArray());
        Assert.Equal("2026-03-10", day.GetProperty("date").GetString());
        Assert.Equal(2, day.GetProperty("rate").GetProperty("absent").GetInt32());
        Assert.Equal(["Bruno Petit", "Chloe Roux"], day.GetProperty("absentColleagues").EnumerateArray().Select(c => c.GetProperty("fullName").GetString()));

        var dedicated = await (await t.ManagerClient.GetAsync($"/api/v1/leave-requests/{id}/overlap")).AssertStatus(HttpStatusCode.OK);
        Assert.True(dedicated.GetProperty("isAlert").GetBoolean());
    }

    [Fact]
    public async Task CA_09_2_fifty_percent_is_not_an_alert()
    {
        var t = await TeamAsync();
        var e = await CreateUserAsync("Emma", "Noir", t.Manager.Id);
        await CreateApprovedAsync(t.B.Id, t.Manager.Id, new(2026, 3, 9), new(2026, 3, 9));
        await CreateApprovedAsync(t.C.Id, t.Manager.Id, new(2026, 3, 9), new(2026, 3, 9));
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-09");

        var overlap = (await (await t.ManagerClient.GetAsync($"/api/v1/leave-requests/{id}")).Json()).GetProperty("overlap");

        Assert.False(overlap.GetProperty("isAlert").GetBoolean());
        Assert.Equal(4, overlap.GetProperty("teamSize").GetInt32());
        Assert.Equal(50, overlap.GetProperty("maxRate").GetProperty("percent").GetInt32());
        Assert.NotNull(e);
    }

    [Fact]
    public async Task CA_09_3_analysis_is_recomputed_after_new_approvals()
    {
        var t = await TeamAsync();
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-09");
        var before = (await (await t.ManagerClient.GetAsync($"/api/v1/leave-requests/{id}")).Json()).GetProperty("overlap");
        Assert.False(before.GetProperty("isAlert").GetBoolean());

        await CreateApprovedAsync(t.B.Id, t.Manager.Id, new(2026, 3, 9), new(2026, 3, 9));
        await CreateApprovedAsync(t.C.Id, t.Manager.Id, new(2026, 3, 9), new(2026, 3, 9));

        var after = (await (await t.ManagerClient.GetAsync($"/api/v1/leave-requests/{id}")).Json()).GetProperty("overlap");
        Assert.True(after.GetProperty("isAlert").GetBoolean());
    }

    [Fact]
    public async Task CA_09_4_and_09_5_approval_in_alert_requires_acknowledgement_which_is_recorded()
    {
        var t = await TeamAsync();
        await CreateApprovedAsync(t.B.Id, t.Manager.Id, new(2026, 3, 10), new(2026, 3, 10));
        await CreateApprovedAsync(t.C.Id, t.Manager.Id, new(2026, 3, 10), new(2026, 3, 10));
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-11");

        var refused = await (await ApproveAsync(t.ManagerClient, id)).AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.OverlapNotAcknowledged);
        Assert.True(refused.GetProperty("overlap").GetProperty("isAlert").GetBoolean());
        Assert.Equal(LeaveRequestStatus.Pending, await QueryDbAsync(db => db.LeaveRequests.Where(r => r.Id == id).Select(r => r.Status).SingleAsync()));

        var approved = await (await ApproveAsync(t.ManagerClient, id, ack: true)).AssertStatus(HttpStatusCode.OK);
        Assert.Equal("Approved", approved.GetProperty("status").GetString());
        var ack = approved.GetProperty("decision").GetProperty("overlapAcknowledgement");
        Assert.Equal(t.Manager.Id, ack.GetProperty("acknowledgedBy").GetProperty("id").GetGuid());
        Assert.Equal(2, ack.GetProperty("rate").GetProperty("absent").GetInt32());
        Assert.Equal(3, ack.GetProperty("rate").GetProperty("teamSize").GetInt32());
    }

    [Fact]
    public async Task CA_05_3_requester_never_sees_the_overlap_analysis()
    {
        var t = await TeamAsync();
        await CreateApprovedAsync(t.B.Id, t.Manager.Id, new(2026, 3, 10), new(2026, 3, 10));
        await CreateApprovedAsync(t.C.Id, t.Manager.Id, new(2026, 3, 10), new(2026, 3, 10));
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-11");

        var mine = await (await t.AClient.GetAsync($"/api/v1/leave-requests/{id}")).AssertStatus(HttpStatusCode.OK);
        Assert.Equal(JsonValueKind.Null, mine.GetProperty("overlap").ValueKind);
        var list = await (await t.AClient.GetAsync("/api/v1/leave-requests?scope=mine")).AssertStatus(HttpStatusCode.OK);
        Assert.Equal(JsonValueKind.Null, list.GetProperty("items")[0].GetProperty("overlapAlert").ValueKind);
        await (await t.AClient.GetAsync($"/api/v1/leave-requests/{id}/overlap")).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
    }

    // ------------------------------------------------------------------ File du manager (US-08)

    [Fact]
    [Trait("Rule", "RB-03")]
    public async Task CA_08_1_to_08_3_queue_lists_only_direct_reports_with_alert_and_never_own_request()
    {
        var t = await TeamAsync();
        await AllocateAsync(t.B.Id, Cp, 10);
        var bigBoss = await CreateUserAsync("Paul", "Boss", null, Role.Manager);
        await WithDbAsync(async db => (await db.Users.FirstAsync(u => u.Id == t.Manager.Id)).SetManager(bigBoss.Id));
        var otherManager = await CreateUserAsync("Olga", "Autre", null, Role.Manager);
        var outsider = await CreateUserAsync("Oscar", "Dehors", otherManager.Id);
        await AllocateAsync(outsider.Id, Cp, 10);
        await AllocateAsync(t.Manager.Id, Cp, 10);
        await CreateApprovedAsync(t.C.Id, t.Manager.Id, new(2026, 3, 9), new(2026, 3, 9));
        await CreateApprovedAsync(t.D.Id, t.Manager.Id, new(2026, 3, 9), new(2026, 3, 9));

        var first = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-09");
        var second = await SubmitOkAsync(await ClientForAsync(t.B), Cp, "2026-03-16", "2026-03-16");
        await SubmitOkAsync(await ClientForAsync(outsider), Cp, "2026-03-09", "2026-03-09");
        var managerClient = await ClientForAsync(t.Manager);
        await SubmitOkAsync(managerClient, Cp, "2026-03-20", "2026-03-20");

        var queue = await (await managerClient.GetAsync("/api/v1/leave-requests?scope=to-decide")).AssertStatus(HttpStatusCode.OK);

        var items = queue.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal([first, second], items.Select(i => i.GetProperty("id").GetGuid()));
        Assert.True(items[0].GetProperty("overlapAlert").GetProperty("isAlert").GetBoolean());
        Assert.Equal(67, items[0].GetProperty("overlapAlert").GetProperty("maxRatePercent").GetInt32());
        Assert.False(items[1].GetProperty("overlapAlert").GetProperty("isAlert").GetBoolean());
    }

    [Fact]
    public async Task To_decide_queue_is_forbidden_for_plain_employees()
    {
        var t = await TeamAsync();

        await (await t.AClient.GetAsync("/api/v1/leave-requests?scope=to-decide")).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
        await (await t.AClient.GetAsync("/api/v1/leave-requests?scope=all")).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
        await (await t.AClient.GetAsync("/api/v1/leave-requests?scope=nope")).AssertProblem(HttpStatusCode.BadRequest, ErrorCodes.ValidationError);
    }

    // ------------------------------------------------------------------ Approbation (US-10)

    [Fact]
    [Trait("Rule", "RB-11")]
    public async Task CA_10_1_10_2_approval_debits_balance_traces_movement_and_emails_employee()
    {
        var t = await TeamAsync(aCpDays: 20);
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-13");

        var body = await (await ApproveAsync(t.ManagerClient, id, comment: "Bon repos")).AssertStatus(HttpStatusCode.OK);

        Assert.Equal("Approved", body.GetProperty("status").GetString());
        Assert.Equal(t.Manager.Id, body.GetProperty("decision").GetProperty("decidedBy").GetProperty("id").GetGuid());
        Assert.Equal(15m, CpAmount(await (await t.AClient.GetAsync("/api/v1/me/balances")).Json()));
        var movements = await (await t.AClient.GetAsync($"/api/v1/me/balances/{Cp}/movements")).AssertStatus(HttpStatusCode.OK);
        var debit = movements.GetProperty("items").EnumerateArray().Last();
        Assert.Equal("Debit", debit.GetProperty("kind").GetString());
        Assert.Equal(-5m, debit.GetProperty("quantity").GetDecimal());
        Assert.Equal(15m, debit.GetProperty("balanceAfter").GetDecimal());
        Assert.Equal(id, debit.GetProperty("leaveRequestId").GetGuid());

        var email = await QueryDbAsync(db => db.Notifications.SingleAsync(n => n.EventType == NotificationEventType.Approval));
        Assert.Equal(t.A.Email, email.RecipientEmail);
        Assert.Equal("[TeamLeave] Demande approuvée", email.Subject);
        Assert.DoesNotContain("Chevauchement", email.Body);
    }

    [Fact]
    public async Task CA_10_3_concurrent_double_approval_debits_once_and_loser_gets_409()
    {
        var t = await TeamAsync(aCpDays: 20);
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-13");
        var secondManagerClient = await ClientForAsync(t.Manager);
        HttpResponseMessage? concurrent = null;

        Factory.Interleaver.BeforeNextSave(async () => concurrent = await ApproveAsync(secondManagerClient, id));
        var first = await ApproveAsync(t.ManagerClient, id);

        await concurrent!.AssertStatus(HttpStatusCode.OK);
        await first.AssertProblem(HttpStatusCode.Conflict, ErrorCodes.InvalidStatusTransition);
        Assert.Equal(1, await QueryDbAsync(db => db.BalanceMovements.CountAsync(m => m.LeaveRequestId == id && m.Kind == MovementKind.Debit)));
        Assert.Equal(15m, CpAmount(await (await t.AClient.GetAsync("/api/v1/me/balances")).Json()));
        // Réconciliation (§4.2) : solde = somme des mouvements.
        var reconciled = await QueryDbAsync(async db =>
        {
            var balance = await db.Balances.SingleAsync(b => b.UserId == t.A.Id && b.LeaveTypeId == Cp);
            var sum = await db.BalanceMovements.Where(m => m.BalanceId == balance.Id).SumAsync(m => m.QuantityHundredths);
            return balance.AmountHundredths == sum;
        });
        Assert.True(reconciled);
    }

    [Fact]
    public async Task CA_10_4_untracked_type_approval_creates_no_movement()
    {
        var t = await TeamAsync();
        var id = await SubmitOkAsync(t.AClient, LeaveType.SickId, "2026-03-09", "2026-03-10");

        await (await ApproveAsync(t.ManagerClient, id)).AssertStatus(HttpStatusCode.OK);

        Assert.Equal(0, await QueryDbAsync(db => db.BalanceMovements.CountAsync(m => m.LeaveRequestId == id)));
    }

    [Fact]
    public async Task CA_10_5_manager_of_another_team_gets_403_not_approver()
    {
        var t = await TeamAsync();
        var otherManager = await CreateUserAsync("Olga", "Autre", null, Role.Manager);
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-09");
        var otherClient = await ClientForAsync(otherManager);

        await (await ApproveAsync(otherClient, id)).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.NotApprover);
        await (await RejectAsync(otherClient, id, "Non")).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.NotApprover);
        // Lecture hors périmètre : 404 (ENF-S7, GAP-01).
        await (await otherClient.GetAsync($"/api/v1/leave-requests/{id}")).AssertProblem(HttpStatusCode.NotFound, ErrorCodes.NotFound);
    }

    [Fact]
    public async Task CA_10_6_already_decided_request_cannot_be_approved_again()
    {
        var t = await TeamAsync();
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-09");
        await (await ApproveAsync(t.ManagerClient, id)).AssertStatus(HttpStatusCode.OK);

        await (await ApproveAsync(t.ManagerClient, id)).AssertProblem(HttpStatusCode.Conflict, ErrorCodes.InvalidStatusTransition);
    }

    [Fact]
    public async Task Employee_without_manager_role_gets_403_forbidden_before_loading()
    {
        var t = await TeamAsync();
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-09");
        var bClient = await ClientForAsync(t.B);

        await (await ApproveAsync(bClient, id)).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
        await (await ApproveAsync(bClient, Guid.NewGuid())).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task RB_04_self_approval_is_forbidden_even_for_hr_admin_without_manager()
    {
        var hr = await CreateUserAsync("Helene", "Rh", null, Role.HrAdmin, Role.Manager);
        var hrClient = await ClientForAsync(hr);
        var id = await SubmitOkAsync(hrClient, LeaveType.SickId, "2026-03-09", "2026-03-09");

        await (await ApproveAsync(hrClient, id)).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.SelfApprovalForbidden);
        await (await RejectAsync(hrClient, id, "Non")).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.SelfApprovalForbidden);
    }

    [Fact]
    public async Task H_31_hr_admin_decides_only_for_employees_without_manager()
    {
        var t = await TeamAsync();
        var hr = await CreateUserAsync("Helene", "Rh", null, Role.HrAdmin);
        var hrClient = await ClientForAsync(hr);
        var loner = await CreateUserAsync("Luc", "Seul");
        var withManager = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-09");
        var withoutManager = await SubmitOkAsync(await ClientForAsync(loner), LeaveType.SickId, "2026-03-09", "2026-03-09");

        await (await ApproveAsync(hrClient, withManager)).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.NotApprover);
        await (await hrClient.GetAsync($"/api/v1/leave-requests/{withManager}")).AssertStatus(HttpStatusCode.OK); // lecture RH (H-30)
        var queue = await (await hrClient.GetAsync("/api/v1/leave-requests?scope=to-decide")).AssertStatus(HttpStatusCode.OK);
        Assert.Equal([withoutManager], queue.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()));
        await (await ApproveAsync(hrClient, withoutManager)).AssertStatus(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Rule", "RB-03")]
    public async Task CA_17_6_after_manager_change_new_manager_decides_and_former_gets_403()
    {
        var t = await TeamAsync();
        var hr = await CreateUserAsync("Helene", "Rh", null, Role.HrAdmin);
        var m2 = await CreateUserAsync("Nina", "Nouvelle", null, Role.Manager);
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-09");
        var hrClient = await ClientForAsync(hr);

        await (await hrClient.PutAsJsonAsync($"/api/v1/users/{t.A.Id}", new
        {
            email = t.A.Email,
            firstName = "Alice",
            lastName = "Martin",
            roles = new[] { "Employee" },
            managerId = m2.Id,
        })).AssertStatus(HttpStatusCode.OK);

        var m2Client = await ClientForAsync(m2);
        var queue = await (await m2Client.GetAsync("/api/v1/leave-requests?scope=to-decide")).AssertStatus(HttpStatusCode.OK);
        Assert.Contains(queue.GetProperty("items").EnumerateArray(), i => i.GetProperty("id").GetGuid() == id);
        await (await ApproveAsync(t.ManagerClient, id)).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.NotApprover);
        await (await ApproveAsync(m2Client, id)).AssertStatus(HttpStatusCode.OK);
    }

    // ------------------------------------------------------------------ Refus (US-11)

    [Fact]
    public async Task CA_11_1_and_14_2_rejection_records_comment_keeps_balance_and_emails_employee()
    {
        var t = await TeamAsync(aCpDays: 20);
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-13");

        var body = await (await RejectAsync(t.ManagerClient, id, "Période de clôture")).AssertStatus(HttpStatusCode.OK);

        Assert.Equal("Rejected", body.GetProperty("status").GetString());
        Assert.Equal("Période de clôture", body.GetProperty("decision").GetProperty("comment").GetString());
        Assert.Equal(20m, CpAmount(await (await t.AClient.GetAsync("/api/v1/me/balances")).Json()));
        var email = await QueryDbAsync(db => db.Notifications.SingleAsync(n => n.EventType == NotificationEventType.Rejection));
        Assert.Equal(t.A.Email, email.RecipientEmail);
        Assert.Contains("Période de clôture", email.Body);

        // CA-05.2 : le demandeur voit commentaire, décideur et date.
        var mine = await (await t.AClient.GetAsync($"/api/v1/leave-requests/{id}")).AssertStatus(HttpStatusCode.OK);
        Assert.Equal("Marc Durand", mine.GetProperty("decision").GetProperty("decidedBy").GetProperty("fullName").GetString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData(null)]
    public async Task CA_11_2_rejection_without_comment_is_400_and_request_stays_pending(string? comment)
    {
        var t = await TeamAsync();
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-09");

        await (await RejectAsync(t.ManagerClient, id, comment)).AssertProblem(HttpStatusCode.BadRequest, ErrorCodes.RejectionCommentRequired);

        Assert.Equal(LeaveRequestStatus.Pending, await QueryDbAsync(db => db.LeaveRequests.Where(r => r.Id == id).Select(r => r.Status).SingleAsync()));
    }

    [Fact]
    public async Task CA_11_4_rejection_in_alert_needs_no_acknowledgement()
    {
        var t = await TeamAsync();
        await CreateApprovedAsync(t.B.Id, t.Manager.Id, new(2026, 3, 9), new(2026, 3, 9));
        await CreateApprovedAsync(t.C.Id, t.Manager.Id, new(2026, 3, 9), new(2026, 3, 9));
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-09");

        await (await RejectAsync(t.ManagerClient, id, "Trop d'absents")).AssertStatus(HttpStatusCode.OK);
    }

    // ------------------------------------------------------------------ Consultation et annulation (US-05, US-06)

    [Fact]
    public async Task CA_05_1_and_05_4_my_requests_sorted_by_start_desc_and_others_are_404()
    {
        var t = await TeamAsync();
        var r1 = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-09");
        var r2 = await SubmitOkAsync(t.AClient, Cp, "2026-04-06", "2026-04-07");
        var r3 = await SubmitOkAsync(t.AClient, Cp, "2026-03-20", "2026-03-20");
        await ApproveAsync(t.ManagerClient, r2);
        await RejectAsync(t.ManagerClient, r3, "Non");

        var list = await (await t.AClient.GetAsync("/api/v1/leave-requests?scope=mine")).AssertStatus(HttpStatusCode.OK);

        Assert.Equal([r2, r3, r1], list.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()));
        Assert.Equal(3, list.GetProperty("totalCount").GetInt32());
        await (await (await ClientForAsync(t.B)).GetAsync($"/api/v1/leave-requests/{r1}")).AssertProblem(HttpStatusCode.NotFound, ErrorCodes.NotFound);
    }

    [Fact]
    [Trait("Rule", "RB-14")]
    public async Task CA_06_1_cancelling_pending_request_frees_availability_without_email()
    {
        var t = await TeamAsync(aCpDays: 10);
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-11");

        var body = await (await t.AClient.PostAsync($"/api/v1/leave-requests/{id}/cancel", null)).AssertStatus(HttpStatusCode.OK);

        Assert.Equal("Cancelled", body.GetProperty("status").GetString());
        var cp = (await (await t.AClient.GetAsync("/api/v1/me/balances")).Json()).EnumerateArray().Single(b => b.GetProperty("leaveType").GetProperty("code").GetString() == "CP");
        Assert.Equal(10m, cp.GetProperty("amount").GetDecimal());
        Assert.Equal(10m, cp.GetProperty("available").GetDecimal());
        // CA-14.3 : seul l'email de soumission existe.
        Assert.Equal(1, await QueryDbAsync(db => db.Notifications.CountAsync()));
    }

    [Fact]
    [Trait("Rule", "RB-14")]
    public async Task CA_06_2_cancelling_future_approved_request_recredits_and_emails_manager()
    {
        var t = await TeamAsync(aCpDays: 15);
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-11");
        await (await ApproveAsync(t.ManagerClient, id)).AssertStatus(HttpStatusCode.OK);
        Assert.Equal(12m, CpAmount(await (await t.AClient.GetAsync("/api/v1/me/balances")).Json()));

        var body = await (await t.AClient.PostAsync($"/api/v1/leave-requests/{id}/cancel", null)).AssertStatus(HttpStatusCode.OK);

        Assert.Equal("Cancelled", body.GetProperty("status").GetString());
        Assert.Equal(15m, CpAmount(await (await t.AClient.GetAsync("/api/v1/me/balances")).Json()));
        var recredit = await QueryDbAsync(db => db.BalanceMovements.SingleAsync(m => m.LeaveRequestId == id && m.Kind == MovementKind.Recredit));
        Assert.Equal(300, recredit.QuantityHundredths);
        var email = await QueryDbAsync(db => db.Notifications.SingleAsync(n => n.EventType == NotificationEventType.ApprovedCancellation));
        Assert.Equal(t.Manager.Email, email.RecipientEmail);
    }

    [Fact]
    public async Task CA_06_3_cancelling_started_approved_request_is_422_already_started()
    {
        var t = await TeamAsync();
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-02", "2026-03-03");
        await (await ApproveAsync(t.ManagerClient, id)).AssertStatus(HttpStatusCode.OK);

        await (await t.AClient.PostAsync($"/api/v1/leave-requests/{id}/cancel", null)).AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.AlreadyStarted);

        Assert.Equal(LeaveRequestStatus.Approved, await QueryDbAsync(db => db.LeaveRequests.Where(r => r.Id == id).Select(r => r.Status).SingleAsync()));
    }

    [Fact]
    public async Task CA_06_4_cancelling_rejected_request_is_409_and_others_cannot_cancel()
    {
        var t = await TeamAsync();
        var id = await SubmitOkAsync(t.AClient, Cp, "2026-03-09", "2026-03-09");
        await (await t.ManagerClient.PostAsync($"/api/v1/leave-requests/{id}/cancel", null)).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.NotRequester);
        await (await (await ClientForAsync(t.B)).PostAsync($"/api/v1/leave-requests/{id}/cancel", null)).AssertProblem(HttpStatusCode.NotFound, ErrorCodes.NotFound);
        await RejectAsync(t.ManagerClient, id, "Non");

        await (await t.AClient.PostAsync($"/api/v1/leave-requests/{id}/cancel", null)).AssertProblem(HttpStatusCode.Conflict, ErrorCodes.InvalidStatusTransition);
    }
}
