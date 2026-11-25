namespace TeamLeave.Domain.Audit;

/// <summary>Entrée du journal d'administration, en ajout seul (RB-26, GAP-13).</summary>
public sealed class AuditEntry
{
    private AuditEntry()
    {
        Action = string.Empty;
        EntityType = string.Empty;
        EntityId = string.Empty;
        Details = string.Empty;
    }

    public long Id { get; private set; }

    public Guid AuthorId { get; private set; }

    public string Action { get; private set; }

    public string EntityType { get; private set; }

    public string EntityId { get; private set; }

    /// <summary>JSON {before, after}, jamais de secret.</summary>
    public string Details { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public static AuditEntry Create(Guid authorId, string action, string entityType, string entityId, string detailsJson, DateTime nowUtc) =>
        new()
        {
            AuthorId = authorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = detailsJson,
            OccurredAtUtc = nowUtc,
        };
}
