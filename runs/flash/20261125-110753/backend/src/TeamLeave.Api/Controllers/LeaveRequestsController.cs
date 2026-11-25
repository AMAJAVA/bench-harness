using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Services;

namespace TeamLeave.Api.Controllers;

[ApiController]
[Route("api/v1/leave-requests")]
[Authorize]
public class LeaveRequestsController : ApiControllerBase
{
    private readonly LeaveRequestService _leaveRequestService;

    public LeaveRequestsController(LeaveRequestService leaveRequestService) => _leaveRequestService = leaveRequestService;

    [HttpPost]
    public async Task<ActionResult<LeaveRequestDto>> Submit([FromBody] CreateLeaveRequestRequest request, CancellationToken cancellationToken)
    {
        var result = await _leaveRequestService.SubmitAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeaveRequestDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _leaveRequestService.GetByIdAsync(id, CurrentUserId, CurrentRoles, cancellationToken));

    [HttpPost("{id:guid}/decision")]
    public async Task<ActionResult<LeaveRequestDto>> Decide(Guid id, [FromBody] DecisionRequest request, CancellationToken cancellationToken)
        => Ok(await _leaveRequestService.DecideAsync(id, CurrentUserId, CurrentRoles, request, cancellationToken));
}
