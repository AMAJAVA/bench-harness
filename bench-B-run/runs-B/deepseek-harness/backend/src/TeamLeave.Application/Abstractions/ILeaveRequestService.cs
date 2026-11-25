using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;

namespace TeamLeave.Application.Abstractions;

public interface ILeaveRequestService
{
    Task<Result<LeaveRequestDetail>> SubmitAsync(
        Guid employeeId,
        SubmitLeaveRequestRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<LeaveRequestListItem>>> ListMineAsync(
        Guid employeeId,
        string? status,
        int? year,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<LeaveRequestDetail>> GetMineAsync(
        Guid employeeId,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<Result<LeaveRequestDetail>> CancelAsync(
        Guid employeeId,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<Result<BalancesResponse>> GetMyBalancesAsync(
        Guid employeeId,
        int? year,
        CancellationToken cancellationToken = default);
}
