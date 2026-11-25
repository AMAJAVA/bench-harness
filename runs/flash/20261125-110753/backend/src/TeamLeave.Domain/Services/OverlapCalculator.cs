using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;

namespace TeamLeave.Domain.Services;

public record OverlapResult(int WorkingDays, int OverlapDays, decimal Ratio, bool HasWarning, IReadOnlyList<(DateOnly Date, List<string> AbsentEmployees)> Details);

public static class OverlapCalculator
{
    public static OverlapResult Calculate(
        DateOnly startDate,
        DateOnly endDate,
        Guid employeeId,
        Guid teamId,
        IEnumerable<LeaveRequest> approvedRequests,
        IEnumerable<Holiday> holidays)
    {
        var workingDays = WorkingDaysCalculator.GetWorkingDays(startDate, endDate, holidays).ToList();
        if (workingDays.Count == 0)
        {
            return new OverlapResult(0, 0, 0, false, new List<(DateOnly, List<string>)>());
        }

        var relevantApproved = approvedRequests
            .Where(r => r.Status == LeaveRequestStatus.Approved
                        && r.TeamId == teamId
                        && r.EmployeeId != employeeId)
            .ToList();

        var details = new List<(DateOnly Date, List<string> AbsentEmployees)>();
        var overlapDays = 0;

        foreach (var day in workingDays)
        {
            var absentEmployees = relevantApproved
                .Where(r => r.StartDate <= day && r.EndDate >= day)
                .Select(r => $"{r.Employee.FirstName} {r.Employee.LastName}")
                .Distinct()
                .ToList();

            if (absentEmployees.Count > 0)
            {
                overlapDays++;
                details.Add((day, absentEmployees));
            }
        }

        var ratio = (decimal)overlapDays / workingDays.Count;
        var hasWarning = 2 * overlapDays > workingDays.Count;
        return new OverlapResult(workingDays.Count, overlapDays, ratio, hasWarning, details);
    }
}
