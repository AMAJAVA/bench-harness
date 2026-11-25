using System.Text.Json;
using TeamLeave.Api.Infrastructure;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;

namespace TeamLeave.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/login", async (
                LoginRequest request,
                IAuthService auth,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var result = await auth.LoginAsync(
                    request,
                    context.Connection.RemoteIpAddress?.ToString(),
                    context.Request.Headers.UserAgent.ToString(),
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    return ApiProblem.Result(result, context);
                }

                RefreshCookie.Set(context.Response, result.Value!.RefreshToken, result.Value.RefreshTokenExpiresAt);
                return Results.Ok(result.Value);
            })
            .AllowAnonymous()
            .RequireRateLimiting("login");

        group.MapPost("/refresh", async (
                HttpRequest request,
                IAuthService auth,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                string? bodyToken = null;
                if (request.ContentLength is > 0)
                {
                    try
                    {
                        var body = await request.ReadFromJsonAsync<RefreshRequest>(cancellationToken);
                        bodyToken = body?.RefreshToken;
                    }
                    catch (JsonException)
                    {
                        return ApiProblem.FromError(
                            ErrorFactory.Validation("Le corps de la requête est invalide."),
                            context);
                    }
                }

                var token = RefreshCookie.Read(request) ?? bodyToken;
                var result = await auth.RefreshAsync(
                    token,
                    context.Connection.RemoteIpAddress?.ToString(),
                    context.Request.Headers.UserAgent.ToString(),
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    RefreshCookie.Clear(context.Response);
                    return ApiProblem.Result(result, context);
                }

                RefreshCookie.Set(context.Response, result.Value!.RefreshToken, result.Value.RefreshTokenExpiresAt);
                return Results.Ok(result.Value);
            })
            .AllowAnonymous()
            .RequireRateLimiting("refresh");

        group.MapPost("/logout", async (
                HttpRequest request,
                IAuthService auth,
                HttpContext context,
                CancellationToken cancellationToken) =>
            {
                var token = RefreshCookie.Read(request);
                await auth.LogoutAsync(token, cancellationToken);
                RefreshCookie.Clear(context.Response);
                return Results.NoContent();
            })
            .RequireAuthorization();

        return app;
    }
}
