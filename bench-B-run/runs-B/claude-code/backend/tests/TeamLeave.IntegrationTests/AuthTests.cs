using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Users;
using TeamLeave.IntegrationTests.Support;

namespace TeamLeave.IntegrationTests;

[Trait("Rule", "RB-01")]
[Trait("Rule", "RB-02")]
[Trait("Rule", "RB-27")]
public sealed class AuthTests : IntegrationTest
{
    [Fact]
    public async Task CA_01_1_login_returns_jwt_with_user_id_and_roles()
    {
        var manager = await CreateUserAsync("Marc", "Durand", null, Role.Manager);

        var body = await (await LoginAsync("MARC.durand@pme.fr", Password)).AssertStatus(HttpStatusCode.OK);

        Assert.Equal("Bearer", body.GetProperty("tokenType").GetString());
        Assert.False(body.GetProperty("mustChangePassword").GetBoolean());
        var payload = DecodePayload(body.GetProperty("accessToken").GetString()!);
        Assert.Equal(manager.Id.ToString(), payload.GetProperty("sub").GetString());
        var roles = payload.GetProperty("role").EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Equal(["Employee", "Manager"], roles.Order());
        Assert.Equal(TeamLeaveFactory.StartUtc.AddMinutes(60).UtcDateTime, body.GetProperty("expiresAtUtc").GetDateTime().ToUniversalTime());
    }

