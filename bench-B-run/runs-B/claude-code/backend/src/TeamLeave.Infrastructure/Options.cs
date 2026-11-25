namespace TeamLeave.Infrastructure;

/// <summary>Section « Database ».</summary>
public sealed class DatabaseOptions
{
    public const string Section = "Database";

    /// <summary>SqlServer (production, step2_arch §2.3) ou Sqlite (tests, développement léger).</summary>
    public string Provider { get; set; } = "SqlServer";

    /// <summary>Applique les migrations (SQL Server) ou crée le schéma (SQLite) au démarrage. Désactivé en production (bundle de migrations, §9.5).</summary>
    public bool AutoMigrate { get; set; }
}

/// <summary>Section « Jwt » (§7.1, §9.3).</summary>
public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "teamleave-api";

    public string Audience { get; set; } = "teamleave-spa";

    public int LifetimeMinutes { get; set; } = 60;

    public int ClockSkewSeconds { get; set; } = 30;

    public SigningKeyOptions SigningKeys { get; set; } = new();

    public sealed class SigningKeyOptions
    {
        /// <summary>Clé courante, 256 bits au moins, encodée en Base64 (Key Vault en production).</summary>
        public string? Current { get; set; }

        /// <summary>Clé précédente, acceptée à la validation pendant la rotation.</summary>
        public string? Previous { get; set; }
    }
}

/// <summary>Section « PasswordHashing » (ENF-S4).</summary>
public sealed class PasswordHashingOptions
{
    public const string Section = "PasswordHashing";

    public int Iterations { get; set; } = 210_000;
}

/// <summary>Section « Smtp » (§2.5, §9.3). Sans hôte, les emails sont seulement journalisés (développement).</summary>
public sealed class SmtpOptions
{
    public const string Section = "Smtp";

    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string From { get; set; } = "teamleave@localhost";

    public string? Username { get; set; }

    public string? Password { get; set; }
}

/// <summary>Section « Notifications » (ADR-03).</summary>
public sealed class NotificationOptions
{
    public const string Section = "Notifications";

    public bool DispatcherEnabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 30;
}

/// <summary>Section « Seed » (GAP-16, GAP-17).</summary>
public sealed class SeedOptions
{
    public const string Section = "Seed";

    public bool Holidays { get; set; } = true;

    public string? InitialAdminEmail { get; set; }

    public string? InitialAdminTemporaryPassword { get; set; }

    public string InitialAdminFirstName { get; set; } = "Admin";

    public string InitialAdminLastName { get; set; } = "RH";
}
