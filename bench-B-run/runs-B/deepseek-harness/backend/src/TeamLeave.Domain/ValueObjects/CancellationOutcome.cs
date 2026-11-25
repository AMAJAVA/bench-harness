namespace TeamLeave.Domain.ValueObjects;

public sealed record CancellationOutcome(bool Allowed, string? Code, int HttpStatus, bool RestoreBalance)
{
    public static CancellationOutcome Allow(bool restoreBalance = false) => new(true, null, 200, restoreBalance);
    public static CancellationOutcome Refuse(string code, int httpStatus) => new(false, code, httpStatus, false);
}
