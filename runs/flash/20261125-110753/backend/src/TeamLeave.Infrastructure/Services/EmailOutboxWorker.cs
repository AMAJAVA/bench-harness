using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeamLeave.Application.Interfaces;
using TeamLeave.Domain.Enums;
using TeamLeave.Infrastructure.Persistence;

namespace TeamLeave.Infrastructure.Services;

public class EmailOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailOutboxWorker> _logger;
    private readonly IEmailSender _emailSender;

    public EmailOutboxWorker(IServiceScopeFactory scopeFactory, ILogger<EmailOutboxWorker> logger, IEmailSender emailSender)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _emailSender = emailSender;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TeamLeaveDbContext>();
                var pending = await db.EmailOutbox
                    .Where(e => e.Status == EmailStatus.Pending && e.Attempts < 3)
                    .OrderBy(e => e.CreatedAt)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var email in pending)
                {
                    try
                    {
                        await _emailSender.SendAsync(email.Recipient, email.Template, email.Payload, stoppingToken);
                        email.Status = EmailStatus.Sent;
                    }
                    catch (Exception ex)
                    {
                        email.Attempts++;
                        email.ErrorMessage = ex.Message;
                        if (email.Attempts >= 3) email.Status = EmailStatus.Failed;
                        _logger.LogError(ex, "Failed to send email {EmailId} to {Recipient}", email.Id, email.Recipient);
                    }
                    email.LastAttemptAt = DateTimeOffset.UtcNow;
                }

                if (pending.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailOutboxWorker loop error");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
