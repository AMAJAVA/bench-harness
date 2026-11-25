namespace TeamLeave.Application.DTOs;

public record LoginRequest(string Email, string Password);
public record UserInfoDto(Guid Id, string Email, string FirstName, string LastName, IReadOnlyList<string> Roles, Guid? TeamId);
public record AuthResponse(string AccessToken, string TokenType, int ExpiresIn, UserInfoDto User);
