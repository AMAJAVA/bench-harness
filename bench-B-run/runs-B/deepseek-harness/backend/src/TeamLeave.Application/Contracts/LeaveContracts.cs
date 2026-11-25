namespace TeamLeave.Application.Contracts;

public sealed record SubmitLeaveRequestRequest(
    Guid LeaveTypeId,
    DateOnly StartDate,
    string? StartHalfDay,
    DateOnly EndDate,
    string? EndHalfDay,
    string? Comment);

public sealed record AllocationDto(int Year, decimal Days);

public sealed record TimelineEventDto(string Event, DateTimeOffset At, PersonSummary? By);

public sealed record LeaveRequestListItem(
    Guid Id,
    LeaveTypeSummary LeaveType,
    DateOnly StartDate,
    string StartHalfDay,
    DateOnly EndDate,
    string EndHalfDay,
    decimal TotalDays,
    string Status,
    DateTimeOffset SubmittedAt,
    bool? OverlapFlagged);

public sealed record LeaveRequestDetail(
    Guid Id,
    PersonSummary Employee,
    LeaveTypeSummary LeaveType,
    DateOnly StartDate,
    string StartHalfDay,
    DateOnly EndDate,
    string EndHalfDay,
    decimal TotalDays,
    string Status,
    string? Comment,
    DateTimeOffset SubmittedAt,
    PersonSummary? DecidedBy,
    DateTimeOffset? DecidedAt,
    string? DecisionComment,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    IReadOnlyList<AllocationDto> Allocations,
    IReadOnlyList<TimelineEventDto> Timeline,
    OverlapDto? Overlap,
    EmployeeBalanceDto? EmployeeBalance,
    IReadOnlyList<OverlapDto>? OverlapHistory);

public sealed record EmployeeBalanceDto(
    int Year,
    Guid LeaveTypeId,
    decimal GrantedDays,
    decimal ConsumedDays,
    decimal AvailableDays);

public sealed record ApproveLeaveRequestRequest(string? Comment, bool OverlapAcknowledged = false);

public sealed record RejectLeaveRequestRequest(string? Comment);

public sealed record BalanceDto(
    Guid LeaveTypeId,
    string Code,
    string Label,
    string Color,
    decimal GrantedDays,
    decimal ConsumedDays,
    decimal AvailableDays);

public sealed record BalancesResponse(int Year, IReadOnlyList<BalanceDto> Balances);

public sealed record AdjustBalanceRequest(Guid LeaveTypeId, int Year, decimal DaysDelta, string? Reason);

public sealed record AdjustBalanceResponse(
    Guid MovementId,
    Guid EmployeeId,
    Guid LeaveTypeId,
    int Year,
    string Nature,
    decimal DaysDelta,
    string Reason,
    decimal GrantedDays,
    decimal ConsumedDays,
    decimal AvailableDays,
    Guid ActorId,
    DateTimeOffset OccurredAt);

public sealed record BalanceMovementDto(
    Guid Id,
    Guid EmployeeId,
    Guid LeaveTypeId,
    int Year,
    string Nature,
    decimal DaysDelta,
    string? Reason,
    Guid? ActorId,
    DateTimeOffset OccurredAt);

public sealed record ReportDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    TeamSummary? Team,
    bool IsActive);
