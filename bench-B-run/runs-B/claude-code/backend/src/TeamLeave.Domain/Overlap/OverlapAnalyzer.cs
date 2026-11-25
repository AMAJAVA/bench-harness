using TeamLeave.Domain.Leave;

namespace TeamLeave.Domain.Overlap;

/// <summary>Taux exact (fraction) : la comparaison au seuil n'utilise jamais l'arrondi (RB-20).</summary>
public readonly record struct Rate(int Absent, int TeamSize)
{
    /// <summary>Strictement supérieur à 50 % : 2 × A &gt; N (RB-16 §5).</summary>
    public bool ExceedsHalf => TeamSize > 0 && 2 * Absent > TeamSize;

    /// <summary>Pourcentage arrondi à l'entier le plus proche, pour l'affichage uniquement (RB-20).</summary>
    public int Percent => TeamSize == 0 ? 0 : (int)Math.Round(100m * Absent / TeamSize, MidpointRounding.AwayFromZero);
}

public sealed record Colleague(Guid Id, string FullName);

/// <summary>Demande d'un membre de l'équipe ; seules les demandes approuvées comptent (RB-16 §3).</summary>
public sealed record TeamAbsence(Guid UserId, DateOnly StartDate, DateOnly EndDate, LeaveRequestStatus Status);

public sealed record OverlapDay(DateOnly Date, Rate Rate, IReadOnlyList<Colleague> AbsentColleagues);

public sealed record OverlapAnalysis(
    bool Applicable,
    int TeamSize,
    bool IsAlert,
    Rate? MaxRate,
    IReadOnlyList<OverlapDay> DaysOverThreshold)
{
    public static OverlapAnalysis NotApplicable { get; } = new(false, 0, false, null, []);
}

/// <summary>
/// Seule implémentation de la règle de chevauchement à 50 % (RB-16, RB-20 ; step2_arch §3.5).
/// Pure : la population et les absences sont fournies par l'appelant.
/// </summary>
public static class OverlapAnalyzer
{
    /// <param name="requesterId">Demandeur E, exclu du numérateur et du dénominateur.</param>
    /// <param name="requesterHasManager">Sans manager, la règle ne s'applique pas.</param>
    /// <param name="evaluatedDays">Jours ouvrés de la demande (RB-07).</param>
    /// <param name="activeTeam">Membres actifs de l'équipe du manager de E (E peut y figurer, il est retiré).</param>
    /// <param name="teamAbsences">Demandes des membres de l'équipe, tous statuts (filtrées ici sur « Approuvée »).</param>
    public static OverlapAnalysis Analyze(
        Guid requesterId,
        bool requesterHasManager,
        IReadOnlyList<DateOnly> evaluatedDays,
        IEnumerable<Colleague> activeTeam,
        IEnumerable<TeamAbsence> teamAbsences)
    {
        if (!requesterHasManager)
        {
            return OverlapAnalysis.NotApplicable;
        }

        var colleagues = activeTeam
            .Where(c => c.Id != requesterId)
            .DistinctBy(c => c.Id)
            .ToDictionary(c => c.Id);
        var teamSize = colleagues.Count;
        if (teamSize == 0)
        {
            return OverlapAnalysis.NotApplicable;
        }

        var approved = teamAbsences
            .Where(a => a.Status == LeaveRequestStatus.Approved && colleagues.ContainsKey(a.UserId))
            .ToList();

        Rate? maxRate = null;
        var daysOver = new List<OverlapDay>();
        foreach (var day in evaluatedDays.Distinct().Order())
        {
            var absentIds = approved
                .Where(a => a.StartDate <= day && day <= a.EndDate)
                .Select(a => a.UserId)
                .Distinct()
                .ToList();
            var rate = new Rate(absentIds.Count, teamSize);
            if (maxRate is null || rate.Absent > maxRate.Value.Absent)
            {
                maxRate = rate;
            }

            if (rate.ExceedsHalf)
            {
                var names = absentIds
                    .Select(id => colleagues[id])
                    .OrderBy(c => c.FullName, StringComparer.CurrentCulture)
                    .ToList();
                daysOver.Add(new OverlapDay(day, rate, names));
            }
        }

        return new OverlapAnalysis(true, teamSize, daysOver.Count > 0, maxRate ?? new Rate(0, teamSize), daysOver);
    }
}
