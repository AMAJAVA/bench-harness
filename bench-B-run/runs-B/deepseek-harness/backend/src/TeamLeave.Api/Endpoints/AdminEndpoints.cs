using TeamLeave.Api.Infrastructure;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;

namespace TeamLeave.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin").RequireAuthorization("HrAdminOnly");

        group.MapGet("/leave-types", async (
            bool includeInactive,
            int? page,
            int? pageSize,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await admin.ListLeaveTypesAsync(
                includeInactive,
                page ?? 1,
                pageSize ?? Pagination.DefaultPageSize,
                cancellationToken);

            return ApiProblem.Result(result, context);
        });

        group.MapPost("/leave-types", async (
            CreateLeaveTypeRequest request,
            ICurrentUser currentUser,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } actorId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await admin.CreateLeaveTypeAsync(actorId, request, cancellationToken);
            return ApiProblem.Result(
                result,
                context,
                value => Results.Created($"/api/v1/admin/leave-types/{value.Id}", value));
        });

        group.MapPatch("/leave-types/{id:guid}", async (
            Guid id,
            UpdateLeaveTypeRequest request,
            ICurrentUser currentUser,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } actorId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await admin.UpdateLeaveTypeAsync(actorId, id, request, cancellationToken);
            return ApiProblem.Result(result, context);
        });

        group.MapGet("/users", async (
            Guid? teamId,
            bool? active,
            int? page,
            int? pageSize,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await admin.ListUsersAsync(
                teamId,
                active,
                page ?? 1,
                pageSize ?? Pagination.DefaultPageSize,
                cancellationToken);

            return ApiProblem.Result(result, context);
        });

        group.MapPost("/users", async (
            CreateUserRequest request,
            ICurrentUser currentUser,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } actorId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await admin.CreateUserAsync(actorId, request, cancellationToken);
            return ApiProblem.Result(
                result,
                context,
                value => Results.Created($"/api/v1/admin/users/{value.Id}", value));
        });

        group.MapPatch("/users/{id:guid}", async (
            Guid id,
            UpdateUserRequest request,
            ICurrentUser currentUser,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } actorId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await admin.UpdateUserAsync(actorId, id, request, cancellationToken);
            return ApiProblem.Result(result, context);
        });

        group.MapPost("/users/{id:guid}/deactivate", async (
            Guid id,
            ICurrentUser currentUser,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } actorId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await admin.DeactivateUserAsync(actorId, id, cancellationToken);
            return ApiProblem.Result(result, context);
        });

        group.MapPost("/users/{id:guid}/reset-password", async (
            Guid id,
            ICurrentUser currentUser,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } actorId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await admin.ResetPasswordAsync(actorId, id, cancellationToken);
            return ApiProblem.Result(result, context);
        });

        group.MapGet("/teams", async (
            bool? active,
            int? page,
            int? pageSize,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await admin.ListTeamsAsync(
                active,
                page ?? 1,
                pageSize ?? Pagination.DefaultPageSize,
                cancellationToken);

            return ApiProblem.Result(result, context);
        });

        group.MapPost("/teams", async (
            CreateTeamRequest request,
            ICurrentUser currentUser,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } actorId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await admin.CreateTeamAsync(actorId, request, cancellationToken);
            return ApiProblem.Result(
                result,
                context,
                value => Results.Created($"/api/v1/admin/teams/{value.Id}", value));
        });

        group.MapPatch("/teams/{id:guid}", async (
            Guid id,
            UpdateTeamRequest request,
            ICurrentUser currentUser,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } actorId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await admin.UpdateTeamAsync(actorId, id, request, cancellationToken);
            return ApiProblem.Result(result, context);
        });

        group.MapGet("/users/{userId:guid}/balances", async (
            Guid userId,
            int? year,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await admin.GetUserBalancesAsync(userId, year, cancellationToken);
            return ApiProblem.Result(result, context);
        });

        group.MapGet("/users/{userId:guid}/balance-adjustments", async (
            Guid userId,
            int? year,
            int? page,
            int? pageSize,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await admin.ListBalanceAdjustmentsAsync(
                userId,
                year,
                page ?? 1,
                pageSize ?? Pagination.DefaultPageSize,
                cancellationToken);

            return ApiProblem.Result(result, context);
        });

        group.MapPost("/users/{userId:guid}/balance-adjustments", async (
            Guid userId,
            AdjustBalanceRequest request,
            ICurrentUser currentUser,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not { } actorId)
            {
                return ApiProblem.FromError(ApiProblem.Unauthenticated(), context);
            }

            var result = await admin.AdjustBalanceAsync(actorId, userId, request, cancellationToken);
            return ApiProblem.Result(
                result,
                context,
                value => Results.Created($"/api/v1/admin/users/{userId}/balance-adjustments/{value.MovementId}", value));
        });

        group.MapGet("/leave-requests", async (
            string? status,
            Guid? teamId,
            Guid? employeeId,
            DateOnly? from,
            DateOnly? to,
            int? page,
            int? pageSize,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await admin.ListLeaveRequestsAsync(
                status,
                teamId,
                employeeId,
                from,
                to,
                page ?? 1,
                pageSize ?? Pagination.DefaultPageSize,
                cancellationToken);

            return ApiProblem.Result(result, context);
        });

        group.MapGet("/leave-requests/{id:guid}", async (
            Guid id,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await admin.GetLeaveRequestAsync(id, cancellationToken);
            return ApiProblem.Result(result, context);
        });

        group.MapGet("/notifications", async (
            string? status,
            int? page,
            int? pageSize,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await admin.ListNotificationsAsync(
                status,
                page ?? 1,
                pageSize ?? Pagination.DefaultPageSize,
                cancellationToken);

            return ApiProblem.Result(result, context);
        });

        group.MapGet("/audit-entries", async (
            DateOnly? from,
            DateOnly? to,
            string? entityType,
            int? page,
            int? pageSize,
            IAdminService admin,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await admin.ListAuditEntriesAsync(
                from,
                to,
                entityType,
                page ?? 1,
                pageSize ?? Pagination.DefaultPageSize,
                cancellationToken);

            return ApiProblem.Result(result, context);
        });

        return app;
    }
}
