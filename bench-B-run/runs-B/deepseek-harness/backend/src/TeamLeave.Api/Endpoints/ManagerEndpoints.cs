using TeamLeave.Api.Infrastructure;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;

namespace TeamLeave.Api.Endpoints;

public static class ManagerEndpoints
{
    public static IEndpointRouteBuilder MapManagerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/manager").RequireAuthorization("ManagerOnly");

        group.MapGet("/leave-requests", async (
            string? status,
            int? page,
            int? pageSize,
            ICurrentUser currentUser,
            IManagerService managers,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } managerId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await managers.ListQueueAsync(
                managerId,
                status,
                page ?? 1,
                pageSize ?? Pagination.DefaultPageSize,
                cancellationToken);

            return ApiProblem.Result(result, context);
        });

        group.MapGet("/leave-requests/{id:guid}", async (
            Guid id,
            ICurrentUser currentUser,
            IManagerService managers,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } managerId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await managers.GetForDecisionAsync(managerId, id, currentUser.Roles, cancellationToken);
            return ApiProblem.Result(result, context);
        });

        group.MapGet("/reports", async (
            int? page,
            int? pageSize,
            ICurrentUser currentUser,
            IManagerService managers,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } managerId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await managers.ListReportsAsync(
                managerId,
                page ?? 1,
                pageSize ?? Pagination.DefaultPageSize,
                cancellationToken);

            return ApiProblem.Result(result, context);
        });

        group.MapGet("/reports/{employeeId:guid}/balances", async (
            Guid employeeId,
            int? year,
            ICurrentUser currentUser,
            IManagerService managers,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } managerId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await managers.GetReportBalancesAsync(managerId, employeeId, year, cancellationToken);
            return ApiProblem.Result(result, context);
        });

        group.MapPost("/leave-requests/{id:guid}/approve", async (
            Guid id,
            ApproveLeaveRequestRequest request,
            ICurrentUser currentUser,
            IManagerService managers,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } managerId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await managers.ApproveAsync(managerId, id, request, currentUser.Roles, cancellationToken);
            return ApiProblem.Result(result, context);
        });

        group.MapPost("/leave-requests/{id:guid}/reject", async (
            Guid id,
            RejectLeaveRequestRequest request,
            ICurrentUser currentUser,
            IManagerService managers,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } managerId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await managers.RejectAsync(managerId, id, request, currentUser.Roles, cancellationToken);
            return ApiProblem.Result(result, context);
        });

        return app;
    }
}
