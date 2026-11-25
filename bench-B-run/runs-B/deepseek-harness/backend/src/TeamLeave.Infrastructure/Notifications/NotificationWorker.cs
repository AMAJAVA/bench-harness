using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamLeave.Application.Abstractions;
using TeamLeave.Domain.Enums;
using TeamLeave.Infrastructure.Persistence;

namespace TeamLeave.Infrastructure.Notifications;

/// <summary>Worker outbox : l'envoi est hors transaction métier, avec au moins trois tentatives.</summary>
public sealed class NotificationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOptions> options,
    IClock clock,
    ILogger<NotificationWorker> logger) : BackgroundService
{
    private readonly EmailOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Le worker de notifications a échoué ; nouvelle tentative planifiée.");
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TeamLeaveDbContext>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var now = clock.UtcNow;
        var batch = await db.Notifications
            .Where(n => n.Status == NotificationStatus.PENDING &&
                        n.Attempts < _options.MaxAttempts &&
                        n.NextAttemptAt <= now)
            .OrderBy(n => n.NextAttemptAt)
            .ThenBy(n => n.Id)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (batch.Count == 0)
        {
            return;
        }

        foreach (var notification in batch)
        {
            notification.Attempts++;
            notification.NextAttemptAt = now.AddMinutes(2);
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var notification in batch)
        {
            try
            {
                var request = await db.LeaveRequests
                    .AsNoTracking()
                    .Include(r => r.Employee)
                    .SingleAsync(r => r.Id == notification.LeaveRequestId, cancellationToken);

                var employeeName = request.Employee?.FullName ?? "Un employé";
                var status = request.Status.ToString();
                var subject = notification.Type == NotificationType.SOUMISSION
                    ? "Nouvelle demande de congé"
                    : "Décision sur votre demande de congé";

                var body =
                    $"Bonjour,\n\n" +
                    $"{employeeName} — du {request.StartDate:dd/MM/yyyy} au {request.EndDate:dd/MM/yyyy}, " +
                    $"{request.TotalDays:0.#} jour(s), statut {status}.\n\n" +
                    $"Consulter la demande : {_options.AppBaseUrl}/requests/{request.Id}\n\n" +
                    "Ce message ne contient volontairement ni type de congé ni commentaire.";

                await emailSender.SendAsync(notification.RecipientEmail, subject, body, cancellationToken);

                notification.Status = NotificationStatus.SENT;
                notification.SentAt = clock.UtcNow;
                notification.LastError = null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                notification.LastError = "Échec de l'envoi du message.";
                notification.Status = notification.Attempts >= _options.MaxAttempts
                    ? NotificationStatus.FAILED
                    : NotificationStatus.PENDING;

                var delay = notification.Attempts switch
                {
                    1 => TimeSpan.FromMinutes(1),
                    2 => TimeSpan.FromMinutes(5),
                    3 => TimeSpan.FromMinutes(15),
                    _ => TimeSpan.FromHours(1)
                };

                notification.NextAttemptAt = clock.UtcNow.Add(delay);
                logger.LogWarning(
                    "Échec d'envoi de la notification {NotificationId} (tentative {Attempts}).",
                    notification.Id,
                    notification.Attempts);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
