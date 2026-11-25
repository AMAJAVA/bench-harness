=== FILE: TeamLeave.sln ===
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.8.34330.188
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TeamLeave.Domain", "src\TeamLeave.Domain\TeamLeave.Domain.csproj", "{11111111-1111-1111-1111-111111111111}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TeamLeave.Application", "src\TeamLeave.Application\TeamLeave.Application.csproj", "{22222222-2222-2222-2222-222222222222}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TeamLeave.Infrastructure", "src\TeamLeave.Infrastructure\TeamLeave.Infrastructure.csproj", "{33333333-3333-3333-3333-333333333333}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TeamLeave.Api", "src\TeamLeave.Api\TeamLeave.Api.csproj", "{44444444-4444-4444-4444-444444444444}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TeamLeave.Tests", "tests\TeamLeave.Tests\TeamLeave.Tests.csproj", "{55555555-5555-5555-5555-555555555555}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{11111111-1111-1111-1111-111111111111}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{11111111-1111-1111-1111-111111111111}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{11111111-1111-1111-1111-111111111111}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{11111111-1111-1111-1111-111111111111}.Release|Any CPU.Build.0 = Release|Any CPU
		{22222222-2222-2222-2222-222222222222}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{22222222-2222-2222-2222-222222222222}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{22222222-2222-2222-2222-222222222222}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{22222222-2222-2222-2222-222222222222}.Release|Any CPU.Build.0 = Release|Any CPU
		{33333333-3333-3333-3333-333333333333}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{33333333-3333-3333-3333-333333333333}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{33333333-3333-3333-3333-333333333333}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{33333333-3333-3333-3333-333333333333}.Release|Any CPU.Build.0 = Release|Any CPU
		{44444444-4444-4444-4444-444444444444}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{44444444-4444-4444-4444-444444444444}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{44444444-4444-4444-4444-444444444444}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{44444444-4444-4444-4444-444444444444}.Release|Any CPU.Build.0 = Release|Any CPU
		{55555555-5555-5555-5555-555555555555}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{55555555-5555-5555-5555-555555555555}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{55555555-5555-5555-5555-555555555555}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{55555555-5555-5555-5555-555555555555}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal

=== FILE: src/TeamLeave.Domain/TeamLeave.Domain.csproj ===
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>

=== FILE: src/TeamLeave.Application/TeamLeave.Application.csproj ===
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\TeamLeave.Domain\TeamLeave.Domain.csproj" />
  </ItemGroup>
</Project>

=== FILE: src/TeamLeave.Infrastructure/TeamLeave.Infrastructure.csproj ===
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Identity.Core" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="8.0.0" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.0" />
    <PackageReference Include="Microsoft.IdentityModel.Tokens" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\TeamLeave.Application\TeamLeave.Application.csproj" />
    <ProjectReference Include="..\TeamLeave.Domain\TeamLeave.Domain.csproj" />
  </ItemGroup>
</Project>

=== FILE: src/TeamLeave.Api/TeamLeave.Api.csproj ===
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\TeamLeave.Application\TeamLeave.Application.csproj" />
    <ProjectReference Include="..\TeamLeave.Infrastructure\TeamLeave.Infrastructure.csproj" />
  </ItemGroup>
</Project>

=== FILE: tests/TeamLeave.Tests/TeamLeave.Tests.csproj ===
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.6" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\TeamLeave.Domain\TeamLeave.Domain.csproj" />
    <ProjectReference Include="..\..\src\TeamLeave.Application\TeamLeave.Application.csproj" />
    <ProjectReference Include="..\..\src\TeamLeave.Infrastructure\TeamLeave.Infrastructure.csproj" />
  </ItemGroup>
</Project>

=== FILE: src/TeamLeave.Domain/Enums/LeaveRequestStatus.cs ===
namespace TeamLeave.Domain.Enums;

public enum LeaveRequestStatus
{
    Pending,
    Approved,
    Rejected
}

=== FILE: src/TeamLeave.Domain/Enums/EmailStatus.cs ===
namespace TeamLeave.Domain.Enums;

public enum EmailStatus
{
    Pending,
    Sent,
    Failed
}

=== FILE: src/TeamLeave.Domain/Enums/RoleNames.cs ===
namespace TeamLeave.Domain.Enums;

public static class RoleNames
{
    public const string Employe = "EMPLOYE";
    public const string Manager = "MANAGER";
    public const string AdminRh = "ADMIN_RH";
}

=== FILE: src/TeamLeave.Domain/Entities/User.cs ===
using TeamLeave.Domain.Enums;

namespace TeamLeave.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? TeamId { get; set; }
    public Team? Team { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}

=== FILE: src/TeamLeave.Domain/Entities/UserRole.cs ===
namespace TeamLeave.Domain.Entities;

public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Role { get; set; } = string.Empty;
}

=== FILE: src/TeamLeave.Domain/Entities/Team.cs ===
namespace TeamLeave.Domain.Entities;

public class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid ManagerId { get; set; }
    public User Manager { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public ICollection<User> Members { get; set; } = new List<User>();
}

=== FILE: src/TeamLeave.Domain/Entities/LeaveType.cs ===
namespace TeamLeave.Domain.Entities;

public class LeaveType
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool TrackBalance { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}

=== FILE: src/TeamLeave.Domain/Entities/LeaveBalance.cs ===
using System.ComponentModel.DataAnnotations;

namespace TeamLeave.Domain.Entities;

public class LeaveBalance
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;
    public int Year { get; set; }
    public decimal AcquiredDays { get; set; }
    public decimal AdjustmentDays { get; set; }
    public decimal UsedDays { get; set; }
    [ConcurrencyCheck]
    public int Version { get; set; }
    public decimal AvailableDays => AcquiredDays + AdjustmentDays - UsedDays;
}

=== FILE: src/TeamLeave.Domain/Entities/LeaveRequest.cs ===
using System.ComponentModel.DataAnnotations;
using TeamLeave.Domain.Enums;

namespace TeamLeave.Domain.Entities;

public class LeaveRequest
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public User Employee { get; set; } = null!;
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int WorkingDays { get; set; }
    public LeaveRequestStatus Status { get; set; }
    public string? EmployeeComment { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public Guid? DeciderId { get; set; }
    public User? Decider { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? ManagerComment { get; set; }
    public bool HasOverlapWarning { get; set; }
    public decimal? OverlapRatio { get; set; }
    public Guid? AcknowledgedOverlapBy { get; set; }
    public User? AcknowledgedOverlapByUser { get; set; }
    public DateTimeOffset? AcknowledgedOverlapAt { get; set; }
    [ConcurrencyCheck]
    public int Version { get; set; }
}

=== FILE: src/TeamLeave.Domain/Entities/Holiday.cs ===
namespace TeamLeave.Domain.Entities;

public class Holiday
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

=== FILE: src/TeamLeave.Domain/Entities/AuditLog.cs ===
namespace TeamLeave.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public Guid? ActorId { get; set; }
    public User? Actor { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? Details { get; set; }
    public string? Reason { get; set; }
}

=== FILE: src/TeamLeave.Domain/Entities/EmailOutbox.cs ===
using TeamLeave.Domain.Enums;

namespace TeamLeave.Domain.Entities;

public class EmailOutbox
{
    public Guid Id { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public EmailStatus Status { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public string? ErrorMessage { get; set; }
}

=== FILE: src/TeamLeave.Domain/Services/WorkingDaysCalculator.cs ===
using TeamLeave.Domain.Entities;

namespace TeamLeave.Domain.Services;

public static class WorkingDaysCalculator
{
    public static int Calculate(DateOnly start, DateOnly end, IEnumerable<Holiday> holidays)
    {
        if (start > end) return 0;
        var holidaySet = holidays.Where(h => h.IsActive).Select(h => h.Date).ToHashSet();
        var count = 0;
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (holidaySet.Contains(date)) continue;
            count++;
        }
        return count;
    }

    public static IEnumerable<DateOnly> GetWorkingDays(DateOnly start, DateOnly end, IEnumerable<Holiday> holidays)
    {
        if (start > end) yield break;
        var holidaySet = holidays.Where(h => h.IsActive).Select(h => h.Date).ToHashSet();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            if (holidaySet.Contains(date)) continue;
            yield return date;
        }
    }
}

=== FILE: src/TeamLeave.Domain/Services/OverlapCalculator.cs ===
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;

namespace TeamLeave.Domain.Services;

public record OverlapResult(int WorkingDays, int OverlapDays, decimal Ratio, bool HasWarning, IReadOnlyList<(DateOnly Date, List<string> AbsentEmployees)> Details);

public static class OverlapCalculator
{
    public static OverlapResult Calculate(
        DateOnly startDate,
        DateOnly endDate,
        Guid employeeId,
        Guid teamId,
        IEnumerable<LeaveRequest> approvedRequests,
        IEnumerable<Holiday> holidays)
    {
        var workingDays = WorkingDaysCalculator.GetWorkingDays(startDate, endDate, holidays).ToList();
        if (workingDays.Count == 0)
        {
            return new OverlapResult(0, 0, 0, false, new List<(DateOnly, List<string>)>());
        }

        var relevantApproved = approvedRequests
            .Where(r => r.Status == LeaveRequestStatus.Approved
                        && r.TeamId == teamId
                        && r.EmployeeId != employeeId)
            .ToList();

        var details = new List<(DateOnly Date, List<string> AbsentEmployees)>();
        var overlapDays = 0;

        foreach (var day in workingDays)
        {
            var absentEmployees = relevantApproved
                .Where(r => r.StartDate <= day && r.EndDate >= day)
                .Select(r => $"{r.Employee.FirstName} {r.Employee.LastName}")
                .Distinct()
                .ToList();

            if (absentEmployees.Count > 0)
            {
                overlapDays++;
                details.Add((day, absentEmployees));
            }
        }

        var ratio = (decimal)overlapDays / workingDays.Count;
        var hasWarning = 2 * overlapDays > workingDays.Count;
        return new OverlapResult(workingDays.Count, overlapDays, ratio, hasWarning, details);
    }
}

=== FILE: src/TeamLeave.Domain/Services/BalanceCalculator.cs ===
namespace TeamLeave.Domain.Services;

public static class BalanceCalculator
{
    public static decimal Available(decimal acquired, decimal adjustment, decimal used)
        => acquired + adjustment - used;

    public static bool IsSufficient(decimal acquired, decimal adjustment, decimal used, int requestedDays)
        => Available(acquired, adjustment, used) >= requestedDays;

    public static bool WouldBeNegative(decimal acquired, decimal adjustment, decimal used)
        => Available(acquired, adjustment, used) < 0;
}

=== FILE: src/TeamLeave.Application/Interfaces/ITeamLeaveDbContext.cs ===
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

=== FILE: src/TeamLeave.Application/Interfaces/IPasswordHasher.cs ===
namespace TeamLeave.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

=== FILE: src/TeamLeave.Application/Interfaces/IJwtTokenGenerator.cs ===
using TeamLeave.Domain.Entities;

namespace TeamLeave.Application.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTimeOffset ExpiresAt) Generate(User user, IEnumerable<string> roles);
}

