using TeamLeave.Domain.Enums;

namespace TeamLeave.Domain.Entities;

public sealed class OverlapEvaluation
{
    public long Id { get; set; }
    public Guid LeaveRequestId { get; set; }
    public LeaveRequest? LeaveRequest { get; set; }
    public Guid TeamId { get; set; }
    public int TeamSize { get; set; }
    public decimal MaxRate { get; set; }
    public string BreachingHalfDaysJson { get; set; } = "[]";
    public OverlapTriggerKind TriggerKind { get; set; }
    public Guid? EvaluatedById { get; set; }
    public DateTimeOffset EvaluatedAt { get; set; }
}
