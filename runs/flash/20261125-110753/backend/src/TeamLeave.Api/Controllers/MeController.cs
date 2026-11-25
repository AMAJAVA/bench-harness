using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Services;
using TeamLeave.Domain.Enums;

namespace TeamLeave.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class MeController : ApiControllerBase
{
    private readonly AuthService _authService;
    private readonly BalanceService _balanceService;
    private readonly LeaveRequestService _leaveRequestService;

    public MeController(AuthService authService, BalanceService balanceService, LeaveRequestService leaveRequestService)
    {
        _authService = authService;
        _balanceService = balanceService;
        _leaveRequestService = leaveRequestService;
    }

    [HttpGet]
    public async Task<ActionResult<MeDto>> GetMe(CancellationToken cancellationToken)
        => Ok(await _authService.GetMeAsync(CurrentUserId, cancellationToken));

    [HttpGet("balances")]
    public async Task<ActionResult<IReadOnlyList<BalanceDto>>> GetBalances([FromQuery] int year, CancellationToken cancellationToken)
        => Ok(await _balanceService.GetBalancesAsync(CurrentUserId, year, cancellationToken));

    [HttpGet("leave-requests")]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestDto>>> GetMyLeaveRequests(
        [FromQuery] string? status,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        LeaveRequestStatus? statusEnum = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LeaveRequestStatus>(status, true, out var parsed))
            statusEnum = parsed;

        return Ok(await _leaveRequestService.GetMyRequestsAsync(CurrentUserId, statusEnum, from, to, cancellationToken));
    }
}
