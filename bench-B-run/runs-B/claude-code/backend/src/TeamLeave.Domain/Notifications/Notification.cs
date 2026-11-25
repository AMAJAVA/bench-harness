using TeamLeave.Domain.Common;

namespace TeamLeave.Domain.Notifications;

public enum NotificationEventType
{
    Submission,
    Approval,
    Rejection,
    ApprovedCancellation,
}

public enum NotificationStatus
{
    Queued,
    Sent,
    Failed,
}

/// <summary>Email en outbox (RB-21, RB-22, RB-28, ADR-03). Une ligne par destinataire.</summary>
public sealed class Notification : IVersioned
{
    public const int MaxAttempts = 4;

    /// <summary>Calendrier des tentatives relatif à la création : t0, t0+5 min, t0+20 min, t0+65 min (ADR-03).</summary>
    public static readonly TimeSpan[] Schedule =
    [
        TimeSpan.Zero,
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(20),
        TimeSpan.FromMinutes(65),
    ];

    private Notification()
    {
        RecipientEmail = string.Empty;
        Subject = string.Empty;
        Body = string.Empty;
    }

    public Guid Id { get; private set; }

    public NotificationEventType EventType { get; private set; }

    public Guid LeaveRequestId { get; private set; }

    public string RecipientEmail { get; private set; }

    public string Subject { get; private set; }

    public string Body { get; private set; }

    public NotificationStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTime? NextAttemptAtUtc { get; private set; }

    public DateTime? LockedUntilUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? LastAttemptAtUtc { get; private set; }

    public DateTime? SentAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public int Version { get; private set; }

    public static Notification Queue(
        Guid id,
        NotificationEventType eventType,
        Guid leaveRequestId,
        string recipientEmail,
        string subject,
        string body,
        DateTime nowUtc) =>
        new()
        {
            Id = id,
            EventType = eventType,
            LeaveRequestId = leaveRequestId,
            RecipientEmail = recipientEmail,
            // Pas de retour chariot dans le sujet (injection d'en-têtes, step2_arch §7.3).
            Subject = subject.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal),
            Body = body,
            Status = NotificationStatus.Queued,
            NextAttemptAtUtc = nowUtc,
            CreatedAtUtc = nowUtc,
        };

    public bool IsDue(DateTime nowUtc) =>
        Status == NotificationStatus.Queued
        && NextAttemptAtUtc <= nowUtc
        && (LockedUntilUtc is null || LockedUntilUtc <= nowUtc);

    public void Reserve(DateTime lockedUntilUtc) => LockedUntilUtc = lockedUntilUtc;

    public void MarkSent(DateTime nowUtc)
    {
        Attempts++;
        LastAttemptAtUtc = nowUtc;
        Status = NotificationStatus.Sent;
        SentAtUtc = nowUtc;
        NextAttemptAtUtc = null;
        LockedUntilUtc = null;
        LastError = null;
    }

    /// <summary>Échec d'envoi : prochaine tentative selon le calendrier, ou « Échec » après la 4e (RB-28).</summary>
    public void MarkAttemptFailed(string error, DateTime nowUtc)
    {
        Attempts++;
        LastAttemptAtUtc = nowUtc;
        LockedUntilUtc = null;
        LastError = error.Length > 500 ? error[..500] : error;
        if (Attempts >= MaxAttempts)
        {
            Status = NotificationStatus.Failed;
            NextAttemptAtUtc = null;
        }
        else
        {
            NextAttemptAtUtc = CreatedAtUtc + Schedule[Attempts];
        }
    }

    public void Touch() => Version++;
}
