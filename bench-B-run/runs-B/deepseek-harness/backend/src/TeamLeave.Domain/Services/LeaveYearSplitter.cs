using TeamLeave.Domain.ValueObjects;

namespace TeamLeave.Domain.Services;

/// <summary>RB-04/RB-06 : répartition d'une demande entre les années civiles concernées.</summary>
public static class LeaveYearSplitter
{
    public static IReadOnlyDictionary<int, int> Split(IEnumerable<LeaveHalfDay> slots)
    {
        var result = new SortedDictionary<int, int>();
        foreach (var slot in slots)
        {
            result[slot.Date.Year] = result.GetValueOrDefault(slot.Date.Year) + 1;
        }

        return result;
    }
}
