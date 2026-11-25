using TeamLeave.Domain.ValueObjects;

namespace TeamLeave.Domain.Services;

/// <summary>
/// RB-12 : taux d'absence projeté par demi-journée, demande incluse.
/// T(h) = (A(h) + 1) / N ; signal si T(h) &gt; 0,50 et N &gt;= 2.
/// </summary>
public static class OverlapAnalyzer
{
    public static OverlapResult Analyze(
        int teamSize,
        IEnumerable<LeaveHalfDay> requestedHalfDays,
        IEnumerable<ApprovedAbsence> otherApprovedAbsences)
    {
        var requested = requestedHalfDays.Distinct().OrderBy(s => s).ToList();
        if (teamSize < 2 || requested.Count == 0)
        {
            return OverlapResult.NotApplicable(teamSize);
        }

        var absentEmployeesBySlot = new Dictionary<LeaveHalfDay, HashSet<Guid>>();
        foreach (var slot in requested)
        {
            absentEmployeesBySlot[slot] = [];
        }

        foreach (var absence in otherApprovedAbsences)
        {
            foreach (var slot in absence.HalfDays.Distinct())
            {
                if (absentEmployeesBySlot.TryGetValue(slot, out var employees))
                {
                    employees.Add(absence.EmployeeId);
                }
            }
        }

        var breaching = new List<LeaveHalfDay>();
        decimal? maxRate = null;

        foreach (var slot in requested)
        {
            var approvedOthers = absentEmployeesBySlot[slot].Count;
            var rate = (approvedOthers + 1m) / teamSize;
            var rounded = Math.Round(rate, 4, MidpointRounding.AwayFromZero);

            maxRate = maxRate is null || rounded > maxRate ? rounded : maxRate;

            if (rate > 0.5m)
            {
                breaching.Add(slot);
            }
        }

        return new OverlapResult(
            Applicable: true,
            Active: breaching.Count > 0,
            TeamSize: teamSize,
            MaxRate: maxRate,
            BreachingHalfDays: breaching);
    }
}
