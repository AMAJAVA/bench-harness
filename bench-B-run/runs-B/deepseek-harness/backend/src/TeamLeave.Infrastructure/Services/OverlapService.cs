using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;
using TeamLeave.Domain.Services;
using TeamLeave.Domain.ValueObjects;
using TeamLeave.Infrastructure.Mapping;
using TeamLeave.Infrastructure.Persistence;

namespace TeamLeave.Infrastructure.Services;

public sealed record OverlapContext(
    OverlapResult Result,
    Guid TeamId,
    int TeamSize,
    DateTimeOffset EvaluatedAt);

public sealed class OverlapService(TeamLeaveDbContext db, IClock clock)
{
    public async Task<OverlapContext> EvaluateAsync(
        LeaveRequest request,
        OverlapTriggerKind trigger,
        Guid? actorId,
        bool persist,
        CancellationToken cancellationToken)
    {
        var teamId = await db.Users
            .Where(u => u.Id == request.EmployeeId)
            .Select(u => u.TeamId)
            .SingleAsync(cancellationToken);

        var teamSize = await db.Users.CountAsync(
            u => u.TeamId == teamId && u.IsActive,
            cancellationToken);

        var approved = await db.LeaveRequests
            .AsNoTracking()
            .Where(r => r.Status == LeaveStatus.APPROVED &&
                        r.EmployeeId != request.EmployeeId &&
                        r.Id != request.Id &&
                        r.StartDate <= request.EndDate &&
                        r.EndDate >= request.StartDate)
            .Join(
                db.Users.Where(u => u.TeamId == teamId && u.IsActive),
                r => r.EmployeeId,
                u => u.Id,
                (r, _) => new
                {
                    r.EmployeeId,
                    r.StartDate,
                    r.StartHalfDay,
                    r.EndDate,
                    r.EndHalfDay
                })
            .ToListAsync(cancellationToken);

        var requestedSlots = LeaveDurationCalculator.EnumerateSlots(
            new LeaveHalfDay(request.StartDate, request.StartHalfDay).Ordinal,
            new LeaveHalfDay(request.EndDate, request.EndHalfDay).Ordinal);

        var absences = approved
            .Select(a => new ApprovedAbsence(
                a.EmployeeId,
                LeaveDurationCalculator.EnumerateSlots(
                    new LeaveHalfDay(a.StartDate, a.StartHalfDay).Ordinal,
                    new LeaveHalfDay(a.EndDate, a.EndHalfDay).Ordinal)))
            .ToList();

        var result = OverlapAnalyzer.Analyze(teamSize, requestedSlots, absences);
        var evaluatedAt = clock.UtcNow;

        if (persist)
        {
            db.OverlapEvaluations.Add(new OverlapEvaluation
            {
                LeaveRequestId = request.Id,
                TeamId = teamId,
                TeamSize = teamSize,
                MaxRate = result.MaxRate ?? 0m,
                BreachingHalfDaysJson = ContractMapper.BreachingJson(result),
                TriggerKind = trigger,
                EvaluatedById = actorId,
                EvaluatedAt = evaluatedAt
            });
        }

        return new OverlapContext(result, teamId, teamSize, evaluatedAt);
    }
}