=== FILE: src/TeamLeave.Application/Interfaces/IClock.cs ===
namespace TeamLeave.Application.Interfaces;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

=== FILE: src/TeamLeave.Application/Interfaces/IEmailSender.cs ===
namespace TeamLeave.Application.Interfaces;

public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken);
}

=== FILE: src/TeamLeave.Application/Exceptions/AppException.cs ===
namespace TeamLeave.Application.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }
    public object? Errors { get; }

    public AppException(int statusCode, string code, string message, object? errors = null) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Errors = errors;
    }
}

=== FILE: src/TeamLeave.Application/DTOs/AuthDtos.cs ===
namespace TeamLeave.Application.DTOs;

public record LoginRequest(string Email, string Password);
public record UserInfoDto(Guid Id, string Email, string FirstName, string LastName, IReadOnlyList<string> Roles, Guid? TeamId);
public record AuthResponse(string AccessToken, string TokenType, int ExpiresIn, UserInfoDto User);

=== FILE: src/TeamLeave.Application/DTOs/MeDto.cs ===
namespace TeamLeave.Application.DTOs;

public record TeamDto(Guid Id, string Name);
public record MeDto(Guid Id, string Email, string FirstName, string LastName, IReadOnlyList<string> Roles, TeamDto? Team);

=== FILE: src/TeamLeave.Application/DTOs/BalanceDtos.cs ===
namespace TeamLeave.Application.DTOs;

public record BalanceDto(Guid LeaveTypeId, string LeaveTypeCode, string LeaveTypeLabel, int Year, decimal AcquiredDays, decimal AdjustmentDays, decimal UsedDays, decimal AvailableDays);
public record AdjustmentRequest(int Year, decimal AdjustmentDays, string Reason);
public record SetBalanceRequest(decimal AcquiredDays, decimal AdjustmentDays);

=== FILE: src/TeamLeave.Application/DTOs/LeaveRequestDtos.cs ===
namespace TeamLeave.Application.DTOs;

public record LeaveTypeInfoDto(Guid Id, string Code, string Label);
public record OverlapDetailDto(DateOnly Date, IReadOnlyList<string> AbsentEmployees);
public record LeaveRequestDto(
    Guid Id,
    Guid EmployeeId,
    Guid TeamId,
    LeaveTypeInfoDto LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    int WorkingDays,
    string Status,
    string? EmployeeComment,
    DateTimeOffset SubmittedAt,
    Guid? DeciderId,
    DateTimeOffset? DecidedAt,
    string? ManagerComment,
    bool HasOverlapWarning,
    decimal? OverlapRatio,
    IReadOnlyList<OverlapDetailDto> OverlapDetails,
    Guid? AcknowledgedOverlapBy,
    DateTimeOffset? AcknowledgedOverlapAt);
public record CreateLeaveRequestRequest(Guid LeaveTypeId, DateOnly StartDate, DateOnly EndDate, string? Comment);
public record DecisionRequest(string Action, string? Comment, bool AcknowledgeOverlapWarning);

=== FILE: src/TeamLeave.Application/DTOs/LeaveTypeDtos.cs ===
namespace TeamLeave.Application.DTOs;

public record LeaveTypeDto(Guid Id, string Code, string Label, bool IsActive, bool TrackBalance, int DisplayOrder);
public record CreateLeaveTypeRequest(string Code, string Label, bool IsActive, bool TrackBalance, int DisplayOrder);
public record UpdateLeaveTypeRequest(string Label, bool IsActive, bool TrackBalance, int DisplayOrder);

=== FILE: src/TeamLeave.Application/DTOs/HolidayDtos.cs ===
namespace TeamLeave.Application.DTOs;

public record HolidayDto(Guid Id, DateOnly Date, string Label, bool IsActive);
public record CreateHolidayRequest(DateOnly Date, string Label);
public record UpdateHolidayRequest(string Label, bool IsActive);

=== FILE: src/TeamLeave.Application/DTOs/AuditDtos.cs ===
namespace TeamLeave.Application.DTOs;

public record AuditLogDto(long Id, Guid? ActorId, string Action, string EntityType, Guid? EntityId, DateTimeOffset OccurredAt, string? Details, string? Reason);
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

=== FILE: src/TeamLeave.Application/Services/AuthService.cs ===
using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Exceptions;
using TeamLeave.Application.Interfaces;

namespace TeamLeave.Application.Services;

public class AuthService
{
    private readonly ITeamLeaveDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(ITeamLeaveDbContext db, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AppException(401, "INVALID_CREDENTIALS", "Email ou mot de passe invalide.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role).ToList();
        var (token, expiresAt) = _jwtTokenGenerator.Generate(user, roles);
        var expiresIn = (int)(expiresAt - DateTimeOffset.UtcNow).TotalSeconds;

        return new AuthResponse(
            token,
            "Bearer",
            expiresIn,
            new UserInfoDto(user.Id, user.Email, user.FirstName, user.LastName, roles, user.TeamId));
    }

    public async Task<MeDto> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .Include(u => u.Team)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null) throw new AppException(404, "USER_NOT_FOUND", "Utilisateur introuvable.");

        return new MeDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.UserRoles.Select(ur => ur.Role).ToList(),
            user.Team == null ? null : new TeamDto(user.Team.Id, user.Team.Name));
    }
}

=== FILE: src/TeamLeave.Application/Services/BalanceService.cs ===
using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Exceptions;
using TeamLeave.Application.Interfaces;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Services;

namespace TeamLeave.Application.Services;

public class BalanceService
{
    private readonly ITeamLeaveDbContext _db;
    private readonly IClock _clock;

    public BalanceService(ITeamLeaveDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<BalanceDto>> GetBalancesAsync(Guid userId, int year, CancellationToken cancellationToken = default)
    {
        var userExists = await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists) throw new AppException(404, "USER_NOT_FOUND", "Utilisateur introuvable.");

        var leaveTypes = await _db.LeaveTypes
            .Where(t => t.IsActive && t.TrackBalance)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync(cancellationToken);

        var balances = await _db.LeaveBalances
            .Where(b => b.UserId == userId && b.Year == year)
            .ToDictionaryAsync(b => b.LeaveTypeId, cancellationToken);

        var result = new List<BalanceDto>();
        foreach (var type in leaveTypes)
        {
            if (balances.TryGetValue(type.Id, out var balance))
            {
                result.Add(new BalanceDto(
                    type.Id,
                    type.Code,
                    type.Label,
                    year,
                    balance.AcquiredDays,
                    balance.AdjustmentDays,
                    balance.UsedDays,
                    balance.AvailableDays));
            }
            else
            {
                result.Add(new BalanceDto(type.Id, type.Code, type.Label, year, 0, 0, 0, 0));
            }
        }

        return result;
    }

