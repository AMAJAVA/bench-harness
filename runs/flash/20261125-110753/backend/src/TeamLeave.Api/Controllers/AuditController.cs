using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Services;
using TeamLeave.Domain.Enums;

namespace TeamLeave.Api.Controllers;

[ApiController]
[Route("api/v1/audit")]
[Authorize(Roles = RoleNames.AdminRh)]
public class AuditController : ApiControllerBase
{
    private readonly AuditService _auditService;

    public AuditController(AuditService auditService) => _auditService = auditService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> Get(
        [FromQuery] string? entityType,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await _auditService.GetAsync(entityType, from, to, page, pageSize, cancellationToken));
}
