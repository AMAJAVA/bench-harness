using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;
using TeamLeave.Infrastructure.Audit;
using TeamLeave.Infrastructure.Persistence;

namespace TeamLeave.Api.Bootstrap;

/// <summary>Commande hors ligne de création du premier Admin RH. Aucun secret n'est journalisé.</summary>
public static class AdminBootstrapper
{
    public static async Task<int> RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var email = Environment.GetEnvironmentVariable("TEAMLEAVE_ADMIN_EMAIL")?.Trim();
        var password = Environment.GetEnvironmentVariable("TEAMLEAVE_ADMIN_PASSWORD");
        var firstName = Environment.GetEnvironmentVariable("TEAMLEAVE_ADMIN_FIRSTNAME")?.Trim() ?? "Admin";
        var lastName = Environment.GetEnvironmentVariable("TEAMLEAVE_ADMIN_LASTNAME")?.Trim() ?? "RH";

        if (string.IsNullOrWhiteSpace(email) ||
            !System.Net.Mail.MailAddress.TryCreate(email, out _))
        {
            Console.Error.WriteLine("TEAMLEAVE_ADMIN_EMAIL est absent ou invalide.");
            return 1;
        }

        if (!PasswordPolicy.IsValid(password, out var reason))
        {
            Console.Error.WriteLine($"TEAMLEAVE_ADMIN_PASSWORD invalide : {reason}");
            return 1;
        }

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TeamLeaveDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var auditWriter = scope.ServiceProvider.GetRequiredService<AuditWriter>();

        if (!await db.Database.CanConnectAsync(cancellationToken))
        {
            Console.Error.WriteLine("La base de données n'est pas joignable. Appliquez database/teamleave.sql.");
            return 1;
        }

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            Console.Error.WriteLine("Un compte utilise déjà cet email.");
            return 1;
        }

        var team = await db.Teams.FirstOrDefaultAsync(t => t.IsActive, cancellationToken);
        if (team is null)
        {
            var now = clock.UtcNow;
            team = new Team { Name = "Direction", IsActive = true, CreatedAt = now, UpdatedAt = now };
            db.Teams.Add(team);
        }

        var user = new User
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PasswordHash = await hasher.HashAsync(password!, cancellationToken),
            Roles = [RoleNames.EMPLOYE, RoleNames.ADMIN_RH],
            TeamId = team.Id,
            Team = team,
            IsActive = true,
            MustChangePassword = true,
            SecurityStamp = Guid.NewGuid(),
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        };

        db.Users.Add(user);
        auditWriter.Add(
            null,
            "ADMIN_BOOTSTRAPPED",
            "USER",
            user.Id,
            after: new { email = user.Email, roles = user.Roles });

        await db.SaveChangesAsync(cancellationToken);
        Console.WriteLine($"Compte Admin RH créé : {user.Email}");
        return 0;
    }
}
