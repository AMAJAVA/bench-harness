namespace TeamLeave.Application.DTOs;

public record TeamDto(Guid Id, string Name);
public record MeDto(Guid Id, string Email, string FirstName, string LastName, IReadOnlyList<string> Roles, TeamDto? Team);
