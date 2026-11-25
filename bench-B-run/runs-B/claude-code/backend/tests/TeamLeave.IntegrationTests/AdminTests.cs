using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Users;
using TeamLeave.IntegrationTests.Support;

namespace TeamLeave.IntegrationTests;

/// <summary>US-15 à US-19 : administration RH (types, soldes, utilisateurs, jours fériés, journaux).</summary>
[Trait("Rule", "RB-24")]
[Trait("Rule", "RB-26")]
public sealed class AdminTests : IntegrationTest
{
    private async Task<(User Hr, HttpClient Client)> HrAsync()
    {
        var hr = await CreateUserAsync("Helene", "Rh", null, Role.HrAdmin);
        return (hr, await ClientForAsync(hr));
    }

    // ------------------------------------------------------------------ Types (US-15)

    [Fact]
    [Trait("Rule", "RB-08")]
    [Trait("Rule", "RB-09")]
    public async Task CA_15_1_created_tracked_type_is_offered_and_gets_zero_balances()
    {
        var (_, hrClient) = await HrAsync();
        var alice = await CreateUserAsync("Alice", "Martin");

        var created = await (await hrClient.PostAsJsonAsync("/api/v1/leave-types", new { code = "FORM", label = "Formation", isTracked = true, allowsRetroactive = false }))
            .AssertStatus(HttpStatusCode.Created);

        Assert.False(created.GetProperty("isSystem").GetBoolean());
        var aliceClient = await ClientForAsync(alice);
        var types = await (await aliceClient.GetAsync("/api/v1/leave-types")).AssertStatus(HttpStatusCode.OK);
        Assert.Contains(types.EnumerateArray(), t => t.GetProperty("code").GetString() == "FORM");
        var balances = await (await aliceClient.GetAsync("/api/v1/me/balances")).Json();
        Assert.Contains(balances.EnumerateArray(), b => b.GetProperty("leaveType").GetProperty("code").GetString() == "FORM" && b.GetProperty("amount").GetDecimal() == 0m);
    }

    [Fact]
    public async Task CA_15_2_duplicate_code_is_409_and_bad_code_is_400()
    {
        var (_, hrClient) = await HrAsync();

        await (await hrClient.PostAsJsonAsync("/api/v1/leave-types", new { code = "CP", label = "Doublon", isTracked = false, allowsRetroactive = false }))
            .AssertProblem(HttpStatusCode.Conflict, ErrorCodes.LeaveTypeCodeAlreadyUsed);
        await (await hrClient.PostAsJsonAsync("/api/v1/leave-types", new { code = "x", label = "Mauvais", isTracked = false, allowsRetroactive = false }))
            .AssertProblem(HttpStatusCode.BadRequest, ErrorCodes.ValidationError);
    }

    [Fact]
    public async Task CA_15_3_used_type_cannot_be_deleted_system_type_neither_unused_type_can()
    {
        var (hr, hrClient) = await HrAsync();
        var form = (await (await hrClient.PostAsJsonAsync("/api/v1/leave-types", new { code = "FORM", label = "Formation", isTracked = false, allowsRetroactive = false })).Json()).GetProperty("id").GetGuid();
        var unused = (await (await hrClient.PostAsJsonAsync("/api/v1/leave-types", new { code = "DEMEN", label = "Déménagement", isTracked = true, allowsRetroactive = false })).Json()).GetProperty("id").GetGuid();
        await SubmitOkAsync(hrClient, form, "2026-03-09", "2026-03-09");

        await (await hrClient.DeleteAsync($"/api/v1/leave-types/{form}")).AssertProblem(HttpStatusCode.Conflict, ErrorCodes.LeaveTypeInUse);
        await (await hrClient.DeleteAsync($"/api/v1/leave-types/{LeaveType.CpId}")).AssertProblem(HttpStatusCode.Conflict, ErrorCodes.SystemLeaveType);
        Assert.Equal(HttpStatusCode.NoContent, (await hrClient.DeleteAsync($"/api/v1/leave-types/{unused}")).StatusCode);
        Assert.NotNull(hr);
    }

