using Microsoft.EntityFrameworkCore;
using TeamLeave.Domain.Entities;

namespace TeamLeave.Application.Interfaces;

public interface ITeamLeaveDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Team> Teams { get; }
    DbSet<LeaveType> LeaveTypes { get; }
    DbSet<LeaveBalance> LeaveBalances { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<Holiday> Holidays { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<EmailOutbox> EmailOutbox { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
