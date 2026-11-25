using System.ComponentModel.DataAnnotations;

namespace TeamLeave.Domain.Entities;

public class LeaveBalance
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;
    public int Year { get; set; }
    public decimal AcquiredDays { get; set; }
    public decimal AdjustmentDays { get; set; }
    public decimal UsedDays { get; set; }
    [ConcurrencyCheck]
    public int Version { get; set; }
    public decimal AvailableDays => AcquiredDays + AdjustmentDays - UsedDays;
}
