namespace TeamLeave.Application.Common;

public static class PasswordPolicy
{
    public const int MinimumLength = 12;
    public const int MaximumLength = 128;

    private static readonly HashSet<string> Forbidden = new(StringComparer.OrdinalIgnoreCase)
    {
        "password1234", "motdepasse123", "azertyuiop123", "qwertyuiop123",
        "123456789012", "teamleave2026", "changeme12345"
    };

    public static bool IsValid(string? password, out string reason)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumLength)
        {
            reason = $"Le mot de passe doit contenir au moins {MinimumLength} caractères.";
            return false;
        }

        if (password.Length > MaximumLength)
        {
            reason = $"Le mot de passe ne peut pas dépasser {MaximumLength} caractères.";
            return false;
        }

        if (Forbidden.Contains(password))
        {
            reason = "Ce mot de passe est trop commun.";
            return false;
        }

        reason = string.Empty;
        return true;
    }
}
