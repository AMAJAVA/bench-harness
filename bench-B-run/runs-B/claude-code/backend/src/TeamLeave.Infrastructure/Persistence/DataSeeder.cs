using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamLeave.Application.Abstractions;
using TeamLeave.Domain.Calendar;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Users;

namespace TeamLeave.Infrastructure.Persistence;

/// <summary>
/// Amorçage idempotent (step2_arch §4.4) : création du schéma si demandé, types initiaux,
/// jours fériés sur table vide (GAP-16), admin RH initial sur table Users vide (GAP-17).
/// </summary>
public sealed class DataSeeder(
    TeamLeaveDbContext db,
    IOptions<DatabaseOptions> database,
    IOptions<SeedOptions> seed,
    IPasswordHasher hasher,
    Clock clock,
    ILogger<DataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (database.Value.AutoMigrate)
        {
            if (db.Database.IsSqlServer())
            {
                await db.Database.MigrateAsync(ct);
            }
            else
            {
                await db.Database.EnsureCreatedAsync(ct);
            }
        }

        await SeedLeaveTypesAsync(ct);
        await SeedHolidaysAsync(ct);
        await SeedInitialAdminAsync(ct);
        await db.SaveChangesAsync(ct);
        db.ResetTracking();
    }

    private async Task SeedLeaveTypesAsync(CancellationToken ct)
    {
        (Guid Id, string Code, string Label, bool Tracked, bool Retro)[] initial =
        [
            (LeaveType.CpId, "CP", "Congés payés", true, false),
            (LeaveType.RttId, "RTT", "RTT", true, false),
            (LeaveType.SickId, "MALADIE", "Maladie", false, true),
            (LeaveType.OtherId, "AUTRE", "Autre", false, false),
        ];
        var existing = await db.LeaveTypes.Select(t => t.Id).ToListAsync(ct);
        foreach (var t in initial.Where(t => !existing.Contains(t.Id)))
        {
            db.LeaveTypes.Add(LeaveType.Create(t.Id, t.Code, t.Label, t.Tracked, t.Retro, isSystem: true));
        }
    }

    private async Task SeedHolidaysAsync(CancellationToken ct)
    {
        if (!seed.Value.Holidays || await db.PublicHolidays.AnyAsync(ct))
        {
            return;
        }

        var year = clock.Today.Year;
        foreach (var (date, label) in FrenchPublicHolidays.For(year).Concat(FrenchPublicHolidays.For(year + 1)))
        {
            db.PublicHolidays.Add(PublicHoliday.Create(date, label));
        }
    }

    private async Task SeedInitialAdminAsync(CancellationToken ct)
    {
        var settings = seed.Value;
        if (string.IsNullOrWhiteSpace(settings.InitialAdminEmail)
            || string.IsNullOrWhiteSpace(settings.InitialAdminTemporaryPassword)
            || await db.Users.AnyAsync(ct))
        {
            return;
        }

        var admin = User.Create(
            Guid.CreateVersion7(),
            settings.InitialAdminEmail,
            settings.InitialAdminFirstName,
            settings.InitialAdminLastName,
            null,
            [Role.Employee, Role.HrAdmin],
            hasher.Hash(settings.InitialAdminTemporaryPassword),
            mustChangePassword: true,
            clock.UtcNow);
        db.Users.Add(admin);
        foreach (var typeId in new[] { LeaveType.CpId, LeaveType.RttId })
        {
            db.Balances.Add(Balance.CreateEmpty(Guid.CreateVersion7(), admin.Id, typeId));
        }

        logger.LogInformation("Compte admin RH initial créé ({UserId}).", admin.Id);
    }
}
