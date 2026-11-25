using Microsoft.EntityFrameworkCore;
using TeamLeave.Domain.Audit;
using TeamLeave.Domain.Calendar;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Notifications;
using TeamLeave.Domain.Users;

namespace TeamLeave.Application.Abstractions;

/// <summary>Port de persistance (step2_arch §3.3 : l'Application dépend des abstractions EF Core, pas du fournisseur).</summary>
public interface ITeamLeaveDbContext
{
    DbSet<User> Users { get; }

    DbSet<UserRole> UserRoles { get; }

    DbSet<LeaveType> LeaveTypes { get; }

    DbSet<Balance> Balances { get; }

    DbSet<BalanceMovement> BalanceMovements { get; }

    DbSet<LeaveRequest> LeaveRequests { get; }

    DbSet<LeaveRequestHistoryEntry> LeaveRequestHistory { get; }

    DbSet<PublicHoliday> PublicHolidays { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<AuditEntry> AuditLog { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Vide le suivi des entités avant un rejeu de cas d'usage (§6.2 M4).</summary>
    void ResetTracking();
}

/// <summary>Utilisateur authentifié de la requête courante.</summary>
public interface ICurrentUser
{
    Guid Id { get; }

    bool IsInRole(Role role);
}

public enum PasswordCheck
{
    Failed,
    Success,
    SuccessRehashNeeded,
}

public interface IPasswordHasher
{
    string Hash(string password);

    PasswordCheck Verify(string hash, string password);
}

public sealed record IssuedToken(string AccessToken, DateTime ExpiresAtUtc);

public interface ITokenIssuer
{
    IssuedToken Issue(User user);
}

public sealed record EmailMessage(string To, string Subject, string Body);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

/// <summary>
/// Violation d'unicité traduite par l'Infrastructure (nom de contrainte normalisé, §5.8 et §6.4).
/// </summary>
public sealed class UniqueConstraintViolationException(string constraintName, Exception inner)
    : Exception($"Violation de la contrainte d'unicité {constraintName}.", inner)
{
    public string ConstraintName { get; } = constraintName;
}

public static class ConstraintNames
{
    public const string UsersNormalizedEmail = "UQ_Users_NormalizedEmail";
    public const string LeaveTypesCode = "UQ_LeaveTypes_Code";
    public const string BalancesUserLeaveType = "UQ_Balances_User_LeaveType";
    public const string PublicHolidaysDate = "PK_PublicHolidays";
    public const string BalanceMovementsRequestKind = "UX_BalanceMovements_Request_Kind";
}
