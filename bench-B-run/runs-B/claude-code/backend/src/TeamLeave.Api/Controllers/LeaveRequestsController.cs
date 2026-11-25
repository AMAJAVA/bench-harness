using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TeamLeave.Api.Infrastructure;
using TeamLeave.Application.Calendar;
using TeamLeave.Application.Contracts;
using TeamLeave.Application.LeaveRequests;

namespace TeamLeave.Api.Controllers;

/// <summary>Demandes de congé (§5.3). Contrôleur fin : liaison, appel du cas d'usage, réponse.</summary>
[ApiController]
[Authorize]
[Route("api/v1/leave-requests")]
public sealed class LeaveRequestsController(LeaveRequestService service) : ControllerBase
{
    [HttpPost("simulate")]
    public Task<SimulationDto> Simulate([FromBody] SimulateRequest request, CancellationToken ct) => service.SimulateAsync(request, ct);

    [HttpPost]
    public async Task<ActionResult<LeaveRequestDto>> Submit([FromBody] SubmitLeaveRequest request, CancellationToken ct)
    {
        var created = await service.SubmitAsync(request, ct);
        return Created($"/api/v1/leave-requests/{created.Id}", created);
    }

    [HttpGet]
    public Task<PagedResult<LeaveRequestSummaryDto>> List([FromQuery] LeaveRequestListQuery query, CancellationToken ct) =>
        service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public Task<LeaveRequestDto> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpGet("{id:guid}/overlap")]
    [Authorize(Policy = Policies.ManagerOrHrAdmin)]
    public Task<OverlapAnalysisDto> Overlap(Guid id, CancellationToken ct) => service.GetOverlapAsync(id, ct);

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = Policies.ManagerOrHrAdmin)]
    public Task<LeaveRequestDto> Approve(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ApproveRequest? request,
        CancellationToken ct) =>
        service.ApproveAsync(id, request ?? new ApproveRequest(), ct);

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = Policies.ManagerOrHrAdmin)]
    public Task<LeaveRequestDto> Reject(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RejectRequest? request,
        CancellationToken ct) =>
        service.RejectAsync(id, request ?? new RejectRequest(), ct);

    [HttpPost("{id:guid}/cancel")]
    public Task<LeaveRequestDto> Cancel(Guid id, CancellationToken ct) => service.CancelAsync(id, ct);
}

/// <summary>Calendrier des absences approuvées (§5.5).</summary>
[ApiController]
[Authorize]
[Route("api/v1/calendar")]
public sealed class CalendarController(CalendarService service) : ControllerBase
{
    [HttpGet]
    public Task<CalendarDto> Get([FromQuery] CalendarQuery query, CancellationToken ct) => service.GetAsync(query, ct);
}