    [Fact]
    public async Task CA_15_4_deactivated_type_is_hidden_from_employees_but_existing_requests_stay_decidable()
    {
        var (_, hrClient) = await HrAsync();
        var m = await CreateUserAsync("Marc", "Durand", null, Role.Manager);
        var alice = await CreateUserAsync("Alice", "Martin", m.Id);
        var aliceClient = await ClientForAsync(alice);
        var id = await SubmitOkAsync(aliceClient, LeaveType.OtherId, "2026-03-09", "2026-03-09");

        var updated = await (await hrClient.PutAsJsonAsync($"/api/v1/leave-types/{LeaveType.OtherId}", new { label = "Autre", isTracked = false, allowsRetroactive = false, isActive = false }))
            .AssertStatus(HttpStatusCode.OK);
        Assert.False(updated.GetProperty("isActive").GetBoolean());

        var types = await (await aliceClient.GetAsync("/api/v1/leave-types?includeInactive=true")).Json();
        Assert.DoesNotContain(types.EnumerateArray(), t => t.GetProperty("code").GetString() == "AUTRE");
        var hrTypes = await (await hrClient.GetAsync("/api/v1/leave-types?includeInactive=true")).Json();
        Assert.Contains(hrTypes.EnumerateArray(), t => t.GetProperty("code").GetString() == "AUTRE");
        await (await (await ClientForAsync(m)).PostAsJsonAsync($"/api/v1/leave-requests/{id}/approve", new { })).AssertStatus(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GAP_05_tracking_change_with_pending_requests_or_nonzero_balances_is_refused()
    {
        var (hr, hrClient) = await HrAsync();
        await AllocateAsync(hr.Id, LeaveType.RttId, 2);

        await (await hrClient.PutAsJsonAsync($"/api/v1/leave-types/{LeaveType.RttId}", new { label = "RTT", isTracked = false, allowsRetroactive = false, isActive = true }))
            .AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.LeaveTypeHasBalances);
        await SubmitOkAsync(hrClient, LeaveType.SickId, "2026-03-09", "2026-03-09");
        await (await hrClient.PutAsJsonAsync($"/api/v1/leave-types/{LeaveType.SickId}", new { label = "Maladie", isTracked = true, allowsRetroactive = true, isActive = true }))
            .AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.LeaveTypeHasPendingRequests);
    }

    [Fact]
    public async Task CA_15_5_non_hr_cannot_create_type()
    {
        var alice = await CreateUserAsync("Alice", "Martin", null, Role.Manager);

        await (await (await ClientForAsync(alice)).PostAsJsonAsync("/api/v1/leave-types", new { code = "FORM", label = "Formation", isTracked = true, allowsRetroactive = false }))
            .AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
    }

    // ------------------------------------------------------------------ Soldes (US-16)

    [Fact]
    [Trait("Rule", "RB-11")]
    [Trait("Rule", "RB-25")]
    public async Task CA_16_1_and_16_5_allocation_is_traced_and_statement_is_chronological()
    {
        var (hr, hrClient) = await HrAsync();
        var alice = await CreateUserAsync("Alice", "Martin");
        var url = $"/api/v1/users/{alice.Id}/balances/{LeaveType.CpId}/movements";

        var movement = await (await hrClient.PostAsJsonAsync(url, new { kind = "Allocation", quantity = 25.0m, reason = "Droits 2026" })).AssertStatus(HttpStatusCode.Created);
        await (await hrClient.PostAsJsonAsync(url, new { kind = "Adjustment", quantity = -2.5m, reason = "Correction" })).AssertStatus(HttpStatusCode.Created);

        Assert.Equal(25m, movement.GetProperty("balanceAfter").GetDecimal());
        Assert.Equal(hr.Id, movement.GetProperty("author").GetProperty("id").GetGuid());
        var statement = await (await hrClient.GetAsync(url)).AssertStatus(HttpStatusCode.OK);
        var items = statement.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(["Allocation", "Adjustment"], items.Select(i => i.GetProperty("kind").GetString()));
        Assert.Equal([25m, 22.5m], items.Select(i => i.GetProperty("balanceAfter").GetDecimal()));
        var balances = await (await hrClient.GetAsync($"/api/v1/users/{alice.Id}/balances")).Json();
        Assert.Contains(balances.EnumerateArray(), b => b.GetProperty("leaveType").GetProperty("code").GetString() == "CP" && b.GetProperty("amount").GetDecimal() == 22.5m);
    }

