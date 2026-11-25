namespace TeamLeave.Domain.Entities;

public sealed class LeaveRequestAllocation
{
    public Guid LeaveRequestId { get; set; }
    public LeaveRequest? LeaveRequest { get; set; }
    public int Year { get; set; }
    public int HalfDays { get; set; }
}
