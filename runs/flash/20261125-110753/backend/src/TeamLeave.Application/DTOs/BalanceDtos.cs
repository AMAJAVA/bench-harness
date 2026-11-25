namespace TeamLeave.Application.DTOs;

public record BalanceDto(Guid LeaveTypeId, string LeaveTypeCode, string LeaveTypeLabel, int Year, decimal AcquiredDays, decimal AdjustmentDays, decimal UsedDays, decimal AvailableDays);
public record AdjustmentRequest(int Year, decimal AdjustmentDays, string Reason);
public record SetBalanceRequest(decimal AcquiredDays, decimal AdjustmentDays);