    [Fact]
    public async Task CA_01_2_wrong_password_or_unknown_email_give_the_same_401()
    {
        await CreateUserAsync("Alice", "Martin");

        var wrongPassword = await (await LoginAsync("alice.martin@pme.fr", "Wrong-Password-123")).AssertProblem(HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials);
        var unknown = await (await LoginAsync("nobody@pme.fr", "Wrong-Password-123")).AssertProblem(HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials);

        Assert.Equal(wrongPassword.GetProperty("detail").GetString(), unknown.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task CA_01_3_deactivated_user_cannot_login()
    {
        var hr = await CreateUserAsync("Helene", "Rh", null, Role.HrAdmin);
        await CreateUserAsync("Other", "Rh", null, Role.HrAdmin);
        var alice = await CreateUserAsync("Alice", "Martin");
        var hrClient = await ClientForAsync(hr);

        await (await hrClient.PostAsync($"/api/v1/users/{alice.Id}/deactivate", null)).AssertStatus(HttpStatusCode.OK);

        await (await LoginAsync(alice.Email, Password)).AssertProblem(HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials);
    }

    [Fact]
    public async Task CA_01_4_five_failures_lock_the_account_for_fifteen_minutes()
    {
        var alice = await CreateUserAsync("Alice", "Martin");
        for (var i = 0; i < 5; i++)
        {
            await (await LoginAsync(alice.Email, "Wrong-Password-123")).AssertProblem(HttpStatusCode.Unauthorized, ErrorCodes.InvalidCredentials);
        }

        var locked = await LoginAsync(alice.Email, Password);
        var body = await locked.AssertProblem(HttpStatusCode.TooManyRequests, ErrorCodes.AccountLocked);
        Assert.True(locked.Headers.Contains("Retry-After"));
        Assert.True(body.TryGetProperty("lockoutEndUtc", out _));

        Factory.Clock.Advance(TimeSpan.FromMinutes(14));
        await (await LoginAsync(alice.Email, Password)).AssertProblem(HttpStatusCode.TooManyRequests, ErrorCodes.AccountLocked);

        Factory.Clock.Advance(TimeSpan.FromMinutes(2));
        await (await LoginAsync(alice.Email, Password)).AssertStatus(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CA_01_5_expired_token_is_rejected()
    {
        var alice = await CreateUserAsync("Alice", "Martin");
        var client = await ClientForAsync(alice);
        await (await client.GetAsync("/api/v1/me")).AssertStatus(HttpStatusCode.OK);

        Factory.Clock.Advance(TimeSpan.FromMinutes(61));

        await (await client.GetAsync("/api/v1/me")).AssertProblem(HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);
    }

    [Fact]
    public async Task Missing_or_forged_token_is_401_unauthenticated()
    {
        await (await Anonymous.GetAsync("/api/v1/me")).AssertProblem(HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);

        var forged = Factory.CreateClient();
        forged.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJub25lIn0.eyJzdWIiOiJ4In0.");
        await (await forged.GetAsync("/api/v1/me")).AssertProblem(HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);
    }

    [Fact]
    public async Task GAP_07_role_change_revokes_current_tokens()
    {
        var hr = await CreateUserAsync("Helene", "Rh", null, Role.HrAdmin);
        var alice = await CreateUserAsync("Alice", "Martin");
        var aliceClient = await ClientForAsync(alice);
        var hrClient = await ClientForAsync(hr);

        await (await hrClient.PutAsJsonAsync($"/api/v1/users/{alice.Id}", new
        {
            email = alice.Email,
            firstName = "Alice",
            lastName = "Martin",
            roles = new[] { "Employee", "Manager" },
            managerId = (Guid?)null,
        })).AssertStatus(HttpStatusCode.OK);

        await (await aliceClient.GetAsync("/api/v1/me")).AssertProblem(HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);
    }

    [Fact]
    public async Task GAP_06_password_change_required_blocks_everything_but_me_and_password()
    {
        var hr = await CreateUserAsync("Helene", "Rh", null, Role.HrAdmin);
        var hrClient = await ClientForAsync(hr);
        var created = await (await hrClient.PostAsJsonAsync("/api/v1/users", new
        {
            email = "new.comer@pme.fr",
            firstName = "New",
            lastName = "Comer",
            roles = Array.Empty<string>(),
            temporaryPassword = "Bienvenue-2026-xK7p",
        })).AssertStatus(HttpStatusCode.Created);
        Assert.True(created.GetProperty("mustChangePassword").GetBoolean());

        var login = await (await LoginAsync("new.comer@pme.fr", "Bienvenue-2026-xK7p")).AssertStatus(HttpStatusCode.OK);
        Assert.True(login.GetProperty("mustChangePassword").GetBoolean());
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());

        await (await client.GetAsync("/api/v1/me")).AssertStatus(HttpStatusCode.OK);
        await (await client.GetAsync("/api/v1/me/balances")).AssertProblem(HttpStatusCode.Forbidden, ErrorCodes.PasswordChangeRequired);

        await (await client.PutAsJsonAsync("/api/v1/me/password", new { currentPassword = "wrong-password-xx", newPassword = "Mon-Nouveau-Secret-42" }))
            .AssertProblem(HttpStatusCode.UnprocessableEntity, ErrorCodes.InvalidCurrentPassword);
        await (await client.PutAsJsonAsync("/api/v1/me/password", new { currentPassword = "Bienvenue-2026-xK7p", newPassword = "court" }))
            .AssertProblem(HttpStatusCode.BadRequest, ErrorCodes.ValidationError);
        var changed = await client.PutAsJsonAsync("/api/v1/me/password", new { currentPassword = "Bienvenue-2026-xK7p", newPassword = "Mon-Nouveau-Secret-42" });
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);

        // Le tampon est régénéré : reconnexion obligatoire.
        await (await client.GetAsync("/api/v1/me")).AssertProblem(HttpStatusCode.Unauthorized, ErrorCodes.Unauthenticated);
        var relogin = await (await LoginAsync("new.comer@pme.fr", "Mon-Nouveau-Secret-42")).AssertStatus(HttpStatusCode.OK);
        Assert.False(relogin.GetProperty("mustChangePassword").GetBoolean());
    }

    [Fact]
    public async Task Me_returns_profile_with_manager()
    {
        var manager = await CreateUserAsync("Marc", "Durand", null, Role.Manager);
        var alice = await CreateUserAsync("Alice", "Martin", manager.Id);
        var client = await ClientForAsync(alice);

        var me = await (await client.GetAsync("/api/v1/me")).AssertStatus(HttpStatusCode.OK);

        Assert.Equal(alice.Id, me.GetProperty("id").GetGuid());
        Assert.Equal("Marc Durand", me.GetProperty("manager").GetProperty("fullName").GetString());
        Assert.Equal(["Employee"], me.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    public async Task Login_body_validation_is_400_problem()
    {
        var response = await Anonymous.PostAsJsonAsync("/api/v1/auth/login", new { email = "" });

        var body = await response.AssertProblem(HttpStatusCode.BadRequest, ErrorCodes.ValidationError);
        Assert.True(body.GetProperty("errors").TryGetProperty("email", out _));
    }

    [Fact]
    public async Task Health_endpoints_are_anonymous()
    {
        Assert.Equal(HttpStatusCode.OK, (await Anonymous.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Anonymous.GetAsync("/health/ready")).StatusCode);
    }

    private static JsonElement DecodePayload(string jwt)
    {
        var part = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
        part = part.PadRight(part.Length + ((4 - (part.Length % 4)) % 4), '=');
        return JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(part))).RootElement.Clone();
    }
}
