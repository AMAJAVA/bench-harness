using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Overlap;

namespace TeamLeave.Application.Common;

/// <summary>
/// Charge la population et les absences approuvées de l'équipe, puis délègue le calcul à <see cref="OverlapAnalyzer"/> (RB-16, RB-17).
/// </summary>
public sealed class OverlapService(ITeamLeaveDbContext db, HolidayCalendar holidays)
{
    public async Task<OverlapAnalysis> AnalyzeAsync(
        Guid requesterId,
        Guid? managerId,
        DateRange period,
        IReadOnlyList<DateOnly>? workingDays,
        CancellationToken ct)
    {
        if (managerId is not { } manager)
        {
            return OverlapAnalysis.NotApplicable;
        }

        var team = await db.Users.AsNoTracking()
            .Where(u => u.ManagerId == manager && u.IsActive && u.Id != requesterId)
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToListAsync(ct);
        if (team.Count == 0)
        {
            return OverlapAnalysis.NotApplicable;
        }

        var teamIds = team.Select(t => t.Id).ToList();
        var absences = await db.LeaveRequests.AsNoTracking()
            .Where(r => r.Status == LeaveRequestStatus.Approved
                && teamIds.Contains(r.RequesterId)
                && r.StartDate <= period.End
                && r.EndDate >= period.Start)
            .Select(r => new TeamAbsence(r.RequesterId, r.StartDate, r.EndDate, r.Status))
            .ToListAsync(ct);

        var days = workingDays ?? await holidays.WorkingDaysAsync(period, ct);
        return OverlapAnalyzer.Analyze(
            requesterId,
            requesterHasManager: true,
            days,
            team.Select(t => new Colleague(t.Id, $"{t.FirstName} {t.LastName}")),
            absences);
    }

    public Task<OverlapAnalysis> AnalyzeAsync(LeaveRequest request, Guid? requesterManagerId, CancellationToken ct) =>
        AnalyzeAsync(request.RequesterId, requesterManagerId, request.Period, null, ct);
}
