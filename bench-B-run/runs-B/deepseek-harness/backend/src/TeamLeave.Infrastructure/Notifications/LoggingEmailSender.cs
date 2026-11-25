using Microsoft.Extensions.Logging;
using TeamLeave.Application.Abstractions;

namespace TeamLeave.Infrastructure.Notifications;

/// <summary>Transport local : journalise l'événement sans exposer de contenu métier sensible.</summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Email simulé (transport local) : sujet={Subject}, destinataire={Recipient}",
            subject,
            Mask(to));
        return Task.CompletedTask;
    }

    private static string Mask(string email)
    {
        var at = email.IndexOf('@');
        return at <= 1 ? "***" : $"{email[0]}***{email[at..]}";
    }
}
