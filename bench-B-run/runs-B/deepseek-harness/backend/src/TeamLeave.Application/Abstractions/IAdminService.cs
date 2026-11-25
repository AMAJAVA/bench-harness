using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;

namespace TeamLeave.Application.Abstractions;

public interface IAdminService
{
    Task<Result<PagedResult<LeaveTypeDto>>> ListLeaveTypesAsync(
        bool includeInactive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<LeaveTypeDto>> CreateLeaveTypeAsync(
        Guid actorId,
        CreateLeaveTypeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<LeaveTypeDto>> UpdateLeaveTypeAsync(
        Guid actorId,
        Guid leaveTypeId,
        UpdateLeaveTypeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<UserAdminDto>>> ListUsersAsync(
        Guid? teamId,
        bool? active,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<UserAdminDto>> CreateUserAsync(
        Guid actorId,
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<UserAdminDto>> UpdateUserAsync(
        Guid actorId,
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<DeactivateUserResponse>> DeactivateUserAsync(
        Guid actorId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<ResetPasswordResponse>> ResetPasswordAsync(
        Guid actorId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<TeamDto>>> ListTeamsAsync(
        bool? active,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<TeamDto>> CreateTeamAsync(
        Guid actorId,
        CreateTeamRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<TeamDto>> UpdateTeamAsync(
        Guid actorId,
        Guid teamId,
        UpdateTeamRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<BalancesResponse>> GetUserBalancesAsync(
        Guid userId,
        int? year,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<BalanceMovementDto>>> ListBalanceAdjustmentsAsync(
        Guid userId,
        int? year,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<AdjustBalanceResponse>> AdjustBalanceAsync(
        Guid actorId,
        Guid userId,
        AdjustBalanceRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<LeaveRequestListItem>>> ListLeaveRequestsAsync(
        string? status,
        Guid? teamId,
        Guid? employeeId,
        DateOnly? from,
        DateOnly? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<LeaveRequestDetail>> GetLeaveRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<NotificationDto>>> ListNotificationsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<AuditEntryDto>>> ListAuditEntriesAsync(
        DateOnly? from,
        DateOnly? to,
        string? entityType,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
