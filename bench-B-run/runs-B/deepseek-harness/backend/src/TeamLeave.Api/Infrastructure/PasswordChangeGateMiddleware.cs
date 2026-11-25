namespace TeamLeave.Api.Infrastructure;

public sealed class PasswordChangeGateMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> AllowedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/me",
        "/api/v1/me/password",
        "/api/v1/auth/logout",
        "/api/v1/auth/refresh",
        "/api/v1/auth/login"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var mustChange = string.Equals(
            context.User.FindFirst("mcp")?.Value,
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (mustChange &&
            context.User.Identity?.IsAuthenticated == true &&
            !AllowedPaths.Contains(context.Request.Path.Value ?? string.Empty) &&
            !context.Request.Path.StartsWithSegments("/health") &&
            !context.Request.Path.StartsWithSegments("/openapi"))
        {
            await ApiProblem.WriteAsync(context, ApiProblem.PasswordChangeRequired());
            return;
        }

        await next(context);
    }
}
