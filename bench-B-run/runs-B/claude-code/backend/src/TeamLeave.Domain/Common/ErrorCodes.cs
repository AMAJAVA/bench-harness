namespace TeamLeave.Domain.Common;

/// <summary>Catalogue des codes d'erreur stables (step2_arch §5.8, 38 codes).</summary>
public static class ErrorCodes
{
    // 400
    public const string ValidationError = "VALIDATION_ERROR";
    public const string RejectionCommentRequired = "REJECTION_COMMENT_REQUIRED";
    public const string RangeTooLong = "RANGE_TOO_LONG";

    // 401
    public const string Unauthenticated = "UNAUTHENTICATED";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";

    // 403
    public const string Forbidden = "FORBIDDEN";
    public const string SelfApprovalForbidden = "SELF_APPROVAL_FORBIDDEN";
    public const string NotApprover = "NOT_APPROVER";
    public const string NotRequester = "NOT_REQUESTER";
    public const string PasswordChangeRequired = "PASSWORD_CHANGE_REQUIRED";

    // 404
    public const string NotFound = "NOT_FOUND";

    // 409
    public const string InvalidStatusTransition = "INVALID_STATUS_TRANSITION";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string EmailAlreadyUsed = "EMAIL_ALREADY_USED";
    public const string LeaveTypeCodeAlreadyUsed = "LEAVE_TYPE_CODE_ALREADY_USED";
    public const string LeaveTypeInUse = "LEAVE_TYPE_IN_USE";
    public const string SystemLeaveType = "SYSTEM_LEAVE_TYPE";
    public const string HolidayAlreadyExists = "HOLIDAY_ALREADY_EXISTS";
    public const string UserAlreadyInactive = "USER_ALREADY_INACTIVE";

    // 422
    public const string NoWorkingDay = "NO_WORKING_DAY";
    public const string PastDateNotAllowed = "PAST_DATE_NOT_ALLOWED";
    public const string PeriodTooLong = "PERIOD_TOO_LONG";
    public const string LeaveTypeInactive = "LEAVE_TYPE_INACTIVE";
    public const string LeaveTypeNotTracked = "LEAVE_TYPE_NOT_TRACKED";
    public const string InsufficientBalance = "INSUFFICIENT_BALANCE";
    public const string BalanceBelowPending = "BALANCE_BELOW_PENDING";
    public const string OverlapsOwnRequest = "OVERLAPS_OWN_REQUEST";
    public const string AlreadyStarted = "ALREADY_STARTED";
    public const string OverlapNotAcknowledged = "OVERLAP_NOT_ACKNOWLEDGED";
    public const string HierarchyCycle = "HIERARCHY_CYCLE";
    public const string HasActiveReports = "HAS_ACTIVE_REPORTS";
    public const string InvalidManager = "INVALID_MANAGER";
    public const string LastHrAdmin = "LAST_HR_ADMIN";
    public const string LeaveTypeHasBalances = "LEAVE_TYPE_HAS_BALANCES";
    public const string LeaveTypeHasPendingRequests = "LEAVE_TYPE_HAS_PENDING_REQUESTS";
    public const string InvalidCurrentPassword = "INVALID_CURRENT_PASSWORD";

    // 429
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string RateLimited = "RATE_LIMITED";
}
