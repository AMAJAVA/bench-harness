using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Api.Infrastructure;
using TeamLeave.Application.Admin;
using TeamLeave.Application.Contracts;

namespace TeamLeave.Api.Controllers;

/// <summary>Types de congé (§5.6). Lecture ouverte aux utilisateurs authentifiés, écriture réservée à l'admin RH.</summary>
[ApiController]
[Authorize]
[Route("api/v1/leave-types")]
public sealed class LeaveTypesController(LeaveTypeService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<LeaveTypeDto>> List([FromQuery] bool includeInactive, CancellationToken ct) =>
        service.ListAsync(includeInactive, ct);

    [HttpPost]
    [Authorize(Policy = Policies.HrAdmin)]
    public async Task<ActionResult<LeaveTypeDto>> Create([FromBody] CreateLeaveTypeRequest request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return Created($"/api/v1/leave-types/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.HrAdmin)]
    public Task<LeaveTypeDto> Update(Guid id, [FromBody] UpdateLeaveTypeRequest request, CancellationToken ct) =>
        service.UpdateAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.HrAdmin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}

/// <summary>Utilisateurs et soldes (§5.6), réservé à l'admin RH (P-15, P-16).</summary>
[ApiController]
[Authorize(Policy = Policies.HrAdmin)]
[Route("api/v1/users")]
public sealed class UsersController(UserAdminService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<UserDto>> List([FromQuery] UserListQuery query, CancellationToken ct) => service.ListAsync(query, ct);

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return Created($"/api/v1/users/{created.Id}", created);
    }

    [HttpGet("{id:guid}")]
    public Task<UserDto> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPut("{id:guid}")]
    public Task<UserDto> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct) => service.UpdateAsync(id, request, ct);

    [HttpPost("{id:guid}/deactivate")]
    public Task<UserDto> Deactivate(Guid id, CancellationToken ct) => service.DeactivateAsync(id, ct);

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await service.ResetPasswordAsync(id, request, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, CancellationToken ct)
    {
        var export = await service.ExportAsync(id, ct);
        Response.Headers.ContentDisposition = $"attachment; filename=\"teamleave-export-{id}.json\"";
        return new JsonResult(export);
    }

    [HttpGet("{id:guid}/balances")]
    public Task<IReadOnlyList<BalanceDto>> Balances(Guid id, CancellationToken ct) => service.GetBalancesAsync(id, ct);

    [HttpGet("{id:guid}/balances/{typeId:guid}/movements")]
    public Task<PagedResult<MovementDto>> Movements(Guid id, Guid typeId, [FromQuery] PageQuery page, CancellationToken ct) =>
        service.GetMovementsAsync(id, typeId, page, ct);

    [HttpPost("{id:guid}/balances/{typeId:guid}/movements")]
    public async Task<ActionResult<MovementDto>> AddMovement(Guid id, Guid typeId, [FromBody] CreateMovementRequest request, CancellationToken ct)
    {
        var movement = await service.AddMovementAsync(id, typeId, request, ct);
        return Created($"/api/v1/users/{id}/balances/{typeId}/movements", movement);
    }
}

/// <summary>Jours fériés (§5.6) : lecture pour tous (GAP-23), écriture admin RH.</summary>
[ApiController]
[Authorize]
[Route("api/v1/holidays")]
public sealed class HolidaysController(HolidayService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<HolidayDto>> List([FromQuery, Required, Range(1900, 2200)] int? year, CancellationToken ct) =>
        service.ListAsync(year!.Value, ct);

    [HttpPost]
    [Authorize(Policy = Policies.HrAdmin)]
    public async Task<ActionResult<HolidayDto>> Create([FromBody] CreateHolidayRequest request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return Created($"/api/v1/holidays/{created.Date:yyyy-MM-dd}", created);
    }

    [HttpDelete("{date}")]
    [Authorize(Policy = Policies.HrAdmin)]
    public async Task<IActionResult> Delete(DateOnly date, CancellationToken ct)
    {
        await service.DeleteAsync(date, ct);
        return NoContent();
    }
}

/// <summary>Journaux des notifications et d'administration (§5.7), admin RH.</summary>
[ApiController]
[Authorize(Policy = Policies.HrAdmin)]
[Route("api/v1")]
public sealed class JournalsController(JournalService service) : ControllerBase
{
    [HttpGet("notifications")]
    public Task<PagedResult<NotificationDto>> Notifications([FromQuery] NotificationListQuery query, CancellationToken ct) =>
        service.ListNotificationsAsync(query, ct);

    [HttpGet("audit-log")]
    public Task<PagedResult<AuditEntryDto>> AuditLog([FromQuery] AuditLogQuery query, CancellationToken ct) =>
        service.ListAuditAsync(query, ct);
}