    public async Task AdjustBalanceAsync(Guid userId, Guid leaveTypeId, AdjustmentRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        _ = await _db.Users.FindAsync(new object?[] { userId }, cancellationToken)
            ?? throw new AppException(404, "USER_NOT_FOUND", "Utilisateur introuvable.");
        var leaveType = await _db.LeaveTypes.FindAsync(new object?[] { leaveTypeId }, cancellationToken)
            ?? throw new AppException(404, "LEAVE_TYPE_NOT_FOUND", "Type de congé introuvable.");
        if (!leaveType.IsActive) throw new AppException(422, "LEAVE_TYPE_INACTIVE", "Le type de congé est inactif.");
        if (!leaveType.TrackBalance) throw new AppException(422, "LEAVE_TYPE_NOT_TRACKED", "Ce type de congé ne suit pas de solde.");

        var balance = await _db.LeaveBalances
            .SingleOrDefaultAsync(b => b.UserId == userId && b.LeaveTypeId == leaveTypeId && b.Year == request.Year, cancellationToken);

        decimal before = 0;
        if (balance == null)
        {
            balance = new LeaveBalance
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LeaveTypeId = leaveTypeId,
                Year = request.Year,
                AcquiredDays = 0,
                AdjustmentDays = 0,
                UsedDays = 0,
                Version = 0
            };
            _db.LeaveBalances.Add(balance);
        }
        else
        {
            before = balance.AvailableDays;
        }

        var newAdjustment = balance.AdjustmentDays + request.AdjustmentDays;
        var available = BalanceCalculator.Available(balance.AcquiredDays, newAdjustment, balance.UsedDays);
        if (available < 0)
        {
            throw new AppException(409, "BALANCE_NEGATIVE", $"L'ajustement rendrait le solde négatif (disponible actuel: {before}, après: {available}).");
        }

        balance.AdjustmentDays = newAdjustment;
        balance.Version++;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = "BALANCE_ADJUSTMENT",
            EntityType = nameof(LeaveBalance),
            EntityId = balance.Id,
            OccurredAt = _clock.UtcNow,
            Details = $"UserId={userId};LeaveTypeId={leaveTypeId};Year={request.Year};Adjustment={request.AdjustmentDays};Before={before};After={available}",
            Reason = request.Reason
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetBalanceAsync(Guid userId, Guid leaveTypeId, int year, SetBalanceRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        _ = await _db.Users.FindAsync(new object?[] { userId }, cancellationToken)
            ?? throw new AppException(404, "USER_NOT_FOUND", "Utilisateur introuvable.");
        var leaveType = await _db.LeaveTypes.FindAsync(new object?[] { leaveTypeId }, cancellationToken)
            ?? throw new AppException(404, "LEAVE_TYPE_NOT_FOUND", "Type de congé introuvable.");
        if (!leaveType.IsActive) throw new AppException(422, "LEAVE_TYPE_INACTIVE", "Le type de congé est inactif.");
        if (!leaveType.TrackBalance) throw new AppException(422, "LEAVE_TYPE_NOT_TRACKED", "Ce type de congé ne suit pas de solde.");

        var balance = await _db.LeaveBalances
            .SingleOrDefaultAsync(b => b.UserId == userId && b.LeaveTypeId == leaveTypeId && b.Year == year, cancellationToken);

        decimal before = 0;
        if (balance == null)
        {
            balance = new LeaveBalance
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LeaveTypeId = leaveTypeId,
                Year = year,
                AcquiredDays = 0,
                AdjustmentDays = 0,
                UsedDays = 0,
                Version = 0
            };
            _db.LeaveBalances.Add(balance);
        }
        else
        {
            before = balance.AvailableDays;
        }

        var available = BalanceCalculator.Available(request.AcquiredDays, request.AdjustmentDays, balance.UsedDays);
        if (available < 0)
        {
            throw new AppException(409, "BALANCE_NEGATIVE", $"Le solde résultant serait négatif (disponible actuel: {before}, après: {available}).");
        }

        balance.AcquiredDays = request.AcquiredDays;
        balance.AdjustmentDays = request.AdjustmentDays;
        balance.Version++;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = "BALANCE_SET",
            EntityType = nameof(LeaveBalance),
            EntityId = balance.Id,
            OccurredAt = _clock.UtcNow,
            Details = $"UserId={userId};LeaveTypeId={leaveTypeId};Year={year};Acquired={request.AcquiredDays};Adjustment={request.AdjustmentDays};Before={before};After={available}"
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}

=== FILE: src/TeamLeave.Application/Services/LeaveTypeService.cs ===
using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Exceptions;
using TeamLeave.Application.Interfaces;
using TeamLeave.Domain.Entities;

namespace TeamLeave.Application.Services;

public class LeaveTypeService
{
    private readonly ITeamLeaveDbContext _db;
    private readonly IClock _clock;

    public LeaveTypeService(ITeamLeaveDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<LeaveTypeDto>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = _db.LeaveTypes.AsQueryable();
        if (!includeInactive) query = query.Where(t => t.IsActive);
        var types = await query.OrderBy(t => t.DisplayOrder).ToListAsync(cancellationToken);
        return types.Select(t => new LeaveTypeDto(t.Id, t.Code, t.Label, t.IsActive, t.TrackBalance, t.DisplayOrder)).ToList();
    }

    public async Task<LeaveTypeDto> CreateAsync(CreateLeaveTypeRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.LeaveTypes.AnyAsync(t => t.Code == code, cancellationToken))
            throw new AppException(409, "LEAVE_TYPE_CODE_EXISTS", $"Le code '{code}' existe déjà.");

        var type = new LeaveType
        {
            Id = Guid.NewGuid(),
            Code = code,
            Label = request.Label,
            IsActive = request.IsActive,
            TrackBalance = request.TrackBalance,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow
        };
        _db.LeaveTypes.Add(type);
        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = "LEAVE_TYPE_CREATED",
            EntityType = nameof(LeaveType),
            EntityId = type.Id,
            OccurredAt = _clock.UtcNow,
            Details = $"Code={type.Code};Label={type.Label};IsActive={type.IsActive};TrackBalance={type.TrackBalance}"
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new LeaveTypeDto(type.Id, type.Code, type.Label, type.IsActive, type.TrackBalance, type.DisplayOrder);
    }

    public async Task<LeaveTypeDto> UpdateAsync(Guid id, UpdateLeaveTypeRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var type = await _db.LeaveTypes.FindAsync(new object?[] { id }, cancellationToken)
            ?? throw new AppException(404, "LEAVE_TYPE_NOT_FOUND", "Type de congé introuvable.");

        var before = $"Label={type.Label};IsActive={type.IsActive};TrackBalance={type.TrackBalance};DisplayOrder={type.DisplayOrder}";
        type.Label = request.Label;
        type.IsActive = request.IsActive;
        type.TrackBalance = request.TrackBalance;
        type.DisplayOrder = request.DisplayOrder;
        type.UpdatedAt = _clock.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = "LEAVE_TYPE_UPDATED",
            EntityType = nameof(LeaveType),
            EntityId = type.Id,
            OccurredAt = _clock.UtcNow,
            Details = $"Before: {before}; After: Label={type.Label};IsActive={type.IsActive};TrackBalance={type.TrackBalance};DisplayOrder={type.DisplayOrder}"
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new LeaveTypeDto(type.Id, type.Code, type.Label, type.IsActive, type.TrackBalance, type.DisplayOrder);
    }
}

=== FILE: src/TeamLeave.Application/Services/HolidayService.cs ===
using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Exceptions;
using TeamLeave.Application.Interfaces;
using TeamLeave.Domain.Entities;

namespace TeamLeave.Application.Services;

public class HolidayService
{
    private readonly ITeamLeaveDbContext _db;
    private readonly IClock _clock;

    public HolidayService(ITeamLeaveDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<HolidayDto>> GetAsync(int year, CancellationToken cancellationToken = default)
    {
        var start = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);
        var holidays = await _db.Holidays
            .Where(h => h.Date >= start && h.Date <= end)
            .OrderBy(h => h.Date)
            .ToListAsync(cancellationToken);
        return holidays.Select(h => new HolidayDto(h.Id, h.Date, h.Label, h.IsActive)).ToList();
    }

    public async Task<HolidayDto> CreateAsync(CreateHolidayRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (await _db.Holidays.AnyAsync(h => h.Date == request.Date, cancellationToken))
            throw new AppException(409, "HOLIDAY_DATE_EXISTS", $"Un jour férié existe déjà pour le {request.Date}.");

        var holiday = new Holiday
        {
            Id = Guid.NewGuid(),
            Date = request.Date,
            Label = request.Label,
            IsActive = true,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow
        };
        _db.Holidays.Add(holiday);
        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = "HOLIDAY_CREATED",
            EntityType = nameof(Holiday),
            EntityId = holiday.Id,
            OccurredAt = _clock.UtcNow,
            Details = $"Date={holiday.Date};Label={holiday.Label}"
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new HolidayDto(holiday.Id, holiday.Date, holiday.Label, holiday.IsActive);
    }

