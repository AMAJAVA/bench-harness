using Microsoft.EntityFrameworkCore;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;

namespace TeamLeave.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(TeamLeaveDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (await db.Users.AnyAsync()) return;

        var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();

        var adminId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var devTeamId = Guid.NewGuid();
        var hrTeamId = Guid.NewGuid();

        var admin = new User
        {
            Id = adminId,
            Email = "admin@teamleave.local",
            FirstName = "Admin",
            LastName = "RH",
            PasswordHash = passwordHasher.HashPassword(new User(), "Admin123!"),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var manager = new User
        {
            Id = managerId,
            Email = "manager@teamleave.local",
            FirstName = "Marc",
            LastName = "Manager",
            PasswordHash = passwordHasher.HashPassword(new User(), "Manager123!"),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var employee = new User
        {
            Id = employeeId,
            Email = "employee@teamleave.local",
            FirstName = "Emma",
            LastName = "Employé",
            PasswordHash = passwordHasher.HashPassword(new User(), "Employee123!"),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Users.AddRange(admin, manager, employee);
        await db.SaveChangesAsync();

        var devTeam = new Team { Id = devTeamId, Name = "Équipe Développement", ManagerId = managerId, IsActive = true };
        var hrTeam = new Team { Id = hrTeamId, Name = "Équipe RH", ManagerId = adminId, IsActive = true };
        db.Teams.AddRange(devTeam, hrTeam);
        await db.SaveChangesAsync();

        admin.TeamId = hrTeamId;
        manager.TeamId = devTeamId;
        employee.TeamId = devTeamId;
        await db.SaveChangesAsync();

        var cp = new LeaveType { Id = Guid.NewGuid(), Code = "CP", Label = "Congés payés", IsActive = true, TrackBalance = true, DisplayOrder = 1, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        var rtt = new LeaveType { Id = Guid.NewGuid(), Code = "RTT", Label = "RTT", IsActive = true, TrackBalance = true, DisplayOrder = 2, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        var maladie = new LeaveType { Id = Guid.NewGuid(), Code = "MALADIE", Label = "Maladie", IsActive = true, TrackBalance = true, DisplayOrder = 3, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        var autre = new LeaveType { Id = Guid.NewGuid(), Code = "AUTRE", Label = "Autre", IsActive = true, TrackBalance = true, DisplayOrder = 4, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        db.LeaveTypes.AddRange(cp, rtt, maladie, autre);

        db.UserRoles.AddRange(
            new UserRole { UserId = adminId, Role = RoleNames.AdminRh },
            new UserRole { UserId = managerId, Role = RoleNames.Manager },
            new UserRole { UserId = managerId, Role = RoleNames.Employe },
            new UserRole { UserId = employeeId, Role = RoleNames.Employe }
        );

        var year = DateTimeOffset.UtcNow.Year;
        db.LeaveBalances.AddRange(
            new LeaveBalance { Id = Guid.NewGuid(), UserId = managerId, LeaveTypeId = cp.Id, Year = year, AcquiredDays = 25, AdjustmentDays = 0, UsedDays = 0, Version = 0 },
            new LeaveBalance { Id = Guid.NewGuid(), UserId = managerId, LeaveTypeId = rtt.Id, Year = year, AcquiredDays = 10, AdjustmentDays = 0, UsedDays = 0, Version = 0 },
            new LeaveBalance { Id = Guid.NewGuid(), UserId = employeeId, LeaveTypeId = cp.Id, Year = year, AcquiredDays = 25, AdjustmentDays = 0, UsedDays = 0, Version = 0 },
            new LeaveBalance { Id = Guid.NewGuid(), UserId = employeeId, LeaveTypeId = rtt.Id, Year = year, AcquiredDays = 10, AdjustmentDays = 0, UsedDays = 0, Version = 0 }
        );

        var holidays = new[]
        {
            new DateOnly(2025, 1, 1), new DateOnly(2025, 4, 21), new DateOnly(2025, 5, 1),
            new DateOnly(2025, 5, 8), new DateOnly(2025, 5, 29), new DateOnly(2025, 6, 9),
            new DateOnly(2025, 7, 14), new DateOnly(2025, 8, 15), new DateOnly(2025, 11, 1),
            new DateOnly(2025, 11, 11), new DateOnly(2025, 12, 25)
        };
        foreach (var date in holidays)
        {
            db.Holidays.Add(new Holiday
            {
                Id = Guid.NewGuid(),
                Date = date,
                Label = "Jour férié",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }
}
