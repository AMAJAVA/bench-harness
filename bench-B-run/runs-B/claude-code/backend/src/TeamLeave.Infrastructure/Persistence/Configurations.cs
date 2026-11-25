using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TeamLeave.Domain.Audit;
using TeamLeave.Domain.Calendar;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Notifications;
using TeamLeave.Domain.Users;

namespace TeamLeave.Infrastructure.Persistence;

// Mapping Fluent conforme au schéma de référence (step2_arch §4.3). Les CHECK et filtres d'index
// n'utilisent que le sous-ensemble commun SQL Server / SQLite (§4.1).

internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users", t =>
        {
            t.HasCheckConstraint("CK_Users_NotOwnManager", "ManagerId IS NULL OR ManagerId <> Id");
            t.HasCheckConstraint("CK_Users_FailedLoginCount", "FailedLoginCount >= 0");
            t.HasCheckConstraint(
                "CK_Users_Deactivation",
                "(IsActive = 1 AND DeactivatedAtUtc IS NULL) OR (IsActive = 0 AND DeactivatedAtUtc IS NOT NULL)");
        });
        b.HasKey(u => u.Id).HasName("PK_Users");
        b.Property(u => u.Id).ValueGeneratedNever();
        b.Property(u => u.Email).HasMaxLength(254).IsRequired();
        b.Property(u => u.NormalizedEmail).HasMaxLength(254).IsRequired();
        b.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        b.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        b.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
        b.Property(u => u.FailedLoginCount).HasDefaultValue(0);
        b.Property(u => u.Version).IsConcurrencyToken().HasDefaultValue(0);
        b.Ignore(u => u.FullName);
        b.HasIndex(u => u.NormalizedEmail).IsUnique().HasDatabaseName("UQ_Users_NormalizedEmail");
        b.HasIndex(u => u.ManagerId).HasDatabaseName("IX_Users_ManagerId");
        b.HasOne<User>().WithMany().HasForeignKey(u => u.ManagerId).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Users_Manager");
        b.HasMany(u => u.Roles).WithOne().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.ClientCascade).HasConstraintName("FK_UserRoles_Users");
        b.Navigation(u => u.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.ToTable("UserRoles", t => t.HasCheckConstraint("CK_UserRoles_Role", "Role IN ('Employee', 'Manager', 'HrAdmin')"));
        b.HasKey(r => new { r.UserId, r.Role }).HasName("PK_UserRoles");
        b.Property(r => r.Role).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        b.HasIndex(r => r.Role).HasDatabaseName("IX_UserRoles_Role");
    }
}

internal sealed class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> b)
    {
        b.ToTable("LeaveTypes");
        b.HasKey(t => t.Id).HasName("PK_LeaveTypes");
        b.Property(t => t.Id).ValueGeneratedNever();
        b.Property(t => t.Code).HasMaxLength(10).IsUnicode(false).IsRequired();
        b.Property(t => t.Label).HasMaxLength(100).IsRequired();
        b.Property(t => t.Version).IsConcurrencyToken().HasDefaultValue(0);
        b.HasIndex(t => t.Code).IsUnique().HasDatabaseName("UQ_LeaveTypes_Code");
    }
}

internal sealed class BalanceConfiguration : IEntityTypeConfiguration<Balance>
{
    public void Configure(EntityTypeBuilder<Balance> b)
    {
        b.ToTable("Balances", t => t.HasCheckConstraint("CK_Balances_Amount_NonNegative", "AmountHundredths >= 0"));
        b.HasKey(x => x.Id).HasName("PK_Balances");
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken().HasDefaultValue(0);
        b.HasIndex(x => new { x.UserId, x.LeaveTypeId }).IsUnique().HasDatabaseName("UQ_Balances_User_LeaveType");
        b.HasIndex(x => x.LeaveTypeId).HasDatabaseName("IX_Balances_LeaveTypeId");
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Balances_Users");
        b.HasOne<LeaveType>().WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Balances_LeaveTypes");
    }
}

