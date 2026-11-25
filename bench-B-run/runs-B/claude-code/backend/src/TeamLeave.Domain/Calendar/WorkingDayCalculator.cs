using TeamLeave.Domain.Common;

namespace TeamLeave.Domain.Calendar;

/// <summary>Jour férié géré par l'admin RH (RB-07, RB-24).</summary>
public sealed class PublicHoliday
{
    private PublicHoliday()
    {
        Label = string.Empty;
    }

    public DateOnly Date { get; private set; }

    public string Label { get; private set; }

    public static PublicHoliday Create(DateOnly date, string label)
    {
        if (string.IsNullOrWhiteSpace(label) || label.Trim().Length > 100)
        {
            throw new ValidationException("label", "Le libellé est obligatoire (100 caractères au plus).");
        }

        return new PublicHoliday { Date = date, Label = label.Trim() };
    }
}

/// <summary>Décompte des jours ouvrés (RB-06, RB-07) : lundi à vendredi, hors jours fériés.</summary>
public static class WorkingDayCalculator
{
    public static bool IsWorkingDay(DateOnly day, IReadOnlySet<DateOnly> holidays) =>
        day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !holidays.Contains(day);

    public static IReadOnlyList<DateOnly> WorkingDays(DateRange period, IReadOnlySet<DateOnly> holidays) =>
        period.Days().Where(d => IsWorkingDay(d, holidays)).ToList();
}

/// <summary>Jours fériés légaux de France métropolitaine (amorçage, GAP-16).</summary>
public static class FrenchPublicHolidays
{
    public static IReadOnlyList<(DateOnly Date, string Label)> For(int year)
    {
        var easter = EasterSunday(year);
        return
        [
            (new DateOnly(year, 1, 1), "Jour de l'an"),
            (easter.AddDays(1), "Lundi de Pâques"),
            (new DateOnly(year, 5, 1), "Fête du travail"),
            (new DateOnly(year, 5, 8), "Victoire 1945"),
            (easter.AddDays(39), "Ascension"),
            (easter.AddDays(50), "Lundi de Pentecôte"),
            (new DateOnly(year, 7, 14), "Fête nationale"),
            (new DateOnly(year, 8, 15), "Assomption"),
            (new DateOnly(year, 11, 1), "Toussaint"),
            (new DateOnly(year, 11, 11), "Armistice 1918"),
            (new DateOnly(year, 12, 25), "Noël"),
        ];
    }

    /// <summary>Algorithme de Meeus/Jones/Butcher (calendrier grégorien).</summary>
    public static DateOnly EasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = ((19 * a) + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + (2 * e) + (2 * i) - h - k) % 7;
        var m = (a + (11 * h) + (22 * l)) / 451;
        var month = (h + l - (7 * m) + 114) / 31;
        var day = ((h + l - (7 * m) + 114) % 31) + 1;
        return new DateOnly(year, month, day);
    }
}
