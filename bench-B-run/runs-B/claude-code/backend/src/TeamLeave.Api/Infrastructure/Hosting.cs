using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TeamLeave.Application.Abstractions;
using TeamLeave.Domain.Common;

namespace TeamLeave.Api.Infrastructure;

/// <summary>Limitation de débit de la connexion par adresse IP (ENF-S5, GAP-21).</summary>
public static class RateLimiting
{
    public const string LoginPolicy = "login";

    public static IServiceCollection AddLoginRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(LoginPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? IPAddress.None.ToString(),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = context.RequestServices.GetRequiredService<IConfiguration>().GetValue("RateLimiting:LoginPermitLimit", 10),
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
            options.OnRejected = async (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await Problems.WriteAsync(context.HttpContext, ErrorCodes.RateLimited, "Trop de tentatives de connexion depuis cette adresse. Réessayez dans une minute.");
            };
        });
        return services;
    }
}

/// <summary>/health/ready : base joignable (ENF-D4), sans détail interne.</summary>
public sealed class DatabaseHealthCheck(ITeamLeaveDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ((Microsoft.EntityFrameworkCore.DbContext)db).Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}

/// <summary>En-têtes de sécurité (step2_arch §7.3, §7.4).</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers.ContentSecurityPolicy =
            "default-src 'self'; script-src 'self'; object-src 'none'; frame-ancestors 'none'; base-uri 'self'";
        return next(context);
    }
}

/// <summary>Réponse 400 VALIDATION_ERROR des erreurs de liaison et de DataAnnotations.</summary>
public static class ValidationResponses
{
    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .Where(e => e.Value is { Errors.Count: > 0 })
            .ToDictionary(
                e => NormalizeKey(e.Key),
                e => e.Value!.Errors.Select(x => string.IsNullOrEmpty(x.ErrorMessage) ? "Valeur invalide." : x.ErrorMessage).ToArray());
        var problem = Problems.Create(context.HttpContext, ErrorCodes.ValidationError, "Un ou plusieurs champs sont invalides.", null, errors);
        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" },
        };
    }

    private static string NormalizeKey(string key)
    {
        var trimmed = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;
        return string.IsNullOrEmpty(trimmed) ? "body" : char.ToLowerInvariant(trimmed[0]) + trimmed[1..];
    }
}