internal sealed class BalanceMovementConfiguration : IEntityTypeConfiguration<BalanceMovement>
{
    public void Configure(EntityTypeBuilder<BalanceMovement> b)
    {
        b.ToTable("BalanceMovements", t =>
        {
            t.HasCheckConstraint("CK_BalanceMovements_Kind", "Kind IN ('Allocation', 'Adjustment', 'Debit', 'Recredit')");
            t.HasCheckConstraint("CK_BalanceMovements_Quantity_NonZero", "QuantityHundredths <> 0");
            t.HasCheckConstraint("CK_BalanceMovements_BalanceAfter", "BalanceAfterHundredths >= 0");
            t.HasCheckConstraint(
                "CK_BalanceMovements_Sign",
                "(Kind = 'Allocation' AND QuantityHundredths > 0) OR (Kind = 'Adjustment') OR (Kind = 'Debit' AND QuantityHundredths < 0) OR (Kind = 'Recredit' AND QuantityHundredths > 0)");
            t.HasCheckConstraint(
                "CK_BalanceMovements_Links",
                "(Kind IN ('Allocation', 'Adjustment') AND Reason IS NOT NULL AND LeaveRequestId IS NULL) OR (Kind IN ('Debit', 'Recredit') AND LeaveRequestId IS NOT NULL)");
        });
        b.HasKey(x => x.Id).HasName("PK_BalanceMovements");
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Kind).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.Reason).HasMaxLength(500);
        b.HasIndex(x => new { x.LeaveRequestId, x.Kind })
            .IsUnique()
            .HasFilter("LeaveRequestId IS NOT NULL")
            .HasDatabaseName("UX_BalanceMovements_Request_Kind");
        b.HasIndex(x => new { x.BalanceId, x.Id }).HasDatabaseName("IX_BalanceMovements_Balance");
        b.HasOne<Balance>().WithMany().HasForeignKey(x => x.BalanceId).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_BalanceMovements_Balances");
        b.HasOne<LeaveRequest>().WithMany().HasForeignKey(x => x.LeaveRequestId).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_BalanceMovements_LeaveRequests");
        b.HasOne<User>().WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_BalanceMovements_Author");
    }
}

internal sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> b)
    {
        b.ToTable("LeaveRequests", t =>
        {
            t.HasCheckConstraint("CK_LeaveRequests_Dates", "EndDate >= StartDate");
            t.HasCheckConstraint("CK_LeaveRequests_DaysCount", "DaysCount >= 1");
            t.HasCheckConstraint("CK_LeaveRequests_Status", "Status IN ('Pending', 'Approved', 'Rejected', 'Cancelled')");
            t.HasCheckConstraint(
                "CK_LeaveRequests_SubmissionOverlap",
                "SubmissionOverlapTeamSize >= 0 AND SubmissionOverlapMaxAbsent >= 0 AND SubmissionOverlapMaxAbsent <= SubmissionOverlapTeamSize");
            t.HasCheckConstraint(
                "CK_LeaveRequests_Decision",
                "(Status = 'Pending' AND DecidedById IS NULL AND DecidedAtUtc IS NULL) OR (Status IN ('Approved', 'Rejected') AND DecidedById IS NOT NULL AND DecidedAtUtc IS NOT NULL) OR (Status = 'Cancelled')");
            t.HasCheckConstraint("CK_LeaveRequests_NoSelfDecision", "DecidedById IS NULL OR DecidedById <> RequesterId");
            t.HasCheckConstraint("CK_LeaveRequests_RejectionComment", "Status <> 'Rejected' OR DecisionComment IS NOT NULL");
            t.HasCheckConstraint(
                "CK_LeaveRequests_Cancellation",
                "(Status = 'Cancelled' AND CancelledAtUtc IS NOT NULL) OR (Status <> 'Cancelled' AND CancelledAtUtc IS NULL AND CancelledById IS NULL)");
            t.HasCheckConstraint(
                "CK_LeaveRequests_Acknowledgement",
                "(OverlapAcknowledgedById IS NULL AND OverlapAcknowledgedAtUtc IS NULL AND OverlapAcknowledgedAbsent IS NULL AND OverlapAcknowledgedTeamSize IS NULL) OR (OverlapAcknowledgedById IS NOT NULL AND OverlapAcknowledgedAtUtc IS NOT NULL AND OverlapAcknowledgedTeamSize > 0 AND OverlapAcknowledgedAbsent >= 0 AND OverlapAcknowledgedAbsent <= OverlapAcknowledgedTeamSize)");
        });
        b.HasKey(x => x.Id).HasName("PK_LeaveRequests");
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.RequesterComment).HasMaxLength(500);
        b.Property(x => x.DecisionComment).HasMaxLength(1000);
        b.Property(x => x.CancellationReason).HasMaxLength(200);
        b.Property(x => x.Version).IsConcurrencyToken().HasDefaultValue(0);
        b.Ignore(x => x.Period);
        b.HasIndex(x => new { x.RequesterId, x.Status }).HasDatabaseName("IX_LeaveRequests_Requester_Status");
        b.HasIndex(x => new { x.StartDate, x.EndDate }).HasFilter("Status = 'Approved'").HasDatabaseName("IX_LeaveRequests_Approved_Dates");
        b.HasIndex(x => x.SubmittedAtUtc).HasFilter("Status = 'Pending'").HasDatabaseName("IX_LeaveRequests_Pending_Submitted");
        b.HasIndex(x => x.LeaveTypeId).HasDatabaseName("IX_LeaveRequests_LeaveTypeId");
        b.HasOne<User>().WithMany().HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_LeaveRequests_Requester");
        b.HasOne<LeaveType>().WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_LeaveRequests_LeaveTypes");
        b.HasOne<User>().WithMany().HasForeignKey(x => x.DecidedById).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_LeaveRequests_DecidedBy");
        b.HasOne<User>().WithMany().HasForeignKey(x => x.OverlapAcknowledgedById).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_LeaveRequests_AcknowledgedBy");
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CancelledById).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_LeaveRequests_CancelledBy");
        b.HasMany(x => x.History).WithOne().HasForeignKey(h => h.LeaveRequestId).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_LeaveRequestHistory_LeaveRequests");
        b.Navigation(x => x.History).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class LeaveRequestHistoryConfiguration : IEntityTypeConfiguration<LeaveRequestHistoryEntry>
{
    public void Configure(EntityTypeBuilder<LeaveRequestHistoryEntry> b)
    {
        b.ToTable("LeaveRequestHistory", t =>
        {
            t.HasCheckConstraint("CK_LeaveRequestHistory_FromStatus", "FromStatus IS NULL OR FromStatus IN ('Pending', 'Approved', 'Rejected', 'Cancelled')");
            t.HasCheckConstraint("CK_LeaveRequestHistory_ToStatus", "ToStatus IN ('Pending', 'Approved', 'Rejected', 'Cancelled')");
        });
        b.HasKey(x => x.Id).HasName("PK_LeaveRequestHistory");
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.Comment).HasMaxLength(1000);
        b.HasIndex(x => new { x.LeaveRequestId, x.Id }).HasDatabaseName("IX_LeaveRequestHistory_Request");
        b.HasOne<User>().WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_LeaveRequestHistory_Author");
    }
}

