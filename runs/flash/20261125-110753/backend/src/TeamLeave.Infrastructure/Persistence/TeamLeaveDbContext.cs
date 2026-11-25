using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Interfaces;
using TeamLeave.Domain.Entities;

namespace TeamLeave.Infrastructure.Persistence;

public class TeamLeaveDbContext : DbContext, ITeamLeaveDbContext
{
    public TeamLeaveDbContext(DbContextOptions<TeamLeaveDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<EmailOutbox> EmailOutbox => Set<EmailOutbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.Email).HasMaxLength(256).IsRequired();
            b.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            b.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            b.Property(u => u.PasswordHash).IsRequired();
            b.HasOne(u => u.Team).WithMany(t => t.Members).HasForeignKey(u => u.TeamId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserRole>(b =>
        {
            b.HasKey(ur => new { ur.UserId, ur.Role });
            b.Property(ur => ur.Role).HasMaxLength(50).IsRequired();
            b.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Team>(b =>
        {
            b.HasKey(t => t.Id);
            b.Property(t => t.Name).HasMaxLength(200).IsRequired();
            b.HasOne(t => t.Manager).WithMany().HasForeignKey(t => t.ManagerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LeaveType>(b =>
        {
            b.HasKey(t => t.Id);
            b.HasIndex(t => t.Code).IsUnique();
            b.Property(t => t.Code).HasMaxLength(20).IsRequired();
            b.Property(t => t.Label).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<LeaveBalance>(b =>
        {
            b.HasKey(bl => bl.Id);
            b.HasIndex(bl => new { bl.UserId, bl.LeaveTypeId, bl.Year }).IsUnique();
            b.Property(bl => bl.AcquiredDays).HasColumnType("decimal(5,2)");
            b.Property(bl => bl.AdjustmentDays).HasColumnType("decimal(5,2)");
            b.Property(bl => bl.UsedDays).HasColumnType("decimal(5,2)");
            b.Property(bl => bl.Version).IsConcurrencyToken();
            b.HasOne(bl => bl.User).WithMany(u => u.LeaveBalances).HasForeignKey(bl => bl.UserId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(bl => bl.LeaveType).WithMany(t => t.LeaveBalances).HasForeignKey(bl => bl.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LeaveRequest>(b =>
        {
            b.HasKey(lr => lr.Id);
            b.Property(lr => lr.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.Property(lr => lr.Version).IsConcurrencyToken();
            b.HasOne(lr => lr.Employee).WithMany(u => u.LeaveRequests).HasForeignKey(lr => lr.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(lr => lr.Team).WithMany().HasForeignKey(lr => lr.TeamId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(lr => lr.LeaveType).WithMany(t => t.LeaveRequests).HasForeignKey(lr => lr.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(lr => lr.Decider).WithMany().HasForeignKey(lr => lr.DeciderId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(lr => lr.AcknowledgedOverlapByUser).WithMany().HasForeignKey(lr => lr.AcknowledgedOverlapBy).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(lr => new { lr.TeamId, lr.Status });
            b.HasIndex(lr => new { lr.EmployeeId, lr.Status });
            b.HasIndex(lr => new { lr.StartDate, lr.EndDate });
        });

        modelBuilder.Entity<Holiday>(b =>
        {
            b.HasKey(h => h.Id);
            b.HasIndex(h => h.Date).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.HasKey(a => a.Id);
            b.Property(a => a.Action).HasMaxLength(100).IsRequired();
            b.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
            b.HasOne(a => a.Actor).WithMany().HasForeignKey(a => a.ActorId).OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(a => new { a.EntityType, a.EntityId });
            b.HasIndex(a => a.OccurredAt);
        });

        modelBuilder.Entity<EmailOutbox>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Recipient).HasMaxLength(256).IsRequired();
            b.Property(e => e.Template).HasMaxLength(100).IsRequired();
            b.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            b.HasIndex(e => new { e.Status, e.CreatedAt });
        });
    }
}