    public async Task<HolidayDto> UpdateAsync(Guid id, UpdateHolidayRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var holiday = await _db.Holidays.FindAsync(new object?[] { id }, cancellationToken)
            ?? throw new AppException(404, "HOLIDAY_NOT_FOUND", "Jour férié introuvable.");

        var before = $"Label={holiday.Label};IsActive={holiday.IsActive}";
        holiday.Label = request.Label;
        holiday.IsActive = request.IsActive;
        holiday.UpdatedAt = _clock.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = "HOLIDAY_UPDATED",
            EntityType = nameof(Holiday),
            EntityId = holiday.Id,
            OccurredAt = _clock.UtcNow,
            Details = $"Before: {before}; After: Label={holiday.Label};IsActive={holiday.IsActive}"
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new HolidayDto(holiday.Id, holiday.Date, holiday.Label, holiday.IsActive);
    }
}

=== FILE: src/TeamLeave.Application/Services/AuditService.cs ===
using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Interfaces;

namespace TeamLeave.Application.Services;

public class AuditService
{
    private readonly ITeamLeaveDbContext _db;

    public AuditService(ITeamLeaveDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditLogDto>> GetAsync(
        string? entityType,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);
        if (from.HasValue)
            query = query.Where(a => a.OccurredAt >= from.Value);
        if (to.HasValue)
            query = query.Where(a => a.OccurredAt <= to.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(a.Id, a.ActorId, a.Action, a.EntityType, a.EntityId, a.OccurredAt, a.Details, a.Reason))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>(items, page, pageSize, total);
    }
}

=== FILE: src/TeamLeave.Application/Services/LeaveRequestService.cs ===
using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Exceptions;
using TeamLeave.Application.Interfaces;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;
using TeamLeave.Domain.Services;

namespace TeamLeave.Application.Services;

public class LeaveRequestService
{
    private readonly ITeamLeaveDbContext _db;
    private readonly IClock _clock;

    public LeaveRequestService(ITeamLeaveDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<LeaveRequestDto> SubmitAsync(Guid userId, CreateLeaveRequestRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Include(u => u.Team)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new AppException(404, "USER_NOT_FOUND", "Utilisateur introuvable.");

        if (!user.IsActive) throw new AppException(422, "USER_INACTIVE", "L'utilisateur est inactif.");
        if (user.TeamId == null) throw new AppException(422, "NO_TEAM", "L'utilisateur n'est rattaché à aucune équipe.");
        if (user.Team == null || user.Team.ManagerId == Guid.Empty) throw new AppException(422, "NO_TEAM_MANAGER", "L'équipe n'a pas de manager.");

        var leaveType = await _db.LeaveTypes.FindAsync(new object?[] { request.LeaveTypeId }, cancellationToken)
            ?? throw new AppException(400, "LEAVE_TYPE_NOT_FOUND", "Type de congé introuvable.");
        if (!leaveType.IsActive) throw new AppException(400, "LEAVE_TYPE_INACTIVE", "Le type de congé est inactif.");

        if (request.StartDate > request.EndDate)
            throw new AppException(400, "INVALID_DATES", "La date de début doit être antérieure ou égale à la date de fin.");
        if (request.StartDate.Year != request.EndDate.Year)
            throw new AppException(400, "CROSS_YEAR", "Les demandes à cheval sur deux années civiles sont interdites.");

        var holidays = await _db.Holidays.Where(h => h.IsActive).ToListAsync(cancellationToken);
        var workingDays = WorkingDaysCalculator.Calculate(request.StartDate, request.EndDate, holidays);
        if (workingDays == 0)
            throw new AppException(422, "NO_WORKING_DAYS", "La demande ne contient aucun jour ouvré.");

        var year = request.StartDate.Year;
        var balance = await _db.LeaveBalances
            .SingleOrDefaultAsync(b => b.UserId == userId && b.LeaveTypeId == leaveType.Id && b.Year == year, cancellationToken);

        decimal acquired = balance?.AcquiredDays ?? 0;
        decimal adjustment = balance?.AdjustmentDays ?? 0;
        decimal used = balance?.UsedDays ?? 0;

        if (!BalanceCalculator.IsSufficient(acquired, adjustment, used, workingDays))
        {
            var available = BalanceCalculator.Available(acquired, adjustment, used);
            throw new AppException(422, "INSUFFICIENT_BALANCE", $"La durée demandée ({workingDays} jours) dépasse le solde disponible ({available} jours).");
        }

        var approvedRequests = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == LeaveRequestStatus.Approved && r.TeamId == user.TeamId)
            .ToListAsync(cancellationToken);

        var overlap = OverlapCalculator.Calculate(
            request.StartDate,
            request.EndDate,
            userId,
            user.TeamId!.Value,
            approvedRequests,
            holidays);

        var leaveRequest = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = userId,
            TeamId = user.TeamId.Value,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            WorkingDays = workingDays,
            Status = LeaveRequestStatus.Pending,
            EmployeeComment = request.Comment,
            SubmittedAt = _clock.UtcNow,
            HasOverlapWarning = overlap.HasWarning,
            OverlapRatio = overlap.Ratio,
            Version = 0
        };

        _db.LeaveRequests.Add(leaveRequest);

        var manager = await _db.Users.FindAsync(new object?[] { user.Team.ManagerId }, cancellationToken);
        if (manager != null)
        {
            _db.EmailOutbox.Add(new EmailOutbox
            {
                Id = Guid.NewGuid(),
                Recipient = manager.Email,
                Template = "LeaveRequestSubmitted",
                Payload = $"Employee={user.FirstName} {user.LastName};Type={leaveType.Code};Start={request.StartDate};End={request.EndDate};HasOverlapWarning={overlap.HasWarning};OverlapRatio={overlap.Ratio}",
                Status = EmailStatus.Pending,
                Attempts = 0,
                CreatedAt = _clock.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(leaveRequest, approvedRequests, holidays);
    }

    public async Task<LeaveRequestDto> GetByIdAsync(Guid id, Guid currentUserId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default)
    {
        var request = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.Team)
            .Include(r => r.LeaveType)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new AppException(404, "LEAVE_REQUEST_NOT_FOUND", "Demande introuvable.");

        var isAdmin = roles.Contains(RoleNames.AdminRh);
        var isOwner = request.EmployeeId == currentUserId;
        var isManagerOfTeam = roles.Contains(RoleNames.Manager) && request.Team.ManagerId == currentUserId;

        if (!isAdmin && !isOwner && !isManagerOfTeam)
            throw new AppException(403, "FORBIDDEN", "Vous n'avez pas accès à cette demande.");

        var holidays = await _db.Holidays.Where(h => h.IsActive).ToListAsync(cancellationToken);
        var approvedRequests = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == LeaveRequestStatus.Approved && r.TeamId == request.TeamId)
            .ToListAsync(cancellationToken);

        return MapToDto(request, approvedRequests, holidays);
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetMyRequestsAsync(
        Guid userId,
        LeaveRequestStatus? status,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var query = _db.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.Team)
            .Include(r => r.LeaveType)
            .Where(r => r.EmployeeId == userId);

        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (from.HasValue) query = query.Where(r => r.EndDate >= from.Value);
        if (to.HasValue) query = query.Where(r => r.StartDate <= to.Value);

        var requests = await query.OrderByDescending(r => r.SubmittedAt).ToListAsync(cancellationToken);
        var holidays = await _db.Holidays.Where(h => h.IsActive).ToListAsync(cancellationToken);
        var approvedRequests = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == LeaveRequestStatus.Approved)
            .ToListAsync(cancellationToken);

        return requests
            .Select(r => MapToDto(r, approvedRequests.Where(ar => ar.TeamId == r.TeamId).ToList(), holidays))
            .ToList();
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetTeamPendingRequestsAsync(Guid teamId, Guid managerId, CancellationToken cancellationToken = default)
    {
        var team = await _db.Teams.FindAsync(new object?[] { teamId }, cancellationToken)
            ?? throw new AppException(404, "TEAM_NOT_FOUND", "Équipe introuvable.");
        if (team.ManagerId != managerId)
            throw new AppException(403, "NOT_TEAM_MANAGER", "Vous n'êtes pas le manager de cette équipe.");

        var requests = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.Team)
            .Include(r => r.LeaveType)
            .Where(r => r.TeamId == teamId && r.Status == LeaveRequestStatus.Pending)
            .OrderBy(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);

