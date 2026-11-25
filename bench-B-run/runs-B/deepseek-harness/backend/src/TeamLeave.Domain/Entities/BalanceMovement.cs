using TeamLeave.Domain.Enums;

namespace TeamLeave.Domain.Entities;

public sealed class BalanceMovement
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public int Year { get; set; }
    public int DeltaHalfDays { get; set; }
    public BalanceMovementNature Nature { get; set; }
    public Guid? LeaveRequestId { get; set; }
    public string? Reason { get; set; }
    public Guid? ActorId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
