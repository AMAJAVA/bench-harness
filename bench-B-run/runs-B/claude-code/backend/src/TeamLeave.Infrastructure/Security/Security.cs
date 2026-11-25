using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TeamLeave.Application.Abstractions;
using TeamLeave.Domain.Users;

namespace TeamLeave.Infrastructure.Security;

/// <summary>
/// PBKDF2-HMAC-SHA512 (format V3 de <see cref="PasswordHasher{TUser}"/>), itérations configurables (ENF-S4, ADR-02).
/// Un hachage à moins d'itérations que la configuration est signalé « à recalculer ».
/// </summary>
public sealed class Pbkdf2PasswordHasher : Application.Abstractions.IPasswordHasher
{
    private readonly PasswordHasher<User> _inner;

    public Pbkdf2PasswordHasher(IOptions<PasswordHashingOptions> options)
    {
        _inner = new PasswordHasher<User>(Microsoft.Extensions.Options.Options.Create(new PasswordHasherOptions
        {
            CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
            IterationCount = options.Value.Iterations,
        }));
    }

    public string Hash(string password) => _inner.HashPassword(null!, password);

    public PasswordCheck Verify(string hash, string password)
    {
        try
        {
            return _inner.VerifyHashedPassword(null!, hash, password) switch
            {
                PasswordVerificationResult.Success => PasswordCheck.Success,
                PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.SuccessRehashNeeded,
                _ => PasswordCheck.Failed,
            };
        }
        catch (FormatException)
        {
            return PasswordCheck.Failed;
        }
    }
}

/// <summary>Clés HS256 décodées depuis la configuration (courante + précédente, §7.1).</summary>
public sealed class JwtSigningKeys
{
    public JwtSigningKeys(IOptions<JwtOptions> options)
    {
        var keys = options.Value.SigningKeys;
        Current = Decode(keys.Current, "Jwt:SigningKeys:Current")
            ?? throw new InvalidOperationException("La clé de signature JWT (Jwt:SigningKeys:Current) n'est pas configurée.");
        Previous = Decode(keys.Previous, "Jwt:SigningKeys:Previous");
    }

    public SymmetricSecurityKey Current { get; }

    public SymmetricSecurityKey? Previous { get; }

    public IEnumerable<SecurityKey> ValidationKeys => Previous is null ? [Current] : [Current, Previous];

    private static SymmetricSecurityKey? Decode(string? base64, string name)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return null;
        }

        var bytes = Convert.FromBase64String(base64);
        if (bytes.Length < 32)
        {
            throw new InvalidOperationException($"{name} doit faire au moins 256 bits.");
        }

        return new SymmetricSecurityKey(bytes) { KeyId = Convert.ToHexString(SHA256.HashData(bytes))[..16] };
    }
}

/// <summary>Émission du JWT (ADR-02) : sub, rôles, sst, mcp ; 60 min ; HS256.</summary>
public sealed class JwtTokenIssuer(IOptions<JwtOptions> options, JwtSigningKeys keys, Clock clock) : ITokenIssuer
{
    public const string SecurityStampClaim = "sst";
    public const string MustChangePasswordClaim = "mcp";

    private static readonly JsonWebTokenHandler Handler = new();

    public IssuedToken Issue(User user)
    {
        var settings = options.Value;
        var now = clock.UtcNow;
        var expires = now.AddMinutes(settings.LifetimeMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(keys.Current, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
                ["role"] = user.RoleList().Select(r => r.ToString()).ToArray(),
                ["name"] = user.FullName,
                [JwtRegisteredClaimNames.Email] = user.Email,
                [SecurityStampClaim] = user.SecurityStamp.ToString(),
                [MustChangePasswordClaim] = user.MustChangePassword,
            },
        };
        return new IssuedToken(Handler.CreateToken(descriptor), expires);
    }
}
