namespace TeamLeave.Domain.Users;

/// <summary>Politique de mot de passe (ENF-S4, step2_arch §7.1) : 12 à 128 caractères, différent de l'email, hors liste noire.</summary>
public static class PasswordPolicy
{
    public const int MinLength = 12;
    public const int MaxLength = 128;

    // Extrait embarqué des mots de passe les plus courants de 12 caractères et plus.
    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "123456789012", "1234567890123", "password1234", "password12345", "motdepasse12", "motdepasse123",
        "azertyuiop12", "azertyuiopqs", "qwertyuiop12", "qwertyuiopas", "iloveyou1234", "administrator",
        "000000000000", "111111111111", "abcdefghijkl", "abc123456789", "welcome12345", "bienvenue123",
        "bienvenue1234", "soleil123456", "changeme1234", "letmein12345", "passw0rd1234", "p@ssw0rd1234",
    };

    /// <summary>Renvoie le message d'erreur, ou null si le mot de passe est acceptable.</summary>
    public static string? Validate(string? password, string? email)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength || password.Length > MaxLength)
        {
            return $"Le mot de passe doit contenir entre {MinLength} et {MaxLength} caractères.";
        }

        if (email is not null && string.Equals(password.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return "Le mot de passe ne doit pas être égal à l'adresse email.";
        }

        if (CommonPasswords.Contains(password))
        {
            return "Ce mot de passe est trop courant.";
        }

        return null;
    }
}
