using TeamLeave.Api.Infrastructure;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;

namespace TeamLeave.Api.Endpoints;

public static class LeaveTypeEndpoints
{
    public static IEndpointRouteBuilder MapLeaveTypeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/leave-types", async (
            int? page,
            int? pageSize,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await admin.ListLeaveTypesAsync(
                includeInactive: false,
                page ?? 1,
                pageSize ?? Pagination.DefaultPageSize,
                cancellationToken);

            return ApiProblem.Result(result, context);
        }).RequireAuthorization();

        return app;
    }
}
