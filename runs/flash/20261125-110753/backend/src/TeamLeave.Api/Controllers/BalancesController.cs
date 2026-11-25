using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Services;
using TeamLeave.Domain.Enums;

namespace TeamLeave.Api.Controllers;

[ApiController]
[Route("api/v1/users/{userId:guid}/balances")]
[Authorize(Roles = RoleNames.AdminRh)]
public class BalancesController : ApiControllerBase
{
    private readonly BalanceService _balanceService;

    public BalancesController(BalanceService balanceService) => _balanceService = balanceService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BalanceDto>>> GetUserBalances(Guid userId, [FromQuery] int year, CancellationToken cancellationToken)
        => Ok(await _balanceService.GetBalancesAsync(userId, year, cancellationToken));

    [HttpPost("{leaveTypeId:guid}/adjustments")]
    public async Task<IActionResult> Adjust(Guid userId, Guid leaveTypeId, [FromBody] AdjustmentRequest request, CancellationToken cancellationToken)
    {
        await _balanceService.AdjustBalanceAsync(userId, leaveTypeId, request, CurrentUserId, cancellationToken);
        return StatusCode(201);
    }

    [HttpPut("{leaveTypeId:guid}/{year:int}")]
    public async Task<IActionResult> Set(Guid userId, Guid leaveTypeId, int year, [FromBody] SetBalanceRequest request, CancellationToken cancellationToken)
    {
        await _balanceService.SetBalanceAsync(userId, leaveTypeId, year, request, CurrentUserId, cancellationToken);
        return Ok();
    }
}