        var holidays = await _db.Holidays.Where(h => h.IsActive).ToListAsync(cancellationToken);
        var approvedRequests = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == LeaveRequestStatus.Approved && r.TeamId == teamId)
            .ToListAsync(cancellationToken);

        return requests.Select(r => MapToDto(r, approvedRequests, holidays)).ToList();
    }

    public async Task<IReadOnlyList<LeaveRequestDto>> GetTeamCalendarAsync(
        Guid teamId,
        Guid currentUserId,
        IReadOnlyList<string> roles,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        var team = await _db.Teams.FindAsync(new object?[] { teamId }, cancellationToken)
            ?? throw new AppException(404, "TEAM_NOT_FOUND", "Équipe introuvable.");

        var isAdmin = roles.Contains(RoleNames.AdminRh);
        var isManager = roles.Contains(RoleNames.Manager) && team.ManagerId == currentUserId;
        var isMember = await _db.Users.AnyAsync(u => u.Id == currentUserId && u.TeamId == teamId, cancellationToken);

        if (!isAdmin && !isManager && !isMember)
            throw new AppException(403, "FORBIDDEN", "Vous n'avez pas accès au calendrier de cette équipe.");

        var requests = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.Team)
            .Include(r => r.LeaveType)
            .Where(r => r.TeamId == teamId
                        && r.Status == LeaveRequestStatus.Approved
                        && r.StartDate <= to
                        && r.EndDate >= from)
            .OrderBy(r => r.StartDate)
            .ToListAsync(cancellationToken);

        var holidays = await _db.Holidays.Where(h => h.IsActive).ToListAsync(cancellationToken);
        var approvedRequests = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == LeaveRequestStatus.Approved && r.TeamId == teamId)
            .ToListAsync(cancellationToken);

        return requests.Select(r => MapToDto(r, approvedRequests, holidays)).ToList();
    }

    public async Task<LeaveRequestDto> DecideAsync(
        Guid requestId,
        Guid managerId,
        IReadOnlyList<string> roles,
        DecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Include(r => r.Team)
            .Include(r => r.LeaveType)
            .SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new AppException(404, "LEAVE_REQUEST_NOT_FOUND", "Demande introuvable.");

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
            throw new AppException(409, "ALREADY_DECIDED", $"La demande est déjà au statut {leaveRequest.Status}.");

        if (leaveRequest.Team.ManagerId != managerId)
            throw new AppException(403, "NOT_TEAM_MANAGER", "Vous n'êtes pas le manager de cette équipe.");

        if (leaveRequest.EmployeeId == managerId)
            throw new AppException(403, "SELF_APPROVAL", "Vous ne pouvez pas décider de votre propre demande.");

        var action = request.Action.Trim().ToUpperInvariant();
        if (action != "APPROVE" && action != "REJECT")
            throw new AppException(400, "INVALID_ACTION", "L'action doit être APPROVE ou REJECT.");

        if (action == "REJECT" && string.IsNullOrWhiteSpace(request.Comment))
            throw new AppException(400, "COMMENT_REQUIRED", "Un commentaire est obligatoire pour un refus.");

        var holidays = await _db.Holidays.Where(h => h.IsActive).ToListAsync(cancellationToken);
        var approvedRequests = await _db.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == LeaveRequestStatus.Approved && r.TeamId == leaveRequest.TeamId)
            .ToListAsync(cancellationToken);

        var overlap = OverlapCalculator.Calculate(
            leaveRequest.StartDate,
            leaveRequest.EndDate,
            leaveRequest.EmployeeId,
            leaveRequest.TeamId,
            approvedRequests,
            holidays);

        if (action == "APPROVE")
        {
            if (overlap.HasWarning && !request.AcknowledgeOverlapWarning)
                throw new AppException(409, "OVERLAP_WARNING_NOT_ACKNOWLEDGED", "Un avertissement de chevauchement critique est actif. Vous devez l'acquitter explicitement pour approuver.");

            var year = leaveRequest.StartDate.Year;
            var balance = await _db.LeaveBalances
                .SingleOrDefaultAsync(b => b.UserId == leaveRequest.EmployeeId && b.LeaveTypeId == leaveRequest.LeaveTypeId && b.Year == year, cancellationToken);

            decimal acquired = balance?.AcquiredDays ?? 0;
            decimal adjustment = balance?.AdjustmentDays ?? 0;
            decimal used = balance?.UsedDays ?? 0;

            if (!BalanceCalculator.IsSufficient(acquired, adjustment, used, leaveRequest.WorkingDays))
            {
                var available = BalanceCalculator.Available(acquired, adjustment, used);
                throw new AppException(409, "INSUFFICIENT_BALANCE_AT_APPROVAL", $"Le solde est devenu insuffisant (disponible: {available}, demandé: {leaveRequest.WorkingDays}).");
            }

            if (balance == null)
            {
                balance = new LeaveBalance
                {
                    Id = Guid.NewGuid(),
                    UserId = leaveRequest.EmployeeId,
                    LeaveTypeId = leaveRequest.LeaveTypeId,
                    Year = year,
                    AcquiredDays = 0,
                    AdjustmentDays = 0,
                    UsedDays = 0,
                    Version = 0
                };
                _db.LeaveBalances.Add(balance);
            }

            balance.UsedDays += leaveRequest.WorkingDays;
            balance.Version++;

            leaveRequest.Status = LeaveRequestStatus.Approved;
            leaveRequest.HasOverlapWarning = overlap.HasWarning;
            leaveRequest.OverlapRatio = overlap.Ratio;

            if (request.AcknowledgeOverlapWarning)
            {
                leaveRequest.AcknowledgedOverlapBy = managerId;
                leaveRequest.AcknowledgedOverlapAt = _clock.UtcNow;
            }
        }
        else
        {
            leaveRequest.Status = LeaveRequestStatus.Rejected;
            leaveRequest.HasOverlapWarning = overlap.HasWarning;
            leaveRequest.OverlapRatio = overlap.Ratio;
        }

        leaveRequest.DeciderId = managerId;
        leaveRequest.DecidedAt = _clock.UtcNow;
        leaveRequest.ManagerComment = request.Comment;
        leaveRequest.Version++;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = managerId,
            Action = action == "APPROVE" ? "LEAVE_REQUEST_APPROVED" : "LEAVE_REQUEST_REJECTED",
            EntityType = nameof(LeaveRequest),
            EntityId = leaveRequest.Id,
            OccurredAt = _clock.UtcNow,
            Details = $"Status={leaveRequest.Status};Comment={request.Comment};HasOverlapWarning={overlap.HasWarning};OverlapRatio={overlap.Ratio};Acknowledged={request.AcknowledgeOverlapWarning}",
            Reason = request.Comment
        });

        _db.EmailOutbox.Add(new EmailOutbox
        {
            Id = Guid.NewGuid(),
            Recipient = leaveRequest.Employee.Email,
            Template = action == "APPROVE" ? "LeaveRequestApproved" : "LeaveRequestRejected",
            Payload = $"LeaveRequestId={leaveRequest.Id};Status={leaveRequest.Status};ManagerComment={request.Comment}",
            Status = EmailStatus.Pending,
            Attempts = 0,
            CreatedAt = _clock.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException(409, "CONCURRENCY_CONFLICT", "La demande a été modifiée entre-temps. Veuillez recharger et réessayer.");
        }

        return MapToDto(leaveRequest, approvedRequests, holidays);
    }

    private static LeaveRequestDto MapToDto(
        LeaveRequest request,
        IReadOnlyList<LeaveRequest> approvedRequests,
        IReadOnlyList<Holiday> holidays)
    {
        var overlap = OverlapCalculator.Calculate(
            request.StartDate,
            request.EndDate,
            request.EmployeeId,
            request.TeamId,
            approvedRequests,
            holidays);

        return new LeaveRequestDto(
            request.Id,
            request.EmployeeId,
            request.TeamId,
            new LeaveTypeInfoDto(request.LeaveType.Id, request.LeaveType.Code, request.LeaveType.Label),
            request.StartDate,
            request.EndDate,
            request.WorkingDays,
            request.Status.ToString().ToUpperInvariant(),
            request.EmployeeComment,
            request.SubmittedAt,
            request.DeciderId,
            request.DecidedAt,
            request.ManagerComment,
            overlap.HasWarning,
            overlap.Ratio,
            overlap.Details.Select(d => new OverlapDetailDto(d.Date, d.AbsentEmployees)).ToList(),
            request.AcknowledgedOverlapBy,
            request.AcknowledgedOverlapAt);
    }
}

=== FILE: src/TeamLeave.Infrastructure/Persistence/TeamLeaveDbContext.cs ===
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

=== FILE: src/TeamLeave.Infrastructure/Persistence/DbInitializer.cs ===
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

=== FILE: src/TeamLeave.Infrastructure/Services/SystemClock.cs ===
using TeamLeave.Application.Interfaces;

namespace TeamLeave.Infrastructure.Services;

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

=== FILE: src/TeamLeave.Infrastructure/Services/PasswordHasher.cs ===
using Microsoft.AspNetCore.Identity;
using TeamLeave.Application.Interfaces;
using TeamLeave.Domain.Entities;

