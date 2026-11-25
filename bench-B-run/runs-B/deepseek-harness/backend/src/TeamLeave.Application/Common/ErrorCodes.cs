namespace TeamLeave.Application.Common;

public static class ErrorCodes
{
    public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";
    public const string INVALID_REFRESH_TOKEN = "INVALID_REFRESH_TOKEN";
    public const string TOKEN_REUSED = "TOKEN_REUSED";
    public const string UNAUTHENTICATED = "UNAUTHENTICATED";
    public const string PASSWORD_CHANGE_REQUIRED = "PASSWORD_CHANGE_REQUIRED";
    public const string PASSWORD_POLICY = "PASSWORD_POLICY";
    public const string FORBIDDEN = "FORBIDDEN";
    public const string NOT_FOUND = "NOT_FOUND";
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";
    public const string INVALID_DATES = "INVALID_DATES";
    public const string INACTIVE_LEAVE_TYPE = "INACTIVE_LEAVE_TYPE";
    public const string INSUFFICIENT_BALANCE = "INSUFFICIENT_BALANCE";
    public const string COMMENT_REQUIRED = "COMMENT_REQUIRED";
    public const string INVALID_DAYS_STEP = "INVALID_DAYS_STEP";
    public const string REASON_REQUIRED = "REASON_REQUIRED";
    public const string BALANCE_WOULD_BE_NEGATIVE = "BALANCE_WOULD_BE_NEGATIVE";
    public const string INVALID_PAGINATION = "INVALID_PAGINATION";
    public const string PERIOD_TOO_LONG = "PERIOD_TOO_LONG";
    public const string PERSONAL_OVERLAP = "PERSONAL_OVERLAP";
    public const string NO_APPROVER = "NO_APPROVER";
    public const string INSUFFICIENT_BALANCE_AT_APPROVAL = "INSUFFICIENT_BALANCE_AT_APPROVAL";
    public const string OVERLAP_ACK_REQUIRED = "OVERLAP_ACK_REQUIRED";
    public const string INVALID_STATE = "INVALID_STATE";
    public const string DUPLICATE_CODE = "DUPLICATE_CODE";
    public const string DUPLICATE_NAME = "DUPLICATE_NAME";
    public const string EMAIL_ALREADY_USED = "EMAIL_ALREADY_USED";
    public const string LAST_HR_ADMIN = "LAST_HR_ADMIN";
    public const string INVALID_MANAGER = "INVALID_MANAGER";
    public const string TEAM_INACTIVE = "TEAM_INACTIVE";
    public const string USER_INACTIVE = "USER_INACTIVE";
    public const string IMMUTABLE_FIELD = "IMMUTABLE_FIELD";
    public const string METHOD_NOT_ALLOWED = "METHOD_NOT_ALLOWED";
    public const string RATE_LIMITED = "RATE_LIMITED";
    public const string SERVICE_UNAVAILABLE = "SERVICE_UNAVAILABLE";
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
    public const string INVALID_HALF_DAY = "INVALID_HALF_DAY";
    public const string UNKNOWN_ROLE = "UNKNOWN_ROLE";
}
