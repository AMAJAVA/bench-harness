using TeamLeave.Domain.Enums;

namespace TeamLeave.Domain.Entities;

public sealed class Notification
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string EventKey { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public Guid LeaveRequestId { get; set; }
    public LeaveRequest? LeaveRequest { get; set; }
    public Guid? RecipientUserId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.PENDING;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}
