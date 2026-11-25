using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Services;

namespace TeamLeave.Api.Controllers;

[ApiController]
[Route("api/v1/teams/{teamId:guid}")]
[Authorize]
public class TeamsController : ApiControllerBase
{
    private readonly LeaveRequestService _leaveRequestService;

    public TeamsController(LeaveRequestService leaveRequestService) => _leaveRequestService = leaveRequestService;

    [HttpGet("leave-requests")]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestDto>>> GetTeamLeaveRequests(
        Guid teamId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
        => Ok(await _leaveRequestService.GetTeamPendingRequestsAsync(teamId, CurrentUserId, cancellationToken));

    [HttpGet("calendar")]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestDto>>> GetCalendar(
        Guid teamId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        if (from > to) return BadRequest("from must be before to");
        return Ok(await _leaveRequestService.GetTeamCalendarAsync(teamId, CurrentUserId, CurrentRoles, from, to, cancellationToken));
    }
}
