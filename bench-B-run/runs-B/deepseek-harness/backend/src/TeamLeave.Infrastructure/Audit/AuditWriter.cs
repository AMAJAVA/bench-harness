using System.Text.Json;
using TeamLeave.Application.Abstractions;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;
using TeamLeave.Infrastructure.Persistence;

namespace TeamLeave.Infrastructure.Audit;

public sealed class AuditWriter(TeamLeaveDbContext db, IClock clock)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Add(
        Guid? actorId,
        string action,
        string entityType,
        Guid? entityId,
        object? before = null,
        object? after = null,
        string? reason = null,
        Guid? correlationId = null,
        AuditActorType actorType = AuditActorType.USER)
    {
        db.AuditEntries.Add(new AuditEntry
        {
            ActorId = actorId,
            ActorType = actorId is null ? AuditActorType.SYSTEM : actorType,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OccurredAt = clock.UtcNow,
            BeforeSummary = before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            AfterSummary = after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
            Reason = reason,
            CorrelationId = correlationId
        });
    }
}
