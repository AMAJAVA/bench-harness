using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Notifications;

namespace TeamLeave.Infrastructure.Email;

/// <summary>
/// Envoi SMTP STARTTLS (relais Azure Communication Services, §2.5) avec la BCL <see cref="SmtpClient"/>
/// pour rester sur des paquets Microsoft officiels (MailKit écarté, voir step3_backend.md).
/// </summary>
public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl };
        if (!string.IsNullOrEmpty(settings.Username))
        {
            client.Credentials = new NetworkCredential(settings.Username, settings.Password);
        }

        using var mail = new MailMessage(settings.From, message.To, message.Subject, message.Body) { IsBodyHtml = false };
        await client.SendMailAsync(mail, cancellationToken);
    }
}

/// <summary>Expéditeur de développement : journalise l'envoi sans contenu (ENF-S9) quand aucun hôte SMTP n'est configuré.</summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("SMTP non configuré : email « {Subject} » simulé.", message.Subject);
        return Task.CompletedTask;
    }
}

/// <summary>Expéditeur d'emails en tâche de fond (ADR-03) : relève l'outbox toutes les N secondes.</summary>
public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationOptions> options,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, options.Value.PollIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
                while (await processor.DispatchDueAsync(stoppingToken) == OutboxProcessor.BatchSize)
                {
                    // Lot plein : on enchaîne sans attendre.
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Erreur de l'expéditeur d'emails.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
