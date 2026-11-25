namespace TeamLeave.Application.DTOs;

public record HolidayDto(Guid Id, DateOnly Date, string Label, bool IsActive);
public record CreateHolidayRequest(DateOnly Date, string Label);
public record UpdateHolidayRequest(string Label, bool IsActive);
