using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Users;

namespace TeamLeave.Application.Calendar;

/// <summary>Calendrier des absences approuvées (RB-23, US-07, US-12, US-19, step2_arch §5.5).</summary>
public sealed class CalendarService(ITeamLeaveDbContext db, ICurrentUser currentUser)
{
    public const int MaxRangeDays = 93;

    public async Task<CalendarDto> GetAsync(CalendarQuery query, CancellationToken ct)
    {
        var fromDate = query.From!.Value;
        var toDate = query.To!.Value;
        if (fromDate > toDate)
        {
            throw new ValidationException("to", "La date de fin doit être postérieure ou égale à la date de début.");
        }

        if (toDate.DayNumber - fromDate.DayNumber + 1 > MaxRangeDays)
        {
            throw new DomainException(ErrorCodes.RangeTooLong, $"La période demandée dépasse {MaxRangeDays} jours.");
        }

        var me = currentUser.Id;
        var scope = query.Scope ?? "team";
        IQueryable<User> population;
        bool showType;
        switch (scope)
        {
            case "team":
                // RB-03 / GAP-03 : utilisateurs actifs de même manager (moi compris) ; sans manager, moi seul.
                var myManagerId = await db.Users.AsNoTracking().Where(u => u.Id == me).Select(u => u.ManagerId).FirstOrDefaultAsync(ct);
                population = myManagerId is { } managerId
                    ? db.Users.Where(u => u.IsActive && u.ManagerId == managerId)
                    : db.Users.Where(u => u.Id == me);
                showType = false;
                break;
            case "reports":
                if (!currentUser.IsInRole(Role.Manager))
                {
                    throw new ForbiddenException(ErrorCodes.Forbidden, "Réservé aux managers.");
                }

                population = db.Users.Where(u => u.IsActive && u.ManagerId == me);
                showType = true;
                break;
            case "all":
                if (!currentUser.IsInRole(Role.HrAdmin))
                {
                    throw new ForbiddenException(ErrorCodes.Forbidden, "Réservé aux admins RH.");
                }

                population = query.ManagerId is { } filterManager
                    ? db.Users.Where(u => u.ManagerId == filterManager)
                    : db.Users;
                showType = true;
                break;
            default:
                throw new ValidationException("scope", "scope doit valoir team, reports ou all.");
        }

        var rows = await (
                from r in db.LeaveRequests.AsNoTracking()
                join u in population.AsNoTracking() on r.RequesterId equals u.Id
                join t in db.LeaveTypes.AsNoTracking() on r.LeaveTypeId equals t.Id
                where r.Status == LeaveRequestStatus.Approved && r.StartDate <= toDate && r.EndDate >= fromDate
                orderby r.StartDate, u.LastName, u.FirstName
                select new { u.Id, u.FirstName, u.LastName, r.StartDate, r.EndDate, Type = t })
            .ToListAsync(ct);

        var absences = rows
            .Select(x => new CalendarAbsenceDto(
                Mapping.Person(x.Id, x.FirstName, x.LastName),
                x.StartDate,
                x.EndDate,
                showType ? Mapping.TypeRef(x.Type) : null))
            .ToList();
        return new CalendarDto(fromDate, toDate, scope, absences);
    }
}
