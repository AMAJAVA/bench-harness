namespace TeamLeave.Application.DTOs;

public record LeaveTypeInfoDto(Guid Id, string Code, string Label);
public record OverlapDetailDto(DateOnly Date, IReadOnlyList<string> AbsentEmployees);
public record LeaveRequestDto(
    Guid Id,
    Guid EmployeeId,
    Guid TeamId,
    LeaveTypeInfoDto LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    int WorkingDays,
    string Status,
    string? EmployeeComment,
    DateTimeOffset SubmittedAt,
    Guid? DeciderId,
    DateTimeOffset? DecidedAt,
    string? ManagerComment,
    bool HasOverlapWarning,
    decimal? OverlapRatio,
    IReadOnlyList<OverlapDetailDto> OverlapDetails,
    Guid? AcknowledgedOverlapBy,
    DateTimeOffset? AcknowledgedOverlapAt);
public record CreateLeaveRequestRequest(Guid LeaveTypeId, DateOnly StartDate, DateOnly EndDate, string? Comment);
public record DecisionRequest(string Action, string? Comment, bool AcknowledgeOverlapWarning);
