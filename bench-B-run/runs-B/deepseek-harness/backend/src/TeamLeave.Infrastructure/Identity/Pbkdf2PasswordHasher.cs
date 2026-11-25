using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using TeamLeave.Application.Abstractions;

namespace TeamLeave.Infrastructure.Identity;

public sealed class PasswordHashingOptions
{
    public const string SectionName = "PasswordHashing";
    public int Iterations { get; set; } = 600_000;
}

/// <summary>
/// PBKDF2-HMAC-SHA256, sel aléatoire de 16 octets, sortie 32 octets, paramètres versionnés.
/// L'algorithme est volontairement isolé derrière IPasswordHasher pour permettre un passage
/// à Argon2id sans toucher aux cas d'usage.
/// </summary>
public sealed class Pbkdf2PasswordHasher(IOptions<PasswordHashingOptions> options) : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private readonly int _iterations = Math.Max(100_000, options.Value.Iterations);

    public Task<string> HashAsync(string password, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            _iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        var encoded = $"$pbkdf2-sha256$v=1$i={_iterations}$" +
                      $"{Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";

        return Task.FromResult(encoded);
    }

    public Task<bool> VerifyAsync(string password, string hash, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryParse(hash, out var iterations, out var salt, out var expected))
        {
            return Task.FromResult(false);
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expected.Length);

        return Task.FromResult(CryptographicOperations.FixedTimeEquals(actual, expected));
    }

    private static bool TryParse(
        string encoded,
        out int iterations,
        out byte[] salt,
        out byte[] hash)
    {
        iterations = 0;
        salt = [];
        hash = [];

        var parts = encoded.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5 || parts[0] != "pbkdf2-sha256")
        {
            return false;
        }

        var parameters = parts[2].Split(',');
        foreach (var parameter in parameters)
        {
            var pair = parameter.Split('=', 2);
            if (pair.Length == 2 && pair[0] == "i" && int.TryParse(pair[1], out var value))
            {
                iterations = value;
            }
        }

        if (iterations < 10_000)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[3]);
            hash = Convert.FromBase64String(parts[4]);
            return salt.Length >= 8 && hash.Length >= 16;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
