using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Interfaces;

namespace TeamLeave.Application.Services;

public class AuditService
{
    private readonly ITeamLeaveDbContext _db;

    public AuditService(ITeamLeaveDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditLogDto>> GetAsync(
        string? entityType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);
        if (from.HasValue)
            query = query.Where(a => a.OccurredAt >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.OccurredAt <= to.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(a.Id, a.ActorId, a.Action, a.EntityType, a.EntityId, a.OccurredAt, a.Details, a.Reason))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>(items, page, pageSize, total);
    }
}
