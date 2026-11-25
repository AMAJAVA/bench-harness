using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TeamLeave.Application.Abstractions;
using TeamLeave.Infrastructure.Email;
using TeamLeave.Infrastructure.Persistence;
using TeamLeave.Infrastructure.Security;

namespace TeamLeave.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTeamLeaveInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.Section));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));
        services.Configure<PasswordHashingOptions>(configuration.GetSection(PasswordHashingOptions.Section));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.Section));
        services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.Section));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.Section));

        services.AddDbContext<TeamLeaveDbContext>((sp, options) =>
        {
            var provider = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.Provider;
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("TeamLeave")
                ?? throw new InvalidOperationException("ConnectionStrings:TeamLeave n'est pas configurée.");
            if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3));
            }

            // Intercepteurs éventuels enregistrés dans le conteneur (tests d'entrelacement déterministe).
            options.AddInterceptors(sp.GetServices<IInterceptor>());
        });
        services.AddScoped<ITeamLeaveDbContext>(sp => sp.GetRequiredService<TeamLeaveDbContext>());
        services.AddScoped<DataSeeder>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<JwtSigningKeys>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

        services.AddSingleton<IEmailSender>(sp =>
        {
            var smtp = sp.GetRequiredService<IOptions<SmtpOptions>>().Value;
            return string.IsNullOrWhiteSpace(smtp.Host)
                ? ActivatorUtilities.CreateInstance<LoggingEmailSender>(sp)
                : ActivatorUtilities.CreateInstance<SmtpEmailSender>(sp);
        });

        if (configuration.GetSection(NotificationOptions.Section).Get<NotificationOptions>()?.DispatcherEnabled ?? true)
        {
            services.AddHostedService<OutboxDispatcher>();
        }

        return services;
    }
}
