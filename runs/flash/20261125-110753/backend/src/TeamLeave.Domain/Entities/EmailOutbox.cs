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
