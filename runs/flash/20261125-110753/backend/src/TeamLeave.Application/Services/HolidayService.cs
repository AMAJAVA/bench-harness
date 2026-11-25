using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Exceptions;
using TeamLeave.Application.Interfaces;
using TeamLeave.Domain.Entities;

namespace TeamLeave.Application.Services;

public class HolidayService
{
    private readonly ITeamLeaveDbContext _db;
    private readonly IClock _clock;

    public HolidayService(ITeamLeaveDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<HolidayDto>> GetAsync(int year, CancellationToken cancellationToken = default)
    {
        var start = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);
        var holidays = await _db.Holidays
            .Where(h => h.Date >= start && h.Date <= end)
            .OrderBy(h => h.Date)
            .ToListAsync(cancellationToken);
        return holidays.Select(h => new HolidayDto(h.Id, h.Date, h.Label, h.IsActive)).ToList();
    }

    public async Task<HolidayDto> CreateAsync(CreateHolidayRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (await _db.Holidays.AnyAsync(h => h.Date == request.Date, cancellationToken))
            throw new AppException(409, "HOLIDAY_DATE_EXISTS", $"Un jour férié existe déjà pour le {request.Date}.");

        var holiday = new Holiday
        {
            Id = Guid.NewGuid(),
            Date = request.Date,
            Label = request.Label,
            IsActive = true,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow
        };
        _db.Holidays.Add(holiday);
        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = "HOLIDAY_CREATED",
            EntityType = nameof(Holiday),
            EntityId = holiday.Id,
            OccurredAt = _clock.UtcNow,
            Details = $"Date={holiday.Date};Label={holiday.Label}"
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new HolidayDto(holiday.Id, holiday.Date, holiday.Label, holiday.IsActive);
    }

    public async Task<HolidayDto> UpdateAsync(Guid id, UpdateHolidayRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var holiday = await _db.Holidays.FindAsync(new object?[] { id }, cancellationToken)
            ?? throw new AppException(404, "HOLIDAY_NOT_FOUND", "Jour férié introuvable.");

        var before = $"Label={holiday.Label};IsActive={holiday.IsActive}";
        holiday.Label = request.Label;
        holiday.IsActive = request.IsActive;
        holiday.UpdatedAt = _clock.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = "HOLIDAY_UPDATED",
            EntityType = nameof(Holiday),
            EntityId = holiday.Id,
            OccurredAt = _clock.UtcNow,
            Details = $"Before: {before}; After: Label={holiday.Label};IsActive={holiday.IsActive}"
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new HolidayDto(holiday.Id, holiday.Date, holiday.Label, holiday.IsActive);
    }
}
