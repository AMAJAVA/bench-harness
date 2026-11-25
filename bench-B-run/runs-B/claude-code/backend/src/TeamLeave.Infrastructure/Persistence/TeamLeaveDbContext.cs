using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Domain.Audit;
using TeamLeave.Domain.Calendar;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Notifications;
using TeamLeave.Domain.Users;

namespace TeamLeave.Infrastructure.Persistence;

/// <summary>
/// DbContext unique pour SQL Server (production) et SQLite (tests). Applique :
/// le jeton de version applicatif (§6.2 M2), l'interdiction de modifier les journaux (§3.5 « ajout seul »),
/// la traduction des violations d'unicité (§6.4).
/// </summary>
public sealed class TeamLeaveDbContext(DbContextOptions<TeamLeaveDbContext> options) : DbContext(options), ITeamLeaveDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();

    public DbSet<Balance> Balances => Set<Balance>();

    public DbSet<BalanceMovement> BalanceMovements => Set<BalanceMovement>();

    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    public DbSet<LeaveRequestHistoryEntry> LeaveRequestHistory => Set<LeaveRequestHistoryEntry>();

    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<AuditEntry> AuditLog => Set<AuditEntry>();

    public void ResetTracking() => ChangeTracker.Clear();

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new NotSupportedException("Utiliser SaveChangesAsync.");

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        GuardAppendOnly();
        BumpVersions();
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException && UniqueConstraintName(ex) is { } name)
        {
            throw new UniqueConstraintViolationException(name, ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeamLeaveDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Horodatages UTC (GAP-09) : le Kind est restauré à la lecture.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>().HavePrecision(3);
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>().HavePrecision(3);
    }

    private static string? UniqueConstraintName(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? string.Empty;
        var isUnique = ex.InnerException switch
        {
            SqliteException sqlite => sqlite.SqliteErrorCode == 19 && message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase),
            SqlException sql => sql.Number is 2601 or 2627,
            _ => false,
        };
        if (!isUnique)
        {
            return null;
        }

        // SQL Server cite le nom de l'index ; SQLite cite les colonnes.
        (string Name, string[] Markers)[] known =
        [
            (ConstraintNames.BalancesUserLeaveType, ["UQ_Balances_User_LeaveType", "Balances.UserId, Balances.LeaveTypeId"]),
            (ConstraintNames.UsersNormalizedEmail, ["UQ_Users_NormalizedEmail", "Users.NormalizedEmail"]),
            (ConstraintNames.LeaveTypesCode, ["UQ_LeaveTypes_Code", "LeaveTypes.Code"]),
            (ConstraintNames.PublicHolidaysDate, ["PK_PublicHolidays", "PublicHolidays.Date"]),
            (ConstraintNames.BalanceMovementsRequestKind, ["UX_BalanceMovements_Request_Kind", "BalanceMovements.LeaveRequestId, BalanceMovements.Kind"]),
        ];
        foreach (var (name, markers) in known)
        {
            if (markers.Any(m => message.Contains(m, StringComparison.OrdinalIgnoreCase)))
            {
                return name;
            }
        }

        return "UNKNOWN_UNIQUE_CONSTRAINT";
    }

    private void GuardAppendOnly()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is BalanceMovement or LeaveRequestHistoryEntry or AuditEntry
                && entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException($"{entry.Entity.GetType().Name} est en ajout seul (RB-25, RB-26).");
            }
        }
    }

    private void BumpVersions()
    {
        foreach (var entry in ChangeTracker.Entries<IVersioned>().Where(e => e.State == EntityState.Modified))
        {
            var version = entry.Property(e => e.Version);
            if (version.CurrentValue == version.OriginalValue)
            {
                version.CurrentValue = version.OriginalValue + 1;
            }
        }
    }
}
