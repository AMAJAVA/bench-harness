namespace TeamLeave.Application.Contracts;

public sealed record LoginRequest(string? Email, string? Password);

public sealed record RefreshRequest(string? RefreshToken);

public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);

public sealed record UserSummary(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles,
    Guid TeamId,
    bool MustChangePassword);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserSummary User);

public sealed record MeTeam(Guid Id, string Name);

public sealed record MeManager(Guid Id, string FirstName, string LastName);

public sealed record MeResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles,
    MeTeam? Team,
    MeManager? Manager,
    bool MustChangePassword);
