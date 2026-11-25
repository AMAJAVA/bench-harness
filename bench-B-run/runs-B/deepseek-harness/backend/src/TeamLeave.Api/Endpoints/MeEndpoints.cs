using TeamLeave.Api.Infrastructure;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;

namespace TeamLeave.Api.Endpoints;

public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/me").RequireAuthorization();

        group.MapGet("", async (
            ICurrentUser currentUser,
            IAuthService auth,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } userId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await auth.GetMeAsync(userId, cancellationToken);
            return ApiProblem.Result(result, context);
        });

        group.MapPut("/password", async (
            ChangePasswordRequest request,
            ICurrentUser currentUser,
            IAuthService auth,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } userId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await auth.ChangePasswordAsync(userId, request, cancellationToken);
            return result.IsSuccess ? Results.NoContent() : ApiProblem.Result(result, context);
        });

        group.MapGet("/balances", async (
            int? year,
            ICurrentUser currentUser,
            ILeaveRequestService leaveRequests,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } userId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await leaveRequests.GetMyBalancesAsync(userId, year, cancellationToken);
            return ApiProblem.Result(result, context);
        });

        group.MapGet("/leave-requests", async (
            string? status,
            int? year,
            int? page,
            int? pageSize,
            ICurrentUser currentUser,
            ILeaveRequestService leaveRequests,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } userId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await leaveRequests.ListMineAsync(
                userId,
                status,
                year,
                page ?? 1,
                pageSize ?? Pagination.DefaultPageSize,
                cancellationToken);

            return ApiProblem.Result(result, context);
        });

        group.MapPost("/leave-requests", async (
            SubmitLeaveRequestRequest request,
            ICurrentUser currentUser,
            ILeaveRequestService leaveRequests,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } userId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await leaveRequests.SubmitAsync(userId, request, cancellationToken);
            return ApiProblem.Result(
                result,
                context,
                value => Results.Created($"/api/v1/me/leave-requests/{value.Id}", value));
        });

        group.MapGet("/leave-requests/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            ILeaveRequestService leaveRequests,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } userId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await leaveRequests.GetMineAsync(userId, id, cancellationToken);
            return ApiProblem.Result(result, context);
        });

        group.MapPost("/leave-requests/{id:guid}/cancel", async (
            Guid id,
            ICurrentUser currentUser,
            ILeaveRequestService leaveRequests,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } userId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await leaveRequests.CancelAsync(userId, id, cancellationToken);
            return ApiProblem.Result(result, context);
        });

        return app;
    }
}
