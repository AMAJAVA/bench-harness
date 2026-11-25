using System.ComponentModel.DataAnnotations;
using TeamLeave.Domain.Enums;

namespace TeamLeave.Domain.Entities;

public class LeaveRequest
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public User Employee { get; set; } = null!;
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int WorkingDays { get; set; }
    public LeaveRequestStatus Status { get; set; }
    public string? EmployeeComment { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public Guid? DeciderId { get; set; }
    public User? Decider { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? ManagerComment { get; set; }
    public bool HasOverlapWarning { get; set; }
    public decimal? OverlapRatio { get; set; }
    public Guid? AcknowledgedOverlapBy { get; set; }
    public User? AcknowledgedOverlapByUser { get; set; }
    public DateTimeOffset? AcknowledgedOverlapAt { get; set; }
    [ConcurrencyCheck]
    public int Version { get; set; }
}
