namespace TeamLeave.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public Guid? ActorId { get; set; }
    public User? Actor { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? Details { get; set; }
    public string? Reason { get; set; }
}
