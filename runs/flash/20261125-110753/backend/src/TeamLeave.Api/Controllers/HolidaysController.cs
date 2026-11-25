using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Services;
using TeamLeave.Domain.Enums;

namespace TeamLeave.Api.Controllers;

[ApiController]
[Route("api/v1/holidays")]
[Authorize]
public class HolidaysController : ApiControllerBase
{
    private readonly HolidayService _holidayService;

    public HolidaysController(HolidayService holidayService) => _holidayService = holidayService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HolidayDto>>> Get([FromQuery] int year, CancellationToken cancellationToken)
        => Ok(await _holidayService.GetAsync(year, cancellationToken));

    [HttpPost]
    [Authorize(Roles = RoleNames.AdminRh)]
    public async Task<ActionResult<HolidayDto>> Create([FromBody] CreateHolidayRequest request, CancellationToken cancellationToken)
    {
        var result = await _holidayService.CreateAsync(request, CurrentUserId, cancellationToken);
        return CreatedAtAction(nameof(Get), new { year = result.Date.Year }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.AdminRh)]
    public async Task<ActionResult<HolidayDto>> Update(Guid id, [FromBody] UpdateHolidayRequest request, CancellationToken cancellationToken)
        => Ok(await _holidayService.UpdateAsync(id, request, CurrentUserId, cancellationToken));
}
