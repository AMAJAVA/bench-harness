using Microsoft.Extensions.Logging;
using TeamLeave.Application.Interfaces;

namespace TeamLeave.Infrastructure.Services;

public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) => _logger = logger;

    public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email sent to {Recipient}: {Subject} - {Body}", recipient, subject, body);
        return Task.CompletedTask;
    }
}
