using System.ComponentModel.DataAnnotations;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Notifications;
using TeamLeave.Domain.Users;

namespace TeamLeave.Application.Contracts;

// ---------------------------------------------------------------------------------------------
// Contrat d'API (step2_arch §5). Les réponses sont toujours des DTO, jamais des entités (§3.3).
// ---------------------------------------------------------------------------------------------

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed class PageQuery
{
    [Range(1, int.MaxValue, ErrorMessage = "La page doit être supérieure ou égale à 1.")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "La taille de page doit être comprise entre 1 et 100.")]
    public int PageSize { get; set; } = 20;
}

public sealed record PersonRef(Guid Id, string FullName);

public sealed record LeaveTypeRef(Guid Id, string Code, string Label);

public sealed record RateDto(int Absent, int TeamSize, int Percent);

// --- Authentification et profil (§5.2) ---

public sealed class LoginRequest
{
    [Required(ErrorMessage = "L'email est obligatoire.")]
    [MaxLength(254)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Le mot de passe est obligatoire.")]
    [MaxLength(128)]
    public string? Password { get; set; }
}

public sealed record TokenResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc, bool MustChangePassword);

public sealed record MeDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<Role> Roles,
    PersonRef? Manager,
    bool MustChangePassword);

public sealed class ChangePasswordRequest
{
    [Required]
    [MaxLength(128)]
    public string? CurrentPassword { get; set; }

    [Required]
    [MaxLength(128)]
    public string? NewPassword { get; set; }
}

public sealed record BalanceDto(LeaveTypeRef LeaveType, decimal Amount, decimal Pending, decimal Available);

public sealed record MovementDto(
    long Id,
    MovementKind Kind,
    decimal Quantity,
    decimal BalanceAfter,
    string? Reason,
    Guid? LeaveRequestId,
    PersonRef Author,
    DateTime OccurredAtUtc);

// --- Demandes (§5.3) ---

public sealed class SimulateRequest
{
    [Required]
    public Guid? LeaveTypeId { get; set; }

    [Required]
    public DateOnly? StartDate { get; set; }

    [Required]
    public DateOnly? EndDate { get; set; }
}

public sealed class SubmitLeaveRequest
{
    [Required]
    public Guid? LeaveTypeId { get; set; }

    [Required]
    public DateOnly? StartDate { get; set; }

    [Required]
    public DateOnly? EndDate { get; set; }

    [MaxLength(LeaveRequest.MaxRequesterCommentLength, ErrorMessage = "Le commentaire est limité à 500 caractères.")]
    public string? Comment { get; set; }
}

public sealed record SimulationBalanceDto(decimal Amount, decimal Pending, decimal AvailableBefore, decimal AvailableAfter);

public sealed record SimulationDto(
    int DaysCount,
    IReadOnlyList<DateOnly> WorkingDays,
    SimulationBalanceDto? Balance,
    bool IsSubmittable,
    IReadOnlyList<string> BlockingReasons);

public sealed record HistoryEntryDto(
    LeaveRequestStatus? FromStatus,
    LeaveRequestStatus ToStatus,
    PersonRef? Author,
    DateTime OccurredAtUtc,
    string? Comment);

public sealed record OverlapAcknowledgementDto(PersonRef AcknowledgedBy, DateTime AcknowledgedAtUtc, RateDto Rate);

public sealed record DecisionDto(
    LeaveRequestStatus Outcome,
    PersonRef DecidedBy,
    DateTime DecidedAtUtc,
    string? Comment,
    OverlapAcknowledgementDto? OverlapAcknowledgement);

public sealed record CancellationDto(PersonRef? CancelledBy, bool IsSystem, DateTime CancelledAtUtc, string? Reason);

public sealed record OverlapDayDto(DateOnly Date, RateDto Rate, IReadOnlyList<PersonRef> AbsentColleagues);

public sealed record OverlapAnalysisDto(
    Guid LeaveRequestId,
    DateTime ComputedAtUtc,
    bool Applicable,
    int TeamSize,
    bool IsAlert,
    RateDto? MaxRate,
    IReadOnlyList<OverlapDayDto> DaysOverThreshold);

public sealed record LeaveRequestDto(
    Guid Id,
    PersonRef Requester,
    LeaveTypeRef LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    int DaysCount,
    string? Comment,
    LeaveRequestStatus Status,
    DateTime SubmittedAtUtc,
    DecisionDto? Decision,
    CancellationDto? Cancellation,
    IReadOnlyList<HistoryEntryDto> History,
    OverlapAnalysisDto? Overlap);

public sealed record OverlapAlertDto(bool IsAlert, int MaxRatePercent);

public sealed record LeaveRequestSummaryDto(
    Guid Id,
    PersonRef Requester,
    LeaveTypeRef LeaveType,
    DateOnly StartDate,
    DateOnly EndDate,
    int DaysCount,
    LeaveRequestStatus Status,
    DateTime SubmittedAtUtc,
    OverlapAlertDto? OverlapAlert);

public sealed class LeaveRequestListQuery
{
    [Required(ErrorMessage = "Le paramètre scope est obligatoire (mine, to-decide, all).")]
    [RegularExpression("^(mine|to-decide|all)$", ErrorMessage = "scope doit valoir mine, to-decide ou all.")]
    public string? Scope { get; set; }

