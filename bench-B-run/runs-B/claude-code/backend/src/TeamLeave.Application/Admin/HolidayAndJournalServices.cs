using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;
using TeamLeave.Domain.Calendar;
using TeamLeave.Domain.Common;

namespace TeamLeave.Application.Admin;

/// <summary>Jours fériés (US-18, RB-07, RB-24). Lecture ouverte à tous (GAP-23).</summary>
public sealed class HolidayService(ITeamLeaveDbContext db, UseCaseExecutor executor, AuditWriter audit)
{
    public async Task<IReadOnlyList<HolidayDto>> ListAsync(int year, CancellationToken ct)
    {
        var start = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);
        return await db.PublicHolidays.AsNoTracking()
            .Where(h => h.Date >= start && h.Date <= end)
            .OrderBy(h => h.Date)
            .Select(h => new HolidayDto(h.Date, h.Label))
            .ToListAsync(ct);
    }

    public async Task<HolidayDto> CreateAsync(CreateHolidayRequest input, CancellationToken ct)
    {
        var date = input.Date!.Value;
        return await executor.ExecuteAsync(
            async token =>
            {
                if (await db.PublicHolidays.AnyAsync(h => h.Date == date, token))
                {
                    throw new DomainException(ErrorCodes.HolidayAlreadyExists, "Un jour férié existe déjà à cette date.");
                }

                var holiday = PublicHoliday.Create(date, input.Label!);
                db.PublicHolidays.Add(holiday);
                var dto = new HolidayDto(holiday.Date, holiday.Label);
                audit.Write("HolidayCreated", "PublicHoliday", date.ToString("yyyy-MM-dd"), null, dto);
                return dto;
            },
            ct);
    }

    /// <summary>Les jours décomptés des demandes existantes ne changent pas (RB-06, CA-18.3).</summary>
    public async Task DeleteAsync(DateOnly date, CancellationToken ct)
    {
        await executor.ExecuteAsync(
            async token =>
            {
                var holiday = await db.PublicHolidays.FirstOrDefaultAsync(h => h.Date == date, token) ?? throw new NotFoundException();
                db.PublicHolidays.Remove(holiday);
                audit.Write("HolidayDeleted", "PublicHoliday", date.ToString("yyyy-MM-dd"), new HolidayDto(holiday.Date, holiday.Label), null);
            },
            ct);
    }
}

/// <summary>Journaux consultables par l'admin RH (US-13, US-14, US-19 ; GAP-14 : corps des emails jamais exposé).</summary>
public sealed class JournalService(ITeamLeaveDbContext db)
{
    public Task<PagedResult<NotificationDto>> ListNotificationsAsync(NotificationListQuery query, CancellationToken ct)
    {
        var source = db.Notifications.AsNoTracking();
        if (query.Status is { } status)
        {
            source = source.Where(n => n.Status == status);
        }

        if (query.LeaveRequestId is { } requestId)
        {
            source = source.Where(n => n.LeaveRequestId == requestId);
        }

        return source
            .OrderByDescending(n => n.CreatedAtUtc).ThenByDescending(n => n.Id)
            .Select(n => new NotificationDto(
                n.Id, n.EventType, n.LeaveRequestId, n.RecipientEmail, n.Status, n.Attempts,
                n.CreatedAtUtc, n.LastAttemptAtUtc, n.SentAtUtc, n.LastError))
            .ToPageAsync(query.Page, query.PageSize, ct);
    }

    public async Task<PagedResult<AuditEntryDto>> ListAuditAsync(AuditLogQuery query, CancellationToken ct)
    {
        var source =
            from a in db.AuditLog.AsNoTracking()
            join u in db.Users.AsNoTracking() on a.AuthorId equals u.Id
            select new { Entry = a, u.FirstName, u.LastName };
        if (query.From is { } from)
        {
            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            source = source.Where(x => x.Entry.OccurredAtUtc >= fromUtc);
        }

        if (query.To is { } to)
        {
            var toUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            source = source.Where(x => x.Entry.OccurredAtUtc < toUtc);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            source = source.Where(x => x.Entry.EntityType == query.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            source = source.Where(x => x.Entry.EntityId == query.EntityId);
        }

        if (query.AuthorId is { } authorId)
        {
            source = source.Where(x => x.Entry.AuthorId == authorId);
        }

        var page = await source.OrderByDescending(x => x.Entry.Id).ToPageAsync(query.Page, query.PageSize, ct);
        var items = page.Items.Select(x => new AuditEntryDto(
            x.Entry.Id,
            Mapping.Person(x.Entry.AuthorId, x.FirstName, x.LastName),
            x.Entry.Action,
            x.Entry.EntityType,
            x.Entry.EntityId,
            JsonDocument.Parse(x.Entry.Details).RootElement.Clone(),
            x.Entry.OccurredAtUtc)).ToList();
        return new PagedResult<AuditEntryDto>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
