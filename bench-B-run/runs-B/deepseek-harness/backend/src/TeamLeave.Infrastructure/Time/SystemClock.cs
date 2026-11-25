using TeamLeave.Application.Abstractions;

namespace TeamLeave.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    private static readonly TimeZoneInfo Paris = ResolveParis();

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly TodayInParis =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(UtcNow.UtcDateTime, Paris).Date);

    private static TimeZoneInfo ResolveParis()
    {
        foreach (var id in new[] { "Europe/Paris", "Romance Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Essayer l'identifiant suivant.
            }
        }

        return TimeZoneInfo.Utc;
    }
}
