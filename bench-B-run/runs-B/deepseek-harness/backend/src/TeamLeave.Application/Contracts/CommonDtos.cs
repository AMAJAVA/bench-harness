namespace TeamLeave.Application.Contracts;

public sealed record PersonSummary(Guid Id, string FirstName, string LastName);

public sealed record LeaveTypeSummary(Guid Id, string Code, string Label, string? Color = null);

public sealed record TeamSummary(Guid Id, string Name);

public sealed record HalfDaySlotDto(DateOnly Date, string HalfDay);

public sealed record AcknowledgementDto(PersonSummary By, DateTimeOffset At);

public sealed record OverlapDto(
    bool Applicable,
    bool Active,
    Guid? TeamId,
    int TeamSize,
    decimal? MaxRate,
    IReadOnlyList<HalfDaySlotDto> BreachingHalfDays,
    DateTimeOffset EvaluatedAt,
    AcknowledgementDto? Acknowledgement);
