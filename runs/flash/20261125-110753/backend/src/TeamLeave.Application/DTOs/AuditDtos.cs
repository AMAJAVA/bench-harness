namespace TeamLeave.Application.DTOs;

public record AuditLogDto(long Id, Guid? ActorId, string Action, string EntityType, Guid? EntityId, DateTimeOffset OccurredAt, string? Details, string? Reason);
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
