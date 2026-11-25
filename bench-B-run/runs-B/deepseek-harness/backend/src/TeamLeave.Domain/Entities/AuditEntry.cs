using TeamLeave.Domain.Enums;

namespace TeamLeave.Domain.Entities;

public sealed class AuditEntry
{
    public long Id { get; set; }
    public Guid? ActorId { get; set; }
    public AuditActorType ActorType { get; set; } = AuditActorType.USER;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? BeforeSummary { get; set; }
    public string? AfterSummary { get; set; }
    public string? Reason { get; set; }
    public Guid? CorrelationId { get; set; }
}
