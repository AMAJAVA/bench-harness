using TeamLeave.Domain.Entities;

namespace TeamLeave.Application.Abstractions;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "https://api.teamleave.example";
    public string Audience { get; set; } = "teamleave-api";
    public string SigningKey { get; set; } = string.Empty;
    public string CurrentKid { get; set; } = "k1";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(User user);
}
