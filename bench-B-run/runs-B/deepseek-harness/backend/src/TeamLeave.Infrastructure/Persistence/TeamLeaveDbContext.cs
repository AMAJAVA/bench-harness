using Microsoft.EntityFrameworkCore;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;

namespace TeamLeave.Infrastructure.Persistence;

public sealed class TeamLeaveDbContext(DbContextOptions<TeamLeaveDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<User> Users => Set<User>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveRequestAllocation> LeaveRequestAllocations => Set<LeaveRequestAllocation>();
    public DbSet<AnnualBalance> AnnualBalances => Set<AnnualBalance>();
    public DbSet<BalanceMovement> BalanceMovements => Set<BalanceMovement>();
    public DbSet<OverlapEvaluation> OverlapEvaluations => Set<OverlapEvaluation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.HasPostgresExtension("btree_gist");

        modelBuilder.Entity<Team>(entity =>
        {
            entity.ToTable("teams");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name").HasColumnType("citext").HasMaxLength(100).IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
            entity.HasIndex(x => x.Name).IsUnique().HasDatabaseName("uq_teams_name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Email).HasColumnName("email").HasColumnType("citext").HasMaxLength(320).IsRequired();
            entity.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(x => x.Roles).HasColumnName("roles").HasColumnType("text[]").IsRequired();
            entity.Property(x => x.TeamId).HasColumnName("team_id");
            entity.Property(x => x.ManagerId).HasColumnName("manager_id");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.MustChangePassword).HasColumnName("must_change_password");
            entity.Property(x => x.SecurityStamp).HasColumnName("security_stamp");
            entity.Property(x => x.FailedLoginCount).HasColumnName("failed_login_count");
            entity.Property(x => x.LockoutUntil).HasColumnName("lockout_until").HasColumnType("timestamptz");
            entity.Property(x => x.LastLoginAt).HasColumnName("last_login_at").HasColumnType("timestamptz");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
            entity.Ignore(x => x.FullName);
            entity.HasIndex(x => x.Email).IsUnique().HasDatabaseName("uq_users_email");
            entity.HasIndex(x => x.TeamId).HasDatabaseName("ix_users_team");
            entity.HasIndex(x => x.ManagerId).HasDatabaseName("ix_users_manager");
            entity.HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.ToTable("leave_types");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Code).HasColumnName("code").HasColumnType("citext").HasMaxLength(32).IsRequired();
            entity.Property(x => x.Label).HasColumnName("label").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Color).HasColumnName("color").HasColumnType("varchar(7)").IsRequired();
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.IsSystem).HasColumnName("is_system");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
            entity.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_leave_types_code");
        });

        modelBuilder.Entity<AnnualBalance>(entity =>
        {
            entity.ToTable("annual_balances");
            entity.HasKey(x => new { x.EmployeeId, x.LeaveTypeId, x.Year });
            entity.Property(x => x.EmployeeId).HasColumnName("employee_id");
            entity.Property(x => x.LeaveTypeId).HasColumnName("leave_type_id");
            entity.Property(x => x.Year).HasColumnName("year");
            entity.Property(x => x.GrantedHalfDays).HasColumnName("granted_half_days");
            entity.Property(x => x.ConsumedHalfDays).HasColumnName("consumed_half_days");
            entity.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
            entity.Ignore(x => x.AvailableHalfDays);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<LeaveType>().WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("ck_balances_available", "consumed_half_days <= granted_half_days"));
            entity.HasIndex(x => new { x.EmployeeId, x.Year }).HasDatabaseName("ix_balances_employee_year");
        });

        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.ToTable("leave_requests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EmployeeId).HasColumnName("employee_id");
            entity.Property(x => x.LeaveTypeId).HasColumnName("leave_type_id");
            entity.Property(x => x.StartDate).HasColumnName("start_date").HasColumnType("date");
            entity.Property(x => x.StartHalfDay).HasColumnName("start_half_day").HasColumnType("varchar(2)")
                .HasConversion(v => v.ToString(), v => Enum.Parse<HalfDay>(v, true));
            entity.Property(x => x.EndDate).HasColumnName("end_date").HasColumnType("date");
            entity.Property(x => x.EndHalfDay).HasColumnName("end_half_day").HasColumnType("varchar(2)")
                .HasConversion(v => v.ToString(), v => Enum.Parse<HalfDay>(v, true));
            entity.Property(x => x.TotalHalfDays).HasColumnName("total_half_days");
            entity.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(16)")
                .HasConversion(v => v.ToString(), v => Enum.Parse<LeaveStatus>(v, true));
            entity.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(1000);
            entity.Property(x => x.SubmittedAt).HasColumnName("submitted_at").HasColumnType("timestamptz");
            entity.Property(x => x.DecidedById).HasColumnName("decided_by_id");
            entity.Property(x => x.DecidedAt).HasColumnName("decided_at").HasColumnType("timestamptz");
            entity.Property(x => x.DecisionComment).HasColumnName("decision_comment").HasMaxLength(1000);
            entity.Property(x => x.OverlapFlagged).HasColumnName("overlap_flagged");
            entity.Property(x => x.OverlapEvaluatedAt).HasColumnName("overlap_evaluated_at").HasColumnType("timestamptz");
            entity.Property(x => x.AcknowledgedById).HasColumnName("acknowledged_by_id");
            entity.Property(x => x.AcknowledgedAt).HasColumnName("acknowledged_at").HasColumnType("timestamptz");
            entity.Property(x => x.CancelledById).HasColumnName("cancelled_by_id");
            entity.Property(x => x.CancelledAt).HasColumnName("cancelled_at").HasColumnType("timestamptz");
            entity.Property(x => x.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(64);
            entity.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
            entity.Ignore(x => x.TotalDays);
            entity.HasIndex(x => new { x.EmployeeId, x.SubmittedAt }).HasDatabaseName("ix_requests_employee_submitted");
            entity.HasIndex(x => new { x.EmployeeId, x.Status, x.SubmittedAt }).HasDatabaseName("ix_requests_employee_status");
            entity.HasIndex(x => x.Status).HasDatabaseName("ix_requests_status");
            entity.HasIndex(x => x.LeaveTypeId).HasDatabaseName("ix_requests_type");
            entity.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LeaveType).WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DecidedBy).WithMany().HasForeignKey(x => x.DecidedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AcknowledgedBy).WithMany().HasForeignKey(x => x.AcknowledgedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CancelledBy).WithMany().HasForeignKey(x => x.CancelledById).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Allocations).WithOne(x => x.LeaveRequest!)
                .HasForeignKey(x => x.LeaveRequestId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("ck_requests_total", "total_half_days > 0"));
        });

        modelBuilder.Entity<LeaveRequestAllocation>(entity =>
        {
            entity.ToTable("leave_request_allocations");
            entity.HasKey(x => new { x.LeaveRequestId, x.Year });
            entity.Property(x => x.LeaveRequestId).HasColumnName("leave_request_id");
            entity.Property(x => x.Year).HasColumnName("year");
            entity.Property(x => x.HalfDays).HasColumnName("half_days");
        });

        modelBuilder.Entity<BalanceMovement>(entity =>
        {
            entity.ToTable("balance_movements");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EmployeeId).HasColumnName("employee_id");
            entity.Property(x => x.LeaveTypeId).HasColumnName("leave_type_id");
            entity.Property(x => x.Year).HasColumnName("year");
            entity.Property(x => x.DeltaHalfDays).HasColumnName("delta_half_days");
            entity.Property(x => x.Nature).HasColumnName("nature").HasColumnType("varchar(32)")
                .HasConversion(v => v.ToString(), v => Enum.Parse<BalanceMovementNature>(v, true));
            entity.Property(x => x.LeaveRequestId).HasColumnName("leave_request_id");
            entity.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(1000);
            entity.Property(x => x.ActorId).HasColumnName("actor_id");
            entity.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamptz");
            entity.HasOne<AnnualBalance>().WithMany()
                .HasForeignKey(x => new { x.EmployeeId, x.LeaveTypeId, x.Year })
                .HasPrincipalKey(x => new { x.EmployeeId, x.LeaveTypeId, x.Year })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.EmployeeId, x.LeaveTypeId, x.Year, x.OccurredAt })
                .HasDatabaseName("ix_movements_balance");
        });

        modelBuilder.Entity<OverlapEvaluation>(entity =>
        {
            entity.ToTable("overlap_evaluations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(x => x.LeaveRequestId).HasColumnName("leave_request_id");
            entity.Property(x => x.TeamId).HasColumnName("team_id");
            entity.Property(x => x.TeamSize).HasColumnName("team_size");
            entity.Property(x => x.MaxRate).HasColumnName("max_rate").HasPrecision(5, 4);
            entity.Property(x => x.BreachingHalfDaysJson).HasColumnName("breaching_half_days").HasColumnType("jsonb");
            entity.Property(x => x.TriggerKind).HasColumnName("trigger_kind").HasColumnType("varchar(16)")
                .HasConversion(v => v.ToString(), v => Enum.Parse<OverlapTriggerKind>(v, true));
            entity.Property(x => x.EvaluatedById).HasColumnName("evaluated_by_id");
            entity.Property(x => x.EvaluatedAt).HasColumnName("evaluated_at").HasColumnType("timestamptz");
            entity.HasOne(x => x.LeaveRequest).WithMany().HasForeignKey(x => x.LeaveRequestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.LeaveRequestId, x.EvaluatedAt }).HasDatabaseName("ix_overlap_request");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EventKey).HasColumnName("event_key").HasMaxLength(100).IsRequired();
            entity.Property(x => x.Type).HasColumnName("type").HasColumnType("varchar(16)")
                .HasConversion(v => v.ToString(), v => Enum.Parse<NotificationType>(v, true));
            entity.Property(x => x.LeaveRequestId).HasColumnName("leave_request_id");
            entity.Property(x => x.RecipientUserId).HasColumnName("recipient_user_id");
            entity.Property(x => x.RecipientEmail).HasColumnName("recipient_email").HasColumnType("citext");
            entity.Property(x => x.Status).HasColumnName("status").HasColumnType("varchar(16)")
                .HasConversion(v => v.ToString(), v => Enum.Parse<NotificationStatus>(v, true));
            entity.Property(x => x.Attempts).HasColumnName("attempts");
            entity.Property(x => x.LastError).HasColumnName("last_error").HasMaxLength(1000);
            entity.Property(x => x.NextAttemptAt).HasColumnName("next_attempt_at").HasColumnType("timestamptz");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
            entity.Property(x => x.SentAt).HasColumnName("sent_at").HasColumnType("timestamptz");
            entity.HasIndex(x => x.EventKey).IsUnique().HasDatabaseName("uq_notifications_event");
            entity.HasIndex(x => new { x.Status, x.NextAttemptAt }).HasDatabaseName("ix_notifications_due");
            entity.HasOne(x => x.LeaveRequest).WithMany().HasForeignKey(x => x.LeaveRequestId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.TokenHash).HasColumnName("token_hash").HasColumnType("bytea").IsRequired();
            entity.Property(x => x.FamilyId).HasColumnName("family_id");
            entity.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
            entity.Property(x => x.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(64);
            entity.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(512);
            entity.Property(x => x.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");
            entity.Property(x => x.RevokedReason).HasColumnName("revoked_reason").HasMaxLength(32);
            entity.Property(x => x.ReplacedById).HasColumnName("replaced_by_id");
            entity.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("uq_refresh_tokens_hash");
            entity.HasIndex(x => new { x.UserId, x.RevokedAt }).HasDatabaseName("ix_refresh_user");
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("audit_entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(x => x.ActorId).HasColumnName("actor_id");
            entity.Property(x => x.ActorType).HasColumnName("actor_type").HasColumnType("varchar(8)")
                .HasConversion(v => v.ToString(), v => Enum.Parse<AuditActorType>(v, true));
            entity.Property(x => x.Action).HasColumnName("action").HasMaxLength(64).IsRequired();
            entity.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(32).IsRequired();
            entity.Property(x => x.EntityId).HasColumnName("entity_id");
            entity.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamptz");
            entity.Property(x => x.BeforeSummary).HasColumnName("before_summary").HasColumnType("jsonb");
            entity.Property(x => x.AfterSummary).HasColumnName("after_summary").HasColumnType("jsonb");
            entity.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(1000);
            entity.Property(x => x.CorrelationId).HasColumnName("correlation_id");
            entity.HasIndex(x => x.OccurredAt).HasDatabaseName("ix_audit_occurred");
            entity.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("ix_audit_entity");
        });
    }
}