namespace TeamLeave.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(new User(), password);

    public bool Verify(string password, string hash)
    {
        var result = _hasher.VerifyHashedPassword(new User(), hash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}

=== FILE: src/TeamLeave.Infrastructure/Services/JwtTokenGenerator.cs ===
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TeamLeave.Application.Interfaces;
using TeamLeave.Domain.Entities;

namespace TeamLeave.Infrastructure.Services;

public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiresMinutes { get; set; } = 60;
}

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string Token, DateTimeOffset ExpiresAt) Generate(User user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("teamId", user.TeamId?.ToString() ?? string.Empty)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.ExpiresMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

=== FILE: src/TeamLeave.Infrastructure/Services/ConsoleEmailSender.cs ===
using Microsoft.Extensions.Logging;
using TeamLeave.Application.Interfaces;

namespace TeamLeave.Infrastructure.Services;

public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) => _logger = logger;

    public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email sent to {Recipient}: {Subject} - {Body}", recipient, subject, body);
        return Task.CompletedTask;
    }
}

=== FILE: src/TeamLeave.Infrastructure/Services/EmailOutboxWorker.cs ===
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeamLeave.Application.Interfaces;
using TeamLeave.Domain.Enums;
using TeamLeave.Infrastructure.Persistence;

namespace TeamLeave.Infrastructure.Services;

public class EmailOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailOutboxWorker> _logger;
    private readonly IEmailSender _emailSender;

    public EmailOutboxWorker(IServiceScopeFactory scopeFactory, ILogger<EmailOutboxWorker> logger, IEmailSender emailSender)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _emailSender = emailSender;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TeamLeaveDbContext>();
                var pending = await db.EmailOutbox
                    .Where(e => e.Status == EmailStatus.Pending && e.Attempts < 3)
                    .OrderBy(e => e.CreatedAt)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var email in pending)
                {
                    try
                    {
                        await _emailSender.SendAsync(email.Recipient, email.Template, email.Payload, stoppingToken);
                        email.Status = EmailStatus.Sent;
                    }
                    catch (Exception ex)
                    {
                        email.Attempts++;
                        email.ErrorMessage = ex.Message;
                        if (email.Attempts >= 3) email.Status = EmailStatus.Failed;
                        _logger.LogError(ex, "Failed to send email {EmailId} to {Recipient}", email.Id, email.Recipient);
                    }
                    email.LastAttemptAt = DateTimeOffset.UtcNow;
                }

                if (pending.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailOutboxWorker loop error");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}

=== FILE: src/TeamLeave.Api/Middleware/ApiExceptionFilter.cs ===
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Exceptions;

namespace TeamLeave.Api.Middleware;

public class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is AppException appEx)
        {
            var problem = new ProblemDetails
            {
                Status = appEx.StatusCode,
                Title = appEx.Code,
                Detail = appEx.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            };
            if (appEx.Errors != null)
                problem.Extensions["errors"] = appEx.Errors;

            context.Result = new ObjectResult(problem) { StatusCode = appEx.StatusCode };
            context.ExceptionHandled = true;
        }
        else if (context.Exception is DbUpdateConcurrencyException)
        {
            var problem = new ProblemDetails
            {
                Status = 409,
                Title = "CONCURRENCY_CONFLICT",
                Detail = "Un conflit de concurrence est survenu. Veuillez recharger et réessayer.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            };
            context.Result = new ObjectResult(problem) { StatusCode = 409 };
            context.ExceptionHandled = true;
        }
        else
        {
            var problem = new ProblemDetails
            {
                Status = 500,
                Title = "INTERNAL_SERVER_ERROR",
                Detail = "Une erreur interne est survenue.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            };
            context.Result = new ObjectResult(problem) { StatusCode = 500 };
            context.ExceptionHandled = true;
        }
    }
}

=== FILE: src/TeamLeave.Api/Controllers/ApiControllerBase.cs ===
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace TeamLeave.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.TryParse(id, out var userId)
                ? userId
                : throw new UnauthorizedAccessException("User ID not found in token.");
        }
    }

    protected IReadOnlyList<string> CurrentRoles =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
}

=== FILE: src/TeamLeave.Api/Controllers/AuthController.cs ===
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Services;

namespace TeamLeave.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService) => _authService = authService;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        => Ok(await _authService.LoginAsync(request, cancellationToken));
}

=== FILE: src/TeamLeave.Api/Controllers/MeController.cs ===
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Services;
using TeamLeave.Domain.Enums;

namespace TeamLeave.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class MeController : ApiControllerBase
{
    private readonly AuthService _authService;
    private readonly BalanceService _balanceService;
    private readonly LeaveRequestService _leaveRequestService;

    public MeController(AuthService authService, BalanceService balanceService, LeaveRequestService leaveRequestService)
    {
        _authService = authService;
        _balanceService = balanceService;
        _leaveRequestService = leaveRequestService;
    }

    [HttpGet]
    public async Task<ActionResult<MeDto>> GetMe(CancellationToken cancellationToken)
        => Ok(await _authService.GetMeAsync(CurrentUserId, cancellationToken));

    [HttpGet("balances")]
    public async Task<ActionResult<IReadOnlyList<BalanceDto>>> GetBalances([FromQuery] int year, CancellationToken cancellationToken)
        => Ok(await _balanceService.GetBalancesAsync(CurrentUserId, year, cancellationToken));

    [HttpGet("leave-requests")]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestDto>>> GetMyLeaveRequests(
        [FromQuery] string? status,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        LeaveRequestStatus? statusEnum = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LeaveRequestStatus>(status, true, out var parsed))
            statusEnum = parsed;

        return Ok(await _leaveRequestService.GetMyRequestsAsync(CurrentUserId, statusEnum, from, to, cancellationToken));
    }
}

=== FILE: src/TeamLeave.Api/Controllers/LeaveRequestsController.cs ===
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Services;

namespace TeamLeave.Api.Controllers;

[ApiController]
[Route("api/v1/leave-requests")]
[Authorize]
public class LeaveRequestsController : ApiControllerBase
{
    private readonly LeaveRequestService _leaveRequestService;

    public LeaveRequestsController(LeaveRequestService leaveRequestService) => _leaveRequestService = leaveRequestService;

    [HttpPost]
    public async Task<ActionResult<LeaveRequestDto>> Submit([FromBody] CreateLeaveRequestRequest request, CancellationToken cancellationToken)
    {
        var result = await _leaveRequestService.SubmitAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeaveRequestDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _leaveRequestService.GetByIdAsync(id, CurrentUserId, CurrentRoles, cancellationToken));

    [HttpPost("{id:guid}/decision")]
    public async Task<ActionResult<LeaveRequestDto>> Decide(Guid id, [FromBody] DecisionRequest request, CancellationToken cancellationToken)
        => Ok(await _leaveRequestService.DecideAsync(id, CurrentUserId, CurrentRoles, request, cancellationToken));
}

=== FILE: src/TeamLeave.Api/Controllers/TeamsController.cs ===
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Services;

namespace TeamLeave.Api.Controllers;

[ApiController]
[Route("api/v1/teams/{teamId:guid}")]
[Authorize]
public class TeamsController : ApiControllerBase
{
    private readonly LeaveRequestService _leaveRequestService;

    public TeamsController(LeaveRequestService leaveRequestService) => _leaveRequestService = leaveRequestService;

    [HttpGet("leave-requests")]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestDto>>> GetTeamLeaveRequests(
        Guid teamId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
        => Ok(await _leaveRequestService.GetTeamPendingRequestsAsync(teamId, CurrentUserId, cancellationToken));

    [HttpGet("calendar")]
    public async Task<ActionResult<IReadOnlyList<LeaveRequestDto>>> GetCalendar(
        Guid teamId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        if (from > to) return BadRequest("from must be before to");
        return Ok(await _leaveRequestService.GetTeamCalendarAsync(teamId, CurrentUserId, CurrentRoles, from, to, cancellationToken));
    }
}

=== FILE: src/TeamLeave.Api/Controllers/LeaveTypesController.cs ===
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Services;
using TeamLeave.Domain.Enums;

namespace TeamLeave.Api.Controllers;

[ApiController]
[Route("api/v1/leave-types")]
[Authorize]
public class LeaveTypesController : ApiControllerBase
{
    private readonly LeaveTypeService _leaveTypeService;

