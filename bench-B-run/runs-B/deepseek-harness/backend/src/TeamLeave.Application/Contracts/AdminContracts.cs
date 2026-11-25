using System.Text.Json;

namespace TeamLeave.Application.Contracts;

public sealed record LeaveTypeDto(
    Guid Id,
    string Code,
    string Label,
    string Color,
    bool Active,
    bool IsSystem,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateLeaveTypeRequest(string? Code, string? Label, string? Color, bool Active = true);

public sealed record UpdateLeaveTypeRequest(string? Code, string? Label, string? Color, bool? Active);

public sealed record UserAdminDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles,
    Guid TeamId,
    TeamSummary? Team,
    Guid? ManagerId,
    PersonSummary? Manager,
    bool IsActive,
    bool MustChangePassword,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastLoginAt);

public sealed record CreateUserRequest(
    string? Email,
    string? FirstName,
    string? LastName,
    Guid TeamId,
    Guid? ManagerId,
    IReadOnlyList<string>? Roles);

public sealed record UpdateUserRequest(
    string? Email,
    string? FirstName,
    string? LastName,
    Guid? TeamId,
    Guid? ManagerId,
    bool ClearManager,
    IReadOnlyList<string>? Roles);

public sealed record DeactivateUserResponse(
    Guid UserId,
    DateTimeOffset DeactivatedAt,
    int CancelledPendingRequests,
    int CancelledFutureApprovedRequests,
    decimal RestoredDays);

public sealed record ResetPasswordResponse(
    string TemporaryPassword,
    bool MustChangePassword,
    int RefreshTokensRevoked);

public sealed record TeamDto(
    Guid Id,
    string Name,
    bool Active,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateTeamRequest(string? Name);

public sealed record UpdateTeamRequest(string? Name, bool? Active);

public sealed record NotificationDto(
    Guid Id,
    string Type,
    Guid LeaveRequestId,
    string RecipientEmail,
    string Status,
    int Attempts,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt,
    DateTimeOffset NextAttemptAt);

public sealed record AuditEntryDto(
    long Id,
    PersonSummary? Actor,
    string Action,
    string EntityType,
    Guid? EntityId,
    DateTimeOffset OccurredAt,
    JsonElement? BeforeSummary,
    JsonElement? AfterSummary,
    string? Reason);
