using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Notifications;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Overlap;
using TeamLeave.Domain.Users;
using TeamLeave.Infrastructure.Persistence;

namespace TeamLeave.IntegrationTests.Support;

/// <summary>
/// Base des tests HTTP : une application et une base neuves par test (isolation complète).
/// « Aujourd'hui » vaut le lundi 2026-03-02 (conventions des CA, spec §4).
/// </summary>
public abstract class IntegrationTest : IDisposable
{
    public const string Password = "Correct-Horse-Battery-2026";

    protected static readonly DateOnly Today = new(2026, 3, 2);

    protected IntegrationTest()
    {
        Factory = new TeamLeaveFactory();
        Anonymous = Factory.CreateClient();
    }

    protected TeamLeaveFactory Factory { get; }

    protected HttpClient Anonymous { get; }

    public void Dispose()
    {
        Anonymous.Dispose();
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }

    // ------------------------------------------------------------------ Données

    protected async Task<User> CreateUserAsync(string firstName, string lastName, Guid? managerId = null, params Role[] roles)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TeamLeaveDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var user = User.Create(
            Guid.CreateVersion7(),
            $"{firstName}.{lastName}@pme.fr".ToLowerInvariant(),
            firstName,
            lastName,
            managerId,
            roles,
            hasher.Hash(Password),
            mustChangePassword: false,
            Factory.Clock.GetUtcNow().UtcDateTime);
        db.Users.Add(user);
        foreach (var typeId in await db.LeaveTypes.Where(t => t.IsTracked).Select(t => t.Id).ToListAsync())
        {
            db.Balances.Add(Balance.CreateEmpty(Guid.CreateVersion7(), user.Id, typeId));
        }

        await db.SaveChangesAsync();
        return user;
    }

    protected async Task AllocateAsync(Guid userId, Guid leaveTypeId, int days)
    {
        await WithDbAsync(async db =>
        {
            var balance = await db.Balances.FirstAsync(b => b.UserId == userId && b.LeaveTypeId == leaveTypeId);
            db.BalanceMovements.Add(balance.ApplyHrMovement(MovementKind.Allocation, DayAmount.FromDays(days), "Reprise", 0, userId, Now));
        });
    }

    /// <summary>Insère directement une absence approuvée (colleagues de l'équipe).</summary>
    protected async Task<Guid> CreateApprovedAsync(Guid requesterId, Guid deciderId, DateOnly start, DateOnly end, Guid? leaveTypeId = null)
    {
        var id = Guid.CreateVersion7();
        await WithDbAsync(db =>
        {
            var days = Enumerable.Range(0, end.DayNumber - start.DayNumber + 1)
                .Select(start.AddDays)
                .Count(d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday));
            var request = LeaveRequest.Submit(id, requesterId, leaveTypeId ?? LeaveType.SickId, new DateRange(start, end), days, null, OverlapAnalysis.NotApplicable, Now);
            request.Approve(deciderId, null, OverlapAnalysis.NotApplicable, false, Now);
            db.LeaveRequests.Add(request);
            return Task.CompletedTask;
        });
        return id;
    }

    protected async Task WithDbAsync(Func<TeamLeaveDbContext, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TeamLeaveDbContext>();
        await action(db);
        await db.SaveChangesAsync();
    }

    protected async Task<T> QueryDbAsync<T>(Func<TeamLeaveDbContext, Task<T>> query)
    {
        using var scope = Factory.Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<TeamLeaveDbContext>());
    }

    protected async Task<int> DispatchOutboxAsync()
    {
        using var scope = Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().DispatchDueAsync(CancellationToken.None);
    }

    protected DateTime Now => Factory.Clock.GetUtcNow().UtcDateTime;

    // ------------------------------------------------------------------ HTTP

    protected Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        Anonymous.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

    protected async Task<HttpClient> ClientForAsync(User user)
    {
        var response = await LoginAsync(user.Email, Password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = (await response.Json()).GetProperty("accessToken").GetString();
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected static Task<HttpResponseMessage> SubmitAsync(HttpClient client, Guid typeId, string start, string end, string? comment = null) =>
        client.PostAsJsonAsync("/api/v1/leave-requests", new { leaveTypeId = typeId, startDate = start, endDate = end, comment });

    protected static async Task<Guid> SubmitOkAsync(HttpClient client, Guid typeId, string start, string end)
    {
        var response = await SubmitAsync(client, typeId, start, end);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Json()).GetProperty("id").GetGuid();
    }
}

public static class HttpAssertions
{
    public static async Task<JsonElement> Json(this HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        return string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
    }

    /// <summary>Vérifie le statut HTTP et le code métier Problem Details (step2_arch §5.1).</summary>
    public static async Task<JsonElement> AssertProblem(this HttpResponseMessage response, HttpStatusCode status, string code)
    {
        var body = await response.Json();
        Assert.True(status == response.StatusCode, $"Attendu {(int)status}, reçu {(int)response.StatusCode} : {body}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
        return body;
    }

    public static async Task<JsonElement> AssertStatus(this HttpResponseMessage response, HttpStatusCode status)
    {
        var body = await response.Json();
        Assert.True(status == response.StatusCode, $"Attendu {(int)status}, reçu {(int)response.StatusCode} : {body}");
        return body;
    }
}