internal sealed class PublicHolidayConfiguration : IEntityTypeConfiguration<PublicHoliday>
{
    public void Configure(EntityTypeBuilder<PublicHoliday> b)
    {
        b.ToTable("PublicHolidays");
        b.HasKey(x => x.Date).HasName("PK_PublicHolidays");
        b.Property(x => x.Date).ValueGeneratedNever();
        b.Property(x => x.Label).HasMaxLength(100).IsRequired();
    }
}

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications", t =>
        {
            t.HasCheckConstraint("CK_Notifications_EventType", "EventType IN ('Submission', 'Approval', 'Rejection', 'ApprovedCancellation')");
            t.HasCheckConstraint("CK_Notifications_Status", "Status IN ('Queued', 'Sent', 'Failed')");
            t.HasCheckConstraint("CK_Notifications_Attempts", "Attempts >= 0 AND Attempts <= 4");
            t.HasCheckConstraint("CK_Notifications_Sent", "(Status = 'Sent' AND SentAtUtc IS NOT NULL) OR (Status <> 'Sent' AND SentAtUtc IS NULL)");
            t.HasCheckConstraint("CK_Notifications_Queued", "Status <> 'Queued' OR NextAttemptAtUtc IS NOT NULL");
        });
        b.HasKey(x => x.Id).HasName("PK_Notifications");
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.EventType).HasConversion<string>().HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(10).IsUnicode(false);
        b.Property(x => x.RecipientEmail).HasMaxLength(254).IsRequired();
        b.Property(x => x.Subject).HasMaxLength(200).IsRequired();
        b.Property(x => x.Body).IsRequired();
        b.Property(x => x.LastError).HasMaxLength(500);
        b.Property(x => x.Attempts).HasDefaultValue(0);
        b.Property(x => x.Version).IsConcurrencyToken().HasDefaultValue(0);
        b.HasIndex(x => x.NextAttemptAtUtc).HasFilter("Status = 'Queued'").HasDatabaseName("IX_Notifications_Due");
        b.HasIndex(x => new { x.Status, x.CreatedAtUtc }).HasDatabaseName("IX_Notifications_Status_Created");
        b.HasIndex(x => x.LeaveRequestId).HasDatabaseName("IX_Notifications_LeaveRequestId");
        b.HasOne<LeaveRequest>().WithMany().HasForeignKey(x => x.LeaveRequestId).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Notifications_LeaveRequests");
    }
}

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> b)
    {
        b.ToTable("AuditLog");
        b.HasKey(x => x.Id).HasName("PK_AuditLog");
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Action).HasMaxLength(40).IsUnicode(false).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(40).IsUnicode(false).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(64).IsRequired();
        b.Property(x => x.Details).IsRequired();
        b.HasIndex(x => x.OccurredAtUtc).HasDatabaseName("IX_AuditLog_OccurredAt");
        b.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("IX_AuditLog_Entity");
        b.HasOne<User>().WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_AuditLog_Author");
    }
}
