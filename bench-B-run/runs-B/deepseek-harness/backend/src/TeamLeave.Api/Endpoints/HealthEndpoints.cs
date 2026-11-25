using TeamLeave.Api.Infrastructure;
using TeamLeave.Application.Common;
using TeamLeave.Infrastructure.Persistence;

namespace TeamLeave.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }))
            .AllowAnonymous();

        app.MapGet("/health/ready", async (
            TeamLeaveDbContext db,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var canConnect = await db.Database.CanConnectAsync(cancellationToken);
                if (canConnect)
                {
                    return Results.Ok(new { status = "Ready" });
                }
            }
            catch
            {
                // La réponse 503 ci-dessous ne doit pas exposer de détail technique.
            }

            return ApiProblem.FromError(
                AppError.Create(
                    ErrorCodes.SERVICE_UNAVAILABLE,
                    "Service temporairement indisponible",
                    503,
                    "La base de données n'est pas joignable."),
                context);
        }).AllowAnonymous();

        return app;
    }
}