    [Fact]
    [Trait("Rule", "RB-11")]
    public async Task CA_16_2_adjustment_below_pending_is_refused()
    {
        var (_, hrClient) = await HrAsync();
        var alice = await CreateUserAsync("Alice", "Martin");
        await AllocateAsync(alice.Id, LeaveType.CpId, 10);
        await SubmitOkAsync(await ClientForAsync(alice), LeaveType.CpId, "2026-03-09", "2026-03-12");

        await (await hrClient.PostAsJsonAsync($"/api/v1/users/{alice.Id}/balances/{LeaveType.CpId}/movements", new { kind = "Adjustment", quantity = -8m, reason = "Erreur" }))
            .AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.BalanceBelowPending);
    }

    [Fact]
    public async Task CA_16_3_and_16_4_reason_is_required_and_untracked_type_is_refused()
    {
        var (_, hrClient) = await HrAsync();
        var alice = await CreateUserAsync("Alice", "Martin");

        await (await hrClient.PostAsJsonAsync($"/api/v1/users/{alice.Id}/balances/{LeaveType.CpId}/movements", new { kind = "Adjustment", quantity = 1m }))
            .AssertProblem(HttpStatusCode.BadRequest, ErrorCodes.ValidationError);
        await (await hrClient.PostAsJsonAsync($"/api/v1/users/{alice.Id}/balances/{LeaveType.CpId}/movements", new { kind = "Debit", quantity = 1m, reason = "x" }))
            .AssertProblem(HttpStatusCode.BadRequest, ErrorCodes.ValidationError);
        await (await hrClient.PostAsJsonAsync($"/api/v1/users/{alice.Id}/balances/{LeaveType.CpId}/movements", new { kind = "Allocation", quantity = 1.255m, reason = "x" }))
            .AssertProblem(HttpStatusCode.BadRequest, ErrorCodes.ValidationError);
        await (await hrClient.PostAsJsonAsync($"/api/v1/users/{alice.Id}/balances/{LeaveType.SickId}/movements", new { kind = "Allocation", quantity = 5m, reason = "x" }))
            .AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.LeaveTypeNotTracked);
        await (await hrClient.GetAsync($"/api/v1/users/{alice.Id}/balances/{LeaveType.SickId}/movements")).AssertProblem(HttpStatusCode.NotFound, ErrorCodes.NotFound);
    }

    // ------------------------------------------------------------------ Utilisateurs (US-17)

    [Fact]
    [Trait("Rule", "RB-01")]
    [Trait("Rule", "RB-03")]
    public async Task CA_17_1_and_17_2_created_user_joins_manager_team_and_duplicate_email_is_409()
    {
        var (_, hrClient) = await HrAsync();
        var m = await CreateUserAsync("Marc", "Durand", null, Role.Manager);
        var payload = new { email = "alice.martin@pme.fr", firstName = "Alice", lastName = "Martin", roles = new[] { "Employee" }, managerId = m.Id, temporaryPassword = "Bienvenue-2026-xK7p" };

        var created = await (await hrClient.PostAsJsonAsync("/api/v1/users", payload)).AssertStatus(HttpStatusCode.Created);

        Assert.Equal("Marc Durand", created.GetProperty("manager").GetProperty("fullName").GetString());
        var team = await (await hrClient.GetAsync($"/api/v1/users?managerId={m.Id}")).AssertStatus(HttpStatusCode.OK);
        Assert.Contains(team.GetProperty("items").EnumerateArray(), u => u.GetProperty("email").GetString() == "alice.martin@pme.fr");
        var manager = await (await hrClient.GetAsync($"/api/v1/users/{m.Id}")).Json();
        Assert.Equal(1, manager.GetProperty("activeReportsCount").GetInt32());

        await (await hrClient.PostAsJsonAsync("/api/v1/users", payload with { email = "ALICE.MARTIN@pme.fr" }))
            .AssertProblem(HttpStatusCode.Conflict, ErrorCodes.EmailAlreadyUsed);
    }

    [Fact]
    [Trait("Rule", "RB-03")]
    public async Task CA_17_3_hierarchy_cycle_and_invalid_manager_are_refused()
    {
        var (_, hrClient) = await HrAsync();
        var m = await CreateUserAsync("Marc", "Durand", null, Role.Manager);
        var n = await CreateUserAsync("Nina", "Sub", m.Id, Role.Manager);
        var plain = await CreateUserAsync("Paul", "Simple");

        await (await hrClient.PutAsJsonAsync($"/api/v1/users/{m.Id}", new { email = m.Email, firstName = "Marc", lastName = "Durand", roles = new[] { "Manager" }, managerId = n.Id }))
            .AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.HierarchyCycle);
        await (await hrClient.PutAsJsonAsync($"/api/v1/users/{m.Id}", new { email = m.Email, firstName = "Marc", lastName = "Durand", roles = new[] { "Manager" }, managerId = m.Id }))
            .AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.HierarchyCycle);
        await (await hrClient.PutAsJsonAsync($"/api/v1/users/{m.Id}", new { email = m.Email, firstName = "Marc", lastName = "Durand", roles = new[] { "Manager" }, managerId = plain.Id }))
            .AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.InvalidManager);
        await (await hrClient.PutAsJsonAsync($"/api/v1/users/{m.Id}", new { email = m.Email, firstName = "Marc", lastName = "Durand", roles = Array.Empty<string>(), managerId = (Guid?)null }))
            .AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.HasActiveReports);
    }

    [Fact]
    [Trait("Rule", "RB-27")]
    public async Task CA_17_4_user_with_active_reports_cannot_be_deactivated()
    {
        var (_, hrClient) = await HrAsync();
        var m = await CreateUserAsync("Marc", "Durand", null, Role.Manager);
        await CreateUserAsync("Alice", "Martin", m.Id);
        await CreateUserAsync("Bruno", "Petit", m.Id);

        await (await hrClient.PostAsync($"/api/v1/users/{m.Id}/deactivate", null)).AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.HasActiveReports);
    }

    [Fact]
    [Trait("Rule", "RB-27")]
    public async Task CA_17_5_deactivation_cancels_pending_requests_and_revokes_tokens()
    {
        var (_, hrClient) = await HrAsync();
        var m = await CreateUserAsync("Marc", "Durand", null, Role.Manager);
        var alice = await CreateUserAsync("Alice", "Martin", m.Id);
        var aliceClient = await ClientForAsync(alice);
        var id = await SubmitOkAsync(aliceClient, LeaveType.SickId, "2026-03-09", "2026-03-09");

        var body = await (await hrClient.PostAsync($"/api/v1/users/{alice.Id}/deactivate", null)).AssertStatus(HttpStatusCode.OK);

        Assert.False(body.GetProperty("isActive").GetBoolean());
        await (await aliceClient.GetAsync("/api/v1/me")).AssertProblem(HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);
        await (await LoginAsync(alice.Email, Password)).AssertProblem(HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials);
        var request = await (await hrClient.GetAsync($"/api/v1/leave-requests/{id}")).AssertStatus(HttpStatusCode.OK);
        Assert.Equal("Cancelled", request.GetProperty("status").GetString());
        Assert.True(request.GetProperty("cancellation").GetProperty("isSystem").GetBoolean());
        Assert.Equal("Compte désactivé", request.GetProperty("cancellation").GetProperty("reason").GetString());
        await (await hrClient.PostAsync($"/api/v1/users/{alice.Id}/deactivate", null)).AssertProblem(HttpStatusCode.Conflict, ErrorCodes.UserAlreadyInactive);
    }

    [Fact]
    public async Task GAP_12_last_active_hr_admin_cannot_be_deactivated_or_demoted()
    {
        var (hr, hrClient) = await HrAsync();

        await (await hrClient.PostAsync($"/api/v1/users/{hr.Id}/deactivate", null)).AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.LastHrAdmin);
        await (await hrClient.PutAsJsonAsync($"/api/v1/users/{hr.Id}", new { email = hr.Email, firstName = "Helene", lastName = "Rh", roles = Array.Empty<string>(), managerId = (Guid?)null }))
            .AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.LastHrAdmin);
    }

    [Fact]
    public async Task Reset_password_forces_change_and_export_is_an_audited_attachment()
    {
        var (_, hrClient) = await HrAsync();
        var alice = await CreateUserAsync("Alice", "Martin");

        Assert.Equal(HttpStatusCode.NoContent, (await hrClient.PostAsJsonAsync($"/api/v1/users/{alice.Id}/reset-password", new { temporaryPassword = "Temporaire-2026-abc" })).StatusCode);
        var login = await (await LoginAsync(alice.Email, "Temporaire-2026-abc")).AssertStatus(HttpStatusCode.OK);
        Assert.True(login.GetProperty("mustChangePassword").GetBoolean());

        var export = await hrClient.GetAsync($"/api/v1/users/{alice.Id}/export");
        var body = await export.AssertStatus(HttpStatusCode.OK);
        Assert.StartsWith("attachment", export.Content.Headers.ContentDisposition?.ToString() ?? export.Headers.GetValues("Content-Disposition").First());
        Assert.Equal("alice.martin@pme.fr", body.GetProperty("profile").GetProperty("email").GetString());
        Assert.False(body.GetProperty("profile").TryGetProperty("passwordHash", out _));

        var audit = await (await hrClient.GetAsync($"/api/v1/audit-log?entityType=User&entityId={alice.Id}")).AssertStatus(HttpStatusCode.OK);
        var actions = audit.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("action").GetString()).ToList();
        Assert.Equal(["UserDataExported", "PasswordReset"], actions);
    }

    [Fact]
    public async Task Users_endpoints_are_forbidden_for_non_hr()
    {
        var m = await CreateUserAsync("Marc", "Durand", null, Role.Manager);
        var client = await ClientForAsync(m);

        await (await client.GetAsync("/api/v1/users")).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
        await (await client.GetAsync("/api/v1/audit-log")).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
        await (await client.GetAsync("/api/v1/notifications")).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
        await (await client.PostAsJsonAsync("/api/v1/holidays", new { date = "2026-05-01", label = "Fête du travail" })).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.Forbidden);
    }

    // ------------------------------------------------------------------ Jours fériés (US-18)

    [Fact]
    [Trait("Rule", "RB-06")]
    [Trait("Rule", "RB-07")]
    public async Task CA_18_1_to_18_3_holidays_affect_new_counts_only_and_are_unique()
    {
        var (hr, hrClient) = await HrAsync();
        await AllocateAsync(hr.Id, LeaveType.CpId, 10);
        var id = await SubmitOkAsync(hrClient, LeaveType.CpId, "2026-05-04", "2026-05-08");

        await (await hrClient.PostAsJsonAsync("/api/v1/holidays", new { date = "2026-05-01", label = "Fête du travail" })).AssertStatus(HttpStatusCode.Created);
        await (await hrClient.PostAsJsonAsync("/api/v1/holidays", new { date = "2026-05-08", label = "Victoire 1945" })).AssertStatus(HttpStatusCode.Created);

        var sim = await (await hrClient.PostAsJsonAsync("/api/v1/leave-requests/simulate", new { leaveTypeId = LeaveType.CpId, startDate = "2026-04-30", endDate = "2026-05-01" })).Json();
        Assert.Equal(1, sim.GetProperty("daysCount").GetInt32());
        await (await hrClient.PostAsJsonAsync("/api/v1/holidays", new { date = "2026-05-01", label = "Doublon" })).AssertProblem(HttpStatusCode.Conflict, ErrorCodes.HolidayAlreadyExists);
        Assert.Equal(5, await QueryDbAsync(db => db.LeaveRequests.Where(r => r.Id == id).Select(r => r.DaysCount).SingleAsync()));

        var anyone = await ClientForAsync(await CreateUserAsync("Alice", "Martin"));
        var list = await (await anyone.GetAsync("/api/v1/holidays?year=2026")).AssertStatus(HttpStatusCode.OK);
        Assert.Equal(["2026-05-01", "2026-05-08"], list.EnumerateArray().Select(h => h.GetProperty("date").GetString()));
        Assert.Equal(HttpStatusCode.NoContent, (await hrClient.DeleteAsync("/api/v1/holidays/2026-05-08")).StatusCode);
        await (await hrClient.DeleteAsync("/api/v1/holidays/2026-05-08")).AssertProblem(HttpStatusCode.NotFound, ErrorCodes.NotFound);
    }

    // ------------------------------------------------------------------ Vue RH (US-19)

    [Fact]
    public async Task CA_19_1_and_19_3_hr_lists_all_pending_requests_and_sees_admin_actions_in_journal()
    {
        var (hr, hrClient) = await HrAsync();
        var m1 = await CreateUserAsync("Marc", "Durand", null, Role.Manager);
        var m2 = await CreateUserAsync("Nina", "Autre", null, Role.Manager);
        var a = await CreateUserAsync("Alice", "Martin", m1.Id);
        var b = await CreateUserAsync("Bruno", "Petit", m2.Id);
        await SubmitOkAsync(await ClientForAsync(a), LeaveType.SickId, "2026-03-09", "2026-03-09");
        await SubmitOkAsync(await ClientForAsync(b), LeaveType.SickId, "2026-03-10", "2026-03-10");
        await SubmitOkAsync(hrClient, LeaveType.SickId, "2026-03-11", "2026-03-11");

        var all = await (await hrClient.GetAsync("/api/v1/leave-requests?scope=all&status=Pending")).AssertStatus(HttpStatusCode.OK);
        Assert.Equal(3, all.GetProperty("totalCount").GetInt32());
        var own = all.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("requester").GetProperty("id").GetGuid() == hr.Id);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, own.GetProperty("overlapAlert").ValueKind); // GAP-24
        var byManager = await (await hrClient.GetAsync($"/api/v1/leave-requests?scope=all&managerId={m2.Id}")).Json();
        Assert.Equal(1, byManager.GetProperty("totalCount").GetInt32());

        await (await hrClient.PostAsJsonAsync("/api/v1/users", new { email = "zoe@pme.fr", firstName = "Zoe", lastName = "Z", roles = new[] { "Employee" }, temporaryPassword = "Bienvenue-2026-xK7p" }))
            .AssertStatus(HttpStatusCode.Created);
        var journal = await (await hrClient.GetAsync("/api/v1/audit-log?entityType=User")).AssertStatus(HttpStatusCode.OK);
        var entry = journal.GetProperty("items")[0];
        Assert.Equal("UserCreated", entry.GetProperty("action").GetString());
        Assert.Equal(hr.Id, entry.GetProperty("author").GetProperty("id").GetGuid());
        Assert.Equal("zoe@pme.fr", entry.GetProperty("details").GetProperty("after").GetProperty("email").GetString());
    }
}
