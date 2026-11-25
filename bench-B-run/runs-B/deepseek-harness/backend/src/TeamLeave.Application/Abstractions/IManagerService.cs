using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;

namespace TeamLeave.Application.Abstractions;

public interface IManagerService
{
    Task<Result<PagedResult<LeaveRequestListItem>>> ListQueueAsync(
        Guid managerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<LeaveRequestDetail>> GetForDecisionAsync(
        Guid managerId,
        Guid requestId,
        IReadOnlyList<string> callerRoles,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<ReportDto>>> ListReportsAsync(
        Guid managerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<BalancesResponse>> GetReportBalancesAsync(
        Guid managerId,
        Guid employeeId,
        int? year,
        CancellationToken cancellationToken = default);

    Task<Result<LeaveRequestDetail>> ApproveAsync(
        Guid managerId,
        Guid requestId,
        ApproveLeaveRequestRequest request,
        IReadOnlyList<string> callerRoles,
        CancellationToken cancellationToken = default);

    Task<Result<LeaveRequestDetail>> RejectAsync(
        Guid managerId,
        Guid requestId,
        RejectLeaveRequestRequest request,
        IReadOnlyList<string> callerRoles,
        CancellationToken cancellationToken = default);
}
