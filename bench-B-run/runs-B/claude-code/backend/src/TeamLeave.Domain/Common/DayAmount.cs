namespace TeamLeave.Domain.Common;

/// <summary>
/// Quantité de jours exprimée en centièmes (2 500 = 25,00 jours). Arithmétique exacte et portable (step2_arch §4.1, RB-10).
/// </summary>
public static class DayAmount
{
    public const int HundredthsPerDay = 100;

    public static int FromDays(int days) => checked(days * HundredthsPerDay);

    /// <summary>Convertit un décimal de l'API en centièmes ; refuse plus de 2 décimales (H-09).</summary>
    public static bool TryFromDecimal(decimal value, out int hundredths)
    {
        hundredths = 0;
        var scaled = value * HundredthsPerDay;
        if (scaled != decimal.Truncate(scaled) || scaled > int.MaxValue || scaled < int.MinValue)
        {
            return false;
        }

        hundredths = (int)scaled;
        return true;
    }

    public static decimal ToDecimal(int hundredths) => hundredths / (decimal)HundredthsPerDay;
}
