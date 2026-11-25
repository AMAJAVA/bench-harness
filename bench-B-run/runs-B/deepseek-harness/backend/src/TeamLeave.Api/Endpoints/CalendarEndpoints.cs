using TeamLeave.Api.Infrastructure;
using TeamLeave.Application.Abstractions;

namespace TeamLeave.Api.Endpoints;

public static class CalendarEndpoints
{
    public static IEndpointRouteBuilder MapCalendarEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/teams/{teamId:guid}/calendar", async (
            Guid teamId,
            DateOnly? from,
            DateOnly? to,
            ICurrentUser currentUser,
            ICalendarService calendars,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } callerId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await calendars.GetTeamCalendarAsync(
                callerId,
                currentUser.Roles,
                teamId,
                from,
                to,
                cancellationToken);

            return ApiProblem.Result(result, context);
        }).RequireAuthorization();

        return app;
    }
}