    public LeaveRequestStatus? Status { get; set; }

    public Guid? LeaveTypeId { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public Guid? ManagerId { get; set; }

    public Guid? RequesterId { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public sealed class ApproveRequest
{
    [MaxLength(LeaveRequest.MaxDecisionCommentLength, ErrorMessage = "Le commentaire est limité à 1 000 caractères.")]
    public string? Comment { get; set; }

    public bool OverlapAcknowledged { get; set; }
}

public sealed class RejectRequest
{
    // Pas de [Required] : un commentaire absent ou vide donne REJECTION_COMMENT_REQUIRED (RB-15), pas VALIDATION_ERROR.
    public string? Comment { get; set; }
}

// --- Calendrier (§5.5) ---

public sealed class CalendarQuery
{
    [Required(ErrorMessage = "La date de début est obligatoire.")]
    public DateOnly? From { get; set; }

    [Required(ErrorMessage = "La date de fin est obligatoire.")]
    public DateOnly? To { get; set; }

    [Required]
    [RegularExpression("^(team|reports|all)$", ErrorMessage = "scope doit valoir team, reports ou all.")]
    public string? Scope { get; set; } = "team";

    public Guid? ManagerId { get; set; }
}

public sealed record CalendarAbsenceDto(PersonRef User, DateOnly StartDate, DateOnly EndDate, LeaveTypeRef? LeaveType);

public sealed record CalendarDto(DateOnly From, DateOnly To, string Scope, IReadOnlyList<CalendarAbsenceDto> Absences);

// --- Administration (§5.6) ---

public sealed record LeaveTypeDto(Guid Id, string Code, string Label, bool IsTracked, bool AllowsRetroactive, bool IsSystem, bool IsActive);

public sealed class CreateLeaveTypeRequest
{
    [Required]
    [RegularExpression("^[A-Z0-9]{2,10}$", ErrorMessage = "Le code doit comporter 2 à 10 caractères alphanumériques majuscules.")]
    public string? Code { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string? Label { get; set; }

    public bool IsTracked { get; set; }

    public bool AllowsRetroactive { get; set; }
}

public sealed class UpdateLeaveTypeRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string? Label { get; set; }

    [Required]
    public bool? IsTracked { get; set; }

    [Required]
    public bool? AllowsRetroactive { get; set; }

    [Required]
    public bool? IsActive { get; set; }
}

public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<Role> Roles,
    PersonRef? Manager,
    bool IsActive,
    bool MustChangePassword,
    int ActiveReportsCount,
    DateTime CreatedAtUtc,
    DateTime? DeactivatedAtUtc);

public sealed class UserListQuery
{
    public bool? IsActive { get; set; }

    public Guid? ManagerId { get; set; }

    public Role? Role { get; set; }

    public bool? HasManager { get; set; }

    [MaxLength(100)]
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public sealed class CreateUserRequest
{
    [Required]
    [EmailAddress(ErrorMessage = "Adresse email invalide.")]
    [MaxLength(254)]
    public string? Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string? FirstName { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string? LastName { get; set; }

    public List<Role>? Roles { get; set; }

    public Guid? ManagerId { get; set; }

    [Required]
    [MaxLength(128)]
    public string? TemporaryPassword { get; set; }
}

public sealed class UpdateUserRequest
{
    [Required]
    [EmailAddress(ErrorMessage = "Adresse email invalide.")]
    [MaxLength(254)]
    public string? Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string? FirstName { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string? LastName { get; set; }

    [Required]
    public List<Role>? Roles { get; set; }

    public Guid? ManagerId { get; set; }
}

public sealed class ResetPasswordRequest
{
    [Required]
    [MaxLength(128)]
    public string? TemporaryPassword { get; set; }
}

public sealed class CreateMovementRequest
{
    [Required]
    public MovementKind? Kind { get; set; }

    [Required]
    public decimal? Quantity { get; set; }

    [Required(ErrorMessage = "Le motif est obligatoire.")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Le motif doit contenir entre 1 et 500 caractères.")]
    public string? Reason { get; set; }
}

public sealed record HolidayDto(DateOnly Date, string Label);

public sealed class CreateHolidayRequest
{
    [Required]
    public DateOnly? Date { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string? Label { get; set; }
}

// --- Journaux (§5.7) ---

public sealed record NotificationDto(
    Guid Id,
    NotificationEventType EventType,
    Guid LeaveRequestId,
    string RecipientEmail,
    NotificationStatus Status,
    int Attempts,
    DateTime CreatedAtUtc,
    DateTime? LastAttemptAtUtc,
    DateTime? SentAtUtc,
    string? LastError);

public sealed class NotificationListQuery
{
    public NotificationStatus? Status { get; set; }

    public Guid? LeaveRequestId { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public sealed record AuditEntryDto(
    long Id,
    PersonRef Author,
    string Action,
    string EntityType,
    string EntityId,
    System.Text.Json.JsonElement Details,
    DateTime OccurredAtUtc);

public sealed class AuditLogQuery
{
    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    [MaxLength(40)]
    public string? EntityType { get; set; }

    [MaxLength(64)]
    public string? EntityId { get; set; }

    public Guid? AuthorId { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}
