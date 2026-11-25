namespace TeamLeave.Domain.ValueObjects;

public sealed record DecisionOutcome(bool Allowed, string? Code, int HttpStatus)
{
    public static DecisionOutcome Allow() => new(true, null, 200);
    public static DecisionOutcome Refuse(string code, int httpStatus) => new(false, code, httpStatus);
}
