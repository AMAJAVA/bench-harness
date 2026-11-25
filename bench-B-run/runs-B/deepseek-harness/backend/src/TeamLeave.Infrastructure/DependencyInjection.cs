using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TeamLeave.Application.Abstractions;
using TeamLeave.Infrastructure.Audit;
using TeamLeave.Infrastructure.Identity;
using TeamLeave.Infrastructure.Notifications;
using TeamLeave.Infrastructure.Persistence;
using TeamLeave.Infrastructure.Services;
using TeamLeave.Infrastructure.Time;

namespace TeamLeave.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTeamLeaveInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TeamLeave")
                               ?? "Host=localhost;Port=5432;Database=teamleave;Username=teamleave;Password=teamleave";

        var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timeout = 5,
            CommandTimeout = 10
        };

        services.AddDbContext<TeamLeaveDbContext>(options =>
            options.UseNpgsql(connectionBuilder.ConnectionString, npgsql =>
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 2,
                    maxRetryDelay: TimeSpan.FromSeconds(2),
                    errorCodesToAdd: null)));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<PasswordHashingOptions>(configuration.GetSection(PasswordHashingOptions.SectionName));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddScoped<AuditWriter>();
        services.AddScoped<BalanceQueryService>();
        services.AddScoped<OverlapService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILeaveRequestService, LeaveRequestService>();
        services.AddScoped<IManagerService, ManagerService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IAdminService, AdminService>();

        var emailOptions = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
        if (emailOptions.Enabled && !string.IsNullOrWhiteSpace(emailOptions.Host))
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }

        services.AddHostedService<NotificationWorker>();

        return services;
    }
}
