namespace TeamLeave.Application.Contracts;

public sealed record CalendarAbsenceDto(
    Guid? LeaveRequestId,
    Guid EmployeeId,
    string EmployeeDisplayName,
    DateOnly StartDate,
    string StartHalfDay,
    DateOnly EndDate,
    string EndHalfDay,
    LeaveTypeSummary? LeaveType);

public sealed record CalendarResponse(
    Guid TeamId,
    string TeamName,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<CalendarAbsenceDto> Absences);
