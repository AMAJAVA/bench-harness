namespace TeamLeave.Application.DTOs;

public record LeaveTypeDto(Guid Id, string Code, string Label, bool IsActive, bool TrackBalance, int DisplayOrder);
public record CreateLeaveTypeRequest(string Code, string Label, bool IsActive, bool TrackBalance, int DisplayOrder);
public record UpdateLeaveTypeRequest(string Label, bool IsActive, bool TrackBalance, int DisplayOrder);