    public LeaveTypesController(LeaveTypeService leaveTypeService) => _leaveTypeService = leaveTypeService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LeaveTypeDto>>> GetAll([FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        var isAdmin = CurrentRoles.Contains(RoleNames.AdminRh);
        return Ok(await _leaveTypeService.GetAllAsync(includeInactive && isAdmin, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.AdminRh)]
    public async Task<ActionResult<LeaveTypeDto>> Create([FromBody] CreateLeaveTypeRequest request, CancellationToken cancellationToken)
    {
        var result = await _leaveTypeService.CreateAsync(request, CurrentUserId, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { includeInactive = false }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.AdminRh)]
    public async Task<ActionResult<LeaveTypeDto>> Update(Guid id, [FromBody] UpdateLeaveTypeRequest request, CancellationToken cancellationToken)
        => Ok(await _leaveTypeService.UpdateAsync(id, request, CurrentUserId, cancellationToken));
}

=== FILE: src/TeamLeave.Api/Controllers/BalancesController.cs ===
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Services;
using TeamLeave.Domain.Enums;

namespace TeamLeave.Api.Controllers;

[ApiController]
[Route("api/v1/users/{userId:guid}/balances")]
[Authorize(Roles = RoleNames.AdminRh)]
public class BalancesController : ApiControllerBase
{
    private readonly BalanceService _balanceService;

    public BalancesController(BalanceService balanceService) => _balanceService = balanceService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BalanceDto>>> GetUserBalances(Guid userId, [FromQuery] int year, CancellationToken cancellationToken)
        => Ok(await _balanceService.GetBalancesAsync(userId, year, cancellationToken));

    [HttpPost("{leaveTypeId:guid}/adjustments")]
    public async Task<IActionResult> Adjust(Guid userId, Guid leaveTypeId, [FromBody] AdjustmentRequest request, CancellationToken cancellationToken)
    {
        await _balanceService.AdjustBalanceAsync(userId, leaveTypeId, request, CurrentUserId, cancellationToken);
        return StatusCode(201);
    }

    [HttpPut("{leaveTypeId:guid}/{year:int}")]
    public async Task<IActionResult> Set(Guid userId, Guid leaveTypeId, int year, [FromBody] SetBalanceRequest request, CancellationToken cancellationToken)
    {
        await _balanceService.SetBalanceAsync(userId, leaveTypeId, year, request, CurrentUserId, cancellationToken);
        return Ok();
    }
}

=== FILE: src/TeamLeave.Api/Controllers/HolidaysController.cs ===
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Services;
using TeamLeave.Domain.Enums;

namespace TeamLeave.Api.Controllers;

[ApiController]
[Route("api/v1/holidays")]
[Authorize]
public class HolidaysController : ApiControllerBase
{
    private readonly HolidayService _holidayService;

    public HolidaysController(HolidayService holidayService) => _holidayService = holidayService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HolidayDto>>> Get([FromQuery] int year, CancellationToken cancellationToken)
        => Ok(await _holidayService.GetAsync(year, cancellationToken));

    [HttpPost]
    [Authorize(Roles = RoleNames.AdminRh)]
    public async Task<ActionResult<HolidayDto>> Create([FromBody] CreateHolidayRequest request, CancellationToken cancellationToken)
    {
        var result = await _holidayService.CreateAsync(request, CurrentUserId, cancellationToken);
        return CreatedAtAction(nameof(Get), new { year = result.Date.Year }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.AdminRh)]
    public async Task<ActionResult<HolidayDto>> Update(Guid id, [FromBody] UpdateHolidayRequest request, CancellationToken cancellationToken)
        => Ok(await _holidayService.UpdateAsync(id, request, CurrentUserId, cancellationToken));
}

=== FILE: src/TeamLeave.Api/Controllers/AuditController.cs ===
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Services;
using TeamLeave.Domain.Enums;

namespace TeamLeave.Api.Controllers;

[ApiController]
[Route("api/v1/audit")]
[Authorize(Roles = RoleNames.AdminRh)]
public class AuditController : ApiControllerBase
{
    private readonly AuditService _auditService;

    public AuditController(AuditService auditService) => _auditService = auditService;

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> Get(
        [FromQuery] string? entityType,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await _auditService.GetAsync(entityType, from, to, page, pageSize, cancellationToken));
}

=== FILE: src/TeamLeave.Api/Program.cs ===
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TeamLeave.Api.Middleware;
using TeamLeave.Application.Interfaces;
using TeamLeave.Application.Services;
using TeamLeave.Infrastructure.Persistence;
using TeamLeave.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddDbContext<TeamLeaveDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITeamLeaveDbContext>(sp => sp.GetRequiredService<TeamLeaveDbContext>());

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IClock, SystemClock>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BalanceService>();
builder.Services.AddScoped<LeaveTypeService>();
builder.Services.AddScoped<HolidayService>();
builder.Services.AddScoped<LeaveRequestService>();
builder.Services.AddScoped<AuditService>();

builder.Services.AddScoped<IEmailSender, ConsoleEmailSender>();
builder.Services.AddHostedService<EmailOutboxWorker>();

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration missing");
var key = Encoding.UTF8.GetBytes(jwtOptions.Key);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers(options => options.Filters.Add<ApiExceptionFilter>());
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = 400,
            Title = "Requête invalide",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };
        return new BadRequestObjectResult(problem);
    };
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TeamLeaveDbContext>();
    await DbInitializer.InitializeAsync(db);
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }

=== FILE: src/TeamLeave.Api/appsettings.json ===
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=teamleave;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "CetteCleDoitEtreRemplaceeEnProduction_32CaracteresMinimum!",
    "Issuer": "TeamLeave",
    "Audience": "TeamLeave",
    "ExpiresMinutes": 60
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  }
}

=== FILE: db/init.sql ===
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE "Teams" (
    "Id" UUID PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "ManagerId" UUID NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE "Users" (
    "Id" UUID PRIMARY KEY,
    "Email" VARCHAR(256) NOT NULL UNIQUE,
    "FirstName" VARCHAR(100) NOT NULL,
    "LastName" VARCHAR(100) NOT NULL,
    "PasswordHash" TEXT NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "TeamId" UUID NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "FK_Users_Teams" FOREIGN KEY ("TeamId") REFERENCES "Teams"("Id") ON DELETE SET NULL
);

ALTER TABLE "Teams" ADD CONSTRAINT "FK_Teams_Users" FOREIGN KEY ("ManagerId") REFERENCES "Users"("Id") ON DELETE RESTRICT;

CREATE TABLE "UserRoles" (
    "UserId" UUID NOT NULL,
    "Role" VARCHAR(50) NOT NULL,
    PRIMARY KEY ("UserId", "Role"),
    CONSTRAINT "FK_UserRoles_Users" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);

CREATE TABLE "LeaveTypes" (
    "Id" UUID PRIMARY KEY,
    "Code" VARCHAR(20) NOT NULL UNIQUE,
    "Label" VARCHAR(200) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "TrackBalance" BOOLEAN NOT NULL DEFAULT TRUE,
    "DisplayOrder" INT NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL
);

CREATE TABLE "LeaveBalances" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "LeaveTypeId" UUID NOT NULL,
    "Year" INT NOT NULL,
    "AcquiredDays" NUMERIC(5,2) NOT NULL DEFAULT 0,
    "AdjustmentDays" NUMERIC(5,2) NOT NULL DEFAULT 0,
    "UsedDays" NUMERIC(5,2) NOT NULL DEFAULT 0,
    "Version" INT NOT NULL DEFAULT 0,
    CONSTRAINT "UQ_LeaveBalances_User_Type_Year" UNIQUE ("UserId", "LeaveTypeId", "Year"),
    CONSTRAINT "FK_LeaveBalances_Users" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_LeaveBalances_LeaveTypes" FOREIGN KEY ("LeaveTypeId") REFERENCES "LeaveTypes"("Id") ON DELETE RESTRICT
);

CREATE TABLE "LeaveRequests" (
    "Id" UUID PRIMARY KEY,
    "EmployeeId" UUID NOT NULL,
    "TeamId" UUID NOT NULL,
    "LeaveTypeId" UUID NOT NULL,
    "StartDate" DATE NOT NULL,
    "EndDate" DATE NOT NULL,
    "WorkingDays" INT NOT NULL,
    "Status" VARCHAR(20) NOT NULL CHECK ("Status" IN ('Pending','Approved','Rejected')),
    "EmployeeComment" TEXT NULL,
    "SubmittedAt" TIMESTAMPTZ NOT NULL,
    "DeciderId" UUID NULL,
    "DecidedAt" TIMESTAMPTZ NULL,
    "ManagerComment" TEXT NULL,
    "HasOverlapWarning" BOOLEAN NOT NULL DEFAULT FALSE,
    "OverlapRatio" NUMERIC(5,4) NULL,
    "AcknowledgedOverlapBy" UUID NULL,
    "AcknowledgedOverlapAt" TIMESTAMPTZ NULL,
    "Version" INT NOT NULL DEFAULT 0,
    CONSTRAINT "FK_LeaveRequests_Users_Employee" FOREIGN KEY ("EmployeeId") REFERENCES "Users"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_LeaveRequests_Teams" FOREIGN KEY ("TeamId") REFERENCES "Teams"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_LeaveRequests_LeaveTypes" FOREIGN KEY ("LeaveTypeId") REFERENCES "LeaveTypes"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_LeaveRequests_Users_Decider" FOREIGN KEY ("DeciderId") REFERENCES "Users"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_LeaveRequests_Users_Ack" FOREIGN KEY ("AcknowledgedOverlapBy") REFERENCES "Users"("Id") ON DELETE RESTRICT,
    CONSTRAINT "CK_LeaveRequests_Dates" CHECK ("StartDate" <= "EndDate")
);

