namespace TeamLeave.Api.Infrastructure;

public sealed class OriginGuardMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly HashSet<string> _allowedOrigins = (configuration
            .GetSection("Spa:AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:5173"])
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async Task InvokeAsync(HttpContext context)
    {
        var isCookieEndpoint =
            context.Request.Path.Equals("/api/v1/auth/refresh", StringComparison.OrdinalIgnoreCase) ||
            context.Request.Path.Equals("/api/v1/auth/logout", StringComparison.OrdinalIgnoreCase);

        if (isCookieEndpoint && context.Request.Headers.TryGetValue("Origin", out var origin))
        {
            if (!_allowedOrigins.Contains(origin.ToString()))
            {
                await ApiProblem.WriteAsync(context, ApiProblem.Forbidden("Origine non autorisée."));
                return;
            }
        }

        await next(context);
    }
}
