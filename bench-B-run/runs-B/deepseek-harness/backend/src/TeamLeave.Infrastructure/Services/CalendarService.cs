using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;
using TeamLeave.Domain.Enums;
using TeamLeave.Infrastructure.Mapping;
using TeamLeave.Infrastructure.Persistence;

namespace TeamLeave.Infrastructure.Services;

public sealed class CalendarService(
    TeamLeaveDbContext db,
    IClock clock) : ICalendarService
{
    public async Task<Result<CalendarResponse>> GetTeamCalendarAsync(
        Guid callerId,
        IReadOnlyList<string> callerRoles,
        Guid teamId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var today = clock.TodayInParis;
        var defaultFrom = new DateOnly(today.Year, today.Month, 1);
        var defaultTo = defaultFrom.AddMonths(1).AddDays(-1);

        var start = from ?? defaultFrom;
        var end = to ?? defaultTo;

        if (from is not null && to is null)
        {
            end = new DateOnly(start.Year, start.Month, 1).AddMonths(1).AddDays(-1);
        }
        else if (from is null && to is not null)
        {
            start = new DateOnly(end.Year, end.Month, 1);
        }

        if (end < start)
        {
            return Result<CalendarResponse>.Fail(
                ErrorFactory.Validation("La date de fin doit être postérieure ou égale à la date de début."));
        }

        if (end.DayNumber - start.DayNumber + 1 > 366)
        {
            return Result<CalendarResponse>.Fail(
                ErrorFactory.Unprocessable(
                    ErrorCodes.PERIOD_TOO_LONG,
                    "Période trop longue",
                    "La période du calendrier ne peut pas dépasser 366 jours."));
        }

        var team = await db.Teams.AsNoTracking().SingleOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
        {
            return Result<CalendarResponse>.Fail(ErrorFactory.NotFound("Équipe introuvable."));
        }

        var caller = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == callerId, cancellationToken);
        if (caller is null)
        {
            return Result<CalendarResponse>.Fail(ErrorFactory.NotFound("Utilisateur introuvable."));
        }

        var isAdmin = callerRoles.Contains(RoleNames.ADMIN_RH, StringComparer.Ordinal);
        var isMember = caller.TeamId == teamId;
        var managesMember = await db.Users.AnyAsync(
            u => u.TeamId == teamId && u.ManagerId == callerId && u.IsActive,
            cancellationToken);

        if (!isAdmin && !isMember && !managesMember)
        {
            return Result<CalendarResponse>.Fail(ErrorFactory.NotFound("Équipe introuvable."));
        }

        var requests = await db.LeaveRequests
            .AsNoTracking()
            .Include(r => r.Employee)
            .Include(r => r.LeaveType)
            .Where(r => r.Status == LeaveStatus.APPROVED &&
                        r.StartDate <= end &&
                        r.EndDate >= start &&
                        db.Users.Any(u => u.Id == r.EmployeeId && u.TeamId == teamId && u.IsActive))
            .OrderBy(r => r.StartDate)
            .ThenBy(r => r.Employee!.LastName)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var absences = requests.Select(r =>
        {
            var employee = r.Employee!;
            var showType = isAdmin || r.EmployeeId == callerId || employee.ManagerId == callerId;

            return new CalendarAbsenceDto(
                showType ? r.Id : null,
                employee.Id,
                employee.FullName,
                r.StartDate,
                ContractMapper.FormatHalfDay(r.StartHalfDay),
                r.EndDate,
                ContractMapper.FormatHalfDay(r.EndHalfDay),
                showType ? ContractMapper.Type(r.LeaveType!) : null);
        }).ToList();

        return Result<CalendarResponse>.Ok(new CalendarResponse(
            team.Id,
            team.Name,
            start,
            end,
            absences));
    }
}