CREATE INDEX "IX_LeaveRequests_Team_Status" ON "LeaveRequests" ("TeamId", "Status");
CREATE INDEX "IX_LeaveRequests_Employee_Status" ON "LeaveRequests" ("EmployeeId", "Status");
CREATE INDEX "IX_LeaveRequests_Dates" ON "LeaveRequests" ("StartDate", "EndDate");

CREATE TABLE "Holidays" (
    "Id" UUID PRIMARY KEY,
    "Date" DATE NOT NULL UNIQUE,
    "Label" VARCHAR(200) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL
);

CREATE TABLE "AuditLogs" (
    "Id" BIGSERIAL PRIMARY KEY,
    "ActorId" UUID NULL,
    "Action" VARCHAR(100) NOT NULL,
    "EntityType" VARCHAR(100) NOT NULL,
    "EntityId" UUID NULL,
    "OccurredAt" TIMESTAMPTZ NOT NULL,
    "Details" TEXT NULL,
    "Reason" TEXT NULL,
    CONSTRAINT "FK_AuditLogs_Users" FOREIGN KEY ("ActorId") REFERENCES "Users"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_AuditLogs_Entity" ON "AuditLogs" ("EntityType", "EntityId");
CREATE INDEX "IX_AuditLogs_OccurredAt" ON "AuditLogs" ("OccurredAt");

CREATE TABLE "EmailOutbox" (
    "Id" UUID PRIMARY KEY,
    "Recipient" VARCHAR(256) NOT NULL,
    "Template" VARCHAR(100) NOT NULL,
    "Payload" TEXT NOT NULL,
    "Status" VARCHAR(20) NOT NULL CHECK ("Status" IN ('Pending','Sent','Failed')),
    "Attempts" INT NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "LastAttemptAt" TIMESTAMPTZ NULL,
    "ErrorMessage" TEXT NULL
);

CREATE INDEX "IX_EmailOutbox_Status_CreatedAt" ON "EmailOutbox" ("Status", "CreatedAt");

=== FILE: tests/TeamLeave.Tests/DomainTests/BalanceCalculatorTests.cs ===
using FluentAssertions;
using TeamLeave.Domain.Services;
using Xunit;

namespace TeamLeave.Tests.DomainTests;

public class BalanceCalculatorTests
{
    [Fact]
    public void Available_ShouldComputeAcquiredPlusAdjustmentMinusUsed()
    {
        BalanceCalculator.Available(20, 2, 5).Should().Be(17);
    }

    [Theory]
    [InlineData(20, 0, 5, 15, true)]
    [InlineData(20, 0, 5, 16, false)]
    [InlineData(0, 0, 0, 0, true)]
    public void IsSufficient_ShouldReturnExpected(decimal acquired, decimal adjustment, decimal used, int requested, bool expected)
    {
        BalanceCalculator.IsSufficient(acquired, adjustment, used, requested).Should().Be(expected);
    }

    [Fact]
    public void WouldBeNegative_ShouldReturnTrueWhenAvailableNegative()
    {
        BalanceCalculator.WouldBeNegative(10, 0, 11).Should().BeTrue();
        BalanceCalculator.WouldBeNegative(10, 0, 10).Should().BeFalse();
    }
}

=== FILE: tests/TeamLeave.Tests/DomainTests/WorkingDaysCalculatorTests.cs ===
using FluentAssertions;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Services;
using Xunit;

namespace TeamLeave.Tests.DomainTests;

public class WorkingDaysCalculatorTests
{
    [Fact]
    public void Calculate_WithoutHolidays_ShouldCountWeekdays()
    {
        var start = new DateOnly(2025, 7, 14); // Monday
        var end = new DateOnly(2025, 7, 18); // Friday
        WorkingDaysCalculator.Calculate(start, end, Enumerable.Empty<Holiday>()).Should().Be(5);
    }

    [Fact]
    public void Calculate_ShouldExcludeWeekend()
    {
        var start = new DateOnly(2025, 7, 18); // Friday
        var end = new DateOnly(2025, 7, 21); // Monday
        WorkingDaysCalculator.Calculate(start, end, Enumerable.Empty<Holiday>()).Should().Be(2);
    }

    [Fact]
    public void Calculate_ShouldExcludeActiveHolidays()
    {
        var start = new DateOnly(2025, 7, 14);
        var end = new DateOnly(2025, 7, 18);
        var holidays = new[]
        {
            new Holiday { Date = new DateOnly(2025, 7, 15), IsActive = true },
            new Holiday { Date = new DateOnly(2025, 7, 16), IsActive = false }
        };
        WorkingDaysCalculator.Calculate(start, end, holidays).Should().Be(4);
    }
}

=== FILE: tests/TeamLeave.Tests/DomainTests/OverlapCalculatorTests.cs ===
using FluentAssertions;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;
using TeamLeave.Domain.Services;
using Xunit;

namespace TeamLeave.Tests.DomainTests;

public class OverlapCalculatorTests
{
    private static LeaveRequest CreateApproved(Guid employeeId, Guid teamId, DateOnly start, DateOnly end)
    {
        return new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TeamId = teamId,
            StartDate = start,
            EndDate = end,
            Status = LeaveRequestStatus.Approved,
            Employee = new User { FirstName = "A", LastName = "B" }
        };
    }

    [Fact]
    public void Calculate_WhenOverlapIs60Percent_ShouldWarn()
    {
        var employeeId = Guid.NewGuid();
        var otherEmployeeId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var start = new DateOnly(2025, 7, 14);
        var end = new DateOnly(2025, 7, 18);
        var approved = new[]
        {
            CreateApproved(otherEmployeeId, teamId, new DateOnly(2025, 7, 15), new DateOnly(2025, 7, 17))
        };
        var result = OverlapCalculator.Calculate(start, end, employeeId, teamId, approved, Enumerable.Empty<Holiday>());
        result.WorkingDays.Should().Be(5);
        result.OverlapDays.Should().Be(3);
        result.Ratio.Should().Be(0.6m);
        result.HasWarning.Should().BeTrue();
    }

    [Fact]
    public void Calculate_WhenOverlapIsExactly50Percent_ShouldNotWarn()
    {
        var employeeId = Guid.NewGuid();
        var otherEmployeeId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var start = new DateOnly(2025, 7, 14);
        var end = new DateOnly(2025, 7, 17);
        var approved = new[]
        {
            CreateApproved(otherEmployeeId, teamId, new DateOnly(2025, 7, 14), new DateOnly(2025, 7, 15))
        };
        var result = OverlapCalculator.Calculate(start, end, employeeId, teamId, approved, Enumerable.Empty<Holiday>());
        result.WorkingDays.Should().Be(4);
        result.OverlapDays.Should().Be(2);
        result.Ratio.Should().Be(0.5m);
        result.HasWarning.Should().BeFalse();
    }

    [Fact]
    public void Calculate_ShouldIgnoreOwnLeave()
    {
        var employeeId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var start = new DateOnly(2025, 7, 14);
        var end = new DateOnly(2025, 7, 18);
        var approved = new[]
        {
            CreateApproved(employeeId, teamId, start, end)
        };
        var result = OverlapCalculator.Calculate(start, end, employeeId, teamId, approved, Enumerable.Empty<Holiday>());
        result.OverlapDays.Should().Be(0);
        result.HasWarning.Should().BeFalse();
    }

    [Fact]
    public void Calculate_ShouldIgnorePendingAndOtherTeam()
    {
        var employeeId = Guid.NewGuid();
        var otherEmployeeId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var start = new DateOnly(2025, 7, 14);
        var end = new DateOnly(2025, 7, 18);
        var pending = new LeaveRequest
        {
            EmployeeId = otherEmployeeId,
            TeamId = teamId,
            StartDate = start,
            EndDate = end,
            Status = LeaveRequestStatus.Pending,
            Employee = new User { FirstName = "P", LastName = "Pending" }
        };
        var otherTeam = CreateApproved(otherEmployeeId, otherTeamId, start, end);
        var result = OverlapCalculator.Calculate(start, end, employeeId, teamId, new[] { pending, otherTeam }, Enumerable.Empty<Holiday>());
        result.OverlapDays.Should().Be(0);
        result.HasWarning.Should().BeFalse();
    }
}

=== FILE: tests/TeamLeave.Tests/ApplicationTests/LeaveRequestServiceTests.cs ===
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

NOTES DE RUN
============

Prérequis : .NET 8 SDK, PostgreSQL 16 (ou utiliser la chaîne de connexion de votre choix).

Commandes :
  dotnet restore
  dotnet build
  dotnet test
  dotnet run --project src/TeamLeave.Api/TeamLeave.Api.csproj

La base est créée automatiquement au démarrage via `EnsureCreated` et initialisée avec des données de démonstration :
  admin@teamleave.local / Admin123!
  manager@teamleave.local / Manager123!
  employee@teamleave.local / Employee123!

Alternative SQL : exécuter `db/init.sql` sur une base PostgreSQL vide avant de lancer l’API.

L’envoi d’emails est simulé (log console) via `ConsoleEmailSender`. Aucune dépendance SMTP n’est nécessaire pour le MVP.