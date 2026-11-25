using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Exceptions;
using TeamLeave.Application.Services;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;
using TeamLeave.Infrastructure.Persistence;
using Xunit;

namespace TeamLeave.Tests.ApplicationTests;

public class LeaveRequestServiceTests
{
    private class FakeClock : TeamLeave.Application.Interfaces.IClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
    }

    private static async Task<(TeamLeaveDbContext db, User manager, User employee, LeaveRequest request)> SeedApprovalScenario(int balance = 10)
    {
        var options = new DbContextOptionsBuilder<TeamLeaveDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new TeamLeaveDbContext(options);

        var teamId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var manager = new User { Id = managerId, Email = "m@test.com", FirstName = "M", LastName = "Manager", PasswordHash = "x", IsActive = true, TeamId = teamId };
        var employee = new User { Id = employeeId, Email = "e@test.com", FirstName = "E", LastName = "Employee", PasswordHash = "x", IsActive = true, TeamId = teamId };
        var team = new Team { Id = teamId, Name = "Team", ManagerId = managerId, IsActive = true };
        var leaveType = new LeaveType { Id = Guid.NewGuid(), Code = "CP", Label = "CP", IsActive = true, TrackBalance = true, DisplayOrder = 1 };
        var balanceEntity = new LeaveBalance { Id = Guid.NewGuid(), UserId = employeeId, LeaveTypeId = leaveType.Id, Year = 2025, AcquiredDays = balance, AdjustmentDays = 0, UsedDays = 0, Version = 0 };

        var request = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TeamId = teamId,
            LeaveTypeId = leaveType.Id,
            StartDate = new DateOnly(2025, 7, 14),
            EndDate = new DateOnly(2025, 7, 18),
            WorkingDays = 5,
            Status = LeaveRequestStatus.Pending,
            SubmittedAt = DateTimeOffset.UtcNow,
            Version = 0,
            Employee = employee,
            Team = team,
            LeaveType = leaveType
        };

        db.Teams.Add(team);
        db.Users.AddRange(manager, employee);
        db.LeaveTypes.Add(leaveType);
        db.LeaveBalances.Add(balanceEntity);
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync();

        return (db, manager, employee, request);
    }

    [Fact]
    public async Task DecideAsync_WhenManagerIsEmployee_Throws403()
    {
        var (db, manager, employee, request) = await SeedApprovalScenario();
        request.EmployeeId = manager.Id;
        await db.SaveChangesAsync();

        var service = new LeaveRequestService(db, new FakeClock());
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.DecideAsync(request.Id, manager.Id, new[] { RoleNames.Manager }, new DecisionRequest("APPROVE", null, false)));
        ex.StatusCode.Should().Be(403);
        ex.Code.Should().Be("SELF_APPROVAL");
    }

    [Fact]
    public async Task DecideAsync_WhenNotManager_Throws403()
    {
        var (db, manager, employee, request) = await SeedApprovalScenario();
        var otherUser = Guid.NewGuid();
        var service = new LeaveRequestService(db, new FakeClock());
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.DecideAsync(request.Id, otherUser, new[] { RoleNames.Manager }, new DecisionRequest("APPROVE", null, false)));
        ex.StatusCode.Should().Be(403);
        ex.Code.Should().Be("NOT_TEAM_MANAGER");
    }

    [Fact]
    public async Task DecideAsync_RejectWithoutComment_Throws400()
    {
        var (db, manager, employee, request) = await SeedApprovalScenario();
        var service = new LeaveRequestService(db, new FakeClock());
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.DecideAsync(request.Id, manager.Id, new[] { RoleNames.Manager }, new DecisionRequest("REJECT", null, false)));
        ex.StatusCode.Should().Be(400);
        ex.Code.Should().Be("COMMENT_REQUIRED");
    }

    [Fact]
    public async Task DecideAsync_WithActiveOverlapWarningAndNoAck_Throws409()
    {
        var (db, manager, employee, request) = await SeedApprovalScenario();
        var otherEmployee = new User { Id = Guid.NewGuid(), Email = "o@test.com", FirstName = "O", LastName = "Other", PasswordHash = "x", IsActive = true, TeamId = request.TeamId };
        db.Users.Add(otherEmployee);
        db.LeaveRequests.Add(new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = otherEmployee.Id,
            TeamId = request.TeamId,
            LeaveTypeId = request.LeaveTypeId,
            StartDate = new DateOnly(2025, 7, 15),
            EndDate = new DateOnly(2025, 7, 17),
            WorkingDays = 3,
            Status = LeaveRequestStatus.Approved,
            SubmittedAt = DateTimeOffset.UtcNow,
            Version = 0,
            Employee = otherEmployee
        });
        await db.SaveChangesAsync();

        var service = new LeaveRequestService(db, new FakeClock());
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.DecideAsync(request.Id, manager.Id, new[] { RoleNames.Manager }, new DecisionRequest("APPROVE", null, false)));
        ex.StatusCode.Should().Be(409);
        ex.Code.Should().Be("OVERLAP_WARNING_NOT_ACKNOWLEDGED");
    }

    [Fact]
    public async Task DecideAsync_Approve_IncrementsUsedDaysAndSetsStatus()
    {
        var (db, manager, employee, request) = await SeedApprovalScenario(balance: 10);
        var service = new LeaveRequestService(db, new FakeClock());
        var result = await service.DecideAsync(request.Id, manager.Id, new[] { RoleNames.Manager }, new DecisionRequest("APPROVE", "OK", false));
        result.Status.Should().Be("APPROVED");
        var balance = await db.LeaveBalances.SingleAsync(b => b.UserId == employee.Id && b.LeaveTypeId == request.LeaveTypeId && b.Year == 2025);
        balance.UsedDays.Should().Be(5);
    }

    [Fact]
    public async Task DecideAsync_ApproveWithInsufficientBalance_Throws409()
    {
        var (db, manager, employee, request) = await SeedApprovalScenario(balance: 3);
        var service = new LeaveRequestService(db, new FakeClock());
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.DecideAsync(request.Id, manager.Id, new[] { RoleNames.Manager }, new DecisionRequest("APPROVE", null, false)));
        ex.StatusCode.Should().Be(409);
        ex.Code.Should().Be("INSUFFICIENT_BALANCE_AT_APPROVAL");
    }
}
