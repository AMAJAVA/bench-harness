using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamLeave.Application.Abstractions;
using TeamLeave.Domain.Notifications;

namespace TeamLeave.Application.Notifications;

/// <summary>
/// Traitement d'un lot de l'outbox (ADR-03, RB-28) : réservation avec contrôle de version, envoi, mise à jour.
/// Appelé périodiquement par l'expéditeur en tâche de fond ; appelable directement en test.
/// </summary>
public sealed class OutboxProcessor(ITeamLeaveDbContext db, IEmailSender sender, Clock clock, ILogger<OutboxProcessor> logger)
{
    public const int BatchSize = 20;
    public static readonly TimeSpan ReservationDuration = TimeSpan.FromMinutes(2);

    /// <returns>Nombre d'emails traités (envoyés ou en échec) dans ce lot.</returns>
    public async Task<int> DispatchDueAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var due = await db.Notifications
            .Where(n => n.Status == NotificationStatus.Queued
                && n.NextAttemptAtUtc <= now
                && (n.LockedUntilUtc == null || n.LockedUntilUtc <= now))
            .OrderBy(n => n.NextAttemptAtUtc)
            .Take(BatchSize)
            .ToListAsync(ct);
        if (due.Count == 0)
        {
            return 0;
        }

        foreach (var notification in due)
        {
            notification.Reserve(now + ReservationDuration);
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Une autre instance a réservé ces lignes.
            db.ResetTracking();
            return 0;
        }

        foreach (var notification in due)
        {
            try
            {
                await sender.SendAsync(new EmailMessage(notification.RecipientEmail, notification.Subject, notification.Body), ct);
                notification.MarkSent(clock.UtcNow);
                logger.LogInformation("Notification {NotificationId} envoyée.", notification.Id);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                notification.MarkAttemptFailed($"{ex.GetType().Name}: {ex.Message}", clock.UtcNow);
                logger.LogWarning(
                    "Échec d'envoi de la notification {NotificationId} (tentative {Attempt}, statut {Status}).",
                    notification.Id,
                    notification.Attempts,
                    notification.Status);
            }

            await db.SaveChangesAsync(ct);
        }

        db.ResetTracking();
        return due.Count;
    }
}
