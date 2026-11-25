using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Services;
using TeamLeave.Domain.Enums;

namespace TeamLeave.Api.Controllers;

[ApiController]
[Route("api/v1/leave-types")]
[Authorize]
public class LeaveTypesController : ApiControllerBase
{
    private readonly LeaveTypeService _leaveTypeService;

    public LeaveTypesController(LeaveTypeService leaveTypeService) => _leaveTypeService = leaveTypeService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LeaveTypeDto>>> GetAll([FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        var isAdmin = CurrentRoles.Contains(RoleNames.AdminRh);
        return Ok(await _leaveTypeService.GetAllAsync(includeInactive && isAdmin, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.AdminRh)]
    public async Task<ActionResult<LeaveTypeDto>> Create([FromBody] CreateLeaveTypeRequest request, CancellationToken cancellationToken)
    {
        var result = await _leaveTypeService.CreateAsync(request, CurrentUserId, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { includeInactive = false }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.AdminRh)]
    public async Task<ActionResult<LeaveTypeDto>> Update(Guid id, [FromBody] UpdateLeaveTypeRequest request, CancellationToken cancellationToken)
        => Ok(await _leaveTypeService.UpdateAsync(id, request, CurrentUserId, cancellationToken));
}
