using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Contracts;
using TeamLeave.Domain.Audit;
using TeamLeave.Domain.Calendar;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Overlap;

namespace TeamLeave.Application.Common;

/// <summary>Paramètres applicatifs (lien des emails, §9.3 App:PublicBaseUrl).</summary>
public sealed class AppSettings
{
    public string PublicBaseUrl { get; set; } = "https://localhost";
}

internal static class Mapping
{
    public static PersonRef Person(Guid id, string firstName, string lastName) => new(id, $"{firstName} {lastName}");

    public static LeaveTypeRef TypeRef(LeaveType t) => new(t.Id, t.Code, t.Label);

    public static RateDto ToDto(this Rate rate) => new(rate.Absent, rate.TeamSize, rate.Percent);

    public static LeaveTypeDto ToDto(this LeaveType t) =>
        new(t.Id, t.Code, t.Label, t.IsTracked, t.AllowsRetroactive, t.IsSystem, t.IsActive);

    public static OverlapAnalysisDto ToDto(this OverlapAnalysis analysis, Guid leaveRequestId, DateTime computedAtUtc) =>
        new(
            leaveRequestId,
            computedAtUtc,
            analysis.Applicable,
            analysis.TeamSize,
            analysis.IsAlert,
            analysis.Applicable ? analysis.MaxRate?.ToDto() : null,
            analysis.DaysOverThreshold
                .Select(d => new OverlapDayDto(d.Date, d.Rate.ToDto(), d.AbsentColleagues.Select(c => new PersonRef(c.Id, c.FullName)).ToList()))
                .ToList());

    public static OverlapAlertDto ToAlert(this OverlapAnalysis analysis) =>
        new(analysis.IsAlert, analysis.Applicable ? analysis.MaxRate?.Percent ?? 0 : 0);

    public static async Task<PagedResult<T>> ToPageAsync<T>(this IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<T>(items, page, pageSize, total);
    }

    public static async Task<Dictionary<Guid, PersonRef>> PeopleAsync(this ITeamLeaveDbContext db, IEnumerable<Guid> ids, CancellationToken ct)
    {
        var distinct = ids.Distinct().ToList();
        if (distinct.Count == 0)
        {
            return [];
        }

        return await db.Users.AsNoTracking()
            .Where(u => distinct.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToDictionaryAsync(u => u.Id, u => Person(u.Id, u.FirstName, u.LastName), ct);
    }

    public static string Days(decimal value) => value.ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));
}

/// <summary>Écriture du journal d'administration (RB-26). Jamais de secret dans les détails.</summary>
public sealed class AuditWriter(ITeamLeaveDbContext db, ICurrentUser currentUser, Clock clock)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public void Write(string action, string entityType, string entityId, object? before, object? after) =>
        db.AuditLog.Add(AuditEntry.Create(
            currentUser.Id,
            action,
            entityType,
            entityId,
            JsonSerializer.Serialize(new { before, after }, JsonOptions),
            clock.UtcNow));
}

/// <summary>Accès aux jours fériés (RB-07).</summary>
public sealed class HolidayCalendar(ITeamLeaveDbContext db)
{
    public async Task<IReadOnlySet<DateOnly>> ForPeriodAsync(DateRange period, CancellationToken ct)
    {
        var dates = await db.PublicHolidays.AsNoTracking()
            .Where(h => h.Date >= period.Start && h.Date <= period.End)
            .Select(h => h.Date)
            .ToListAsync(ct);
        return dates.ToHashSet();
    }

    public async Task<IReadOnlyList<DateOnly>> WorkingDaysAsync(DateRange period, CancellationToken ct) =>
        WorkingDayCalculator.WorkingDays(period, await ForPeriodAsync(period, ct));
}

internal static class Errors
{
    public static DomainException Domain(string code, string message) => new(code, message);
}
