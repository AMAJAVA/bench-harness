namespace TeamLeave.Domain.ValueObjects;

public sealed record ApprovedAbsence(Guid EmployeeId, IReadOnlyCollection<LeaveHalfDay> HalfDays);
