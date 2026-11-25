namespace TeamLeave.Application.Abstractions;

/// <summary>Horloge applicative : UTC tronqué à la milliseconde (datetime2(3)) et « aujourd'hui » en Europe/Paris (RB-05).</summary>
public sealed class Clock(TimeProvider timeProvider)
{
    private static readonly TimeZoneInfo Paris = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

    public DateTime UtcNow
    {
        get
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            return new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        }
    }

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(UtcNow, Paris));

    public static DateTime ToParis(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Paris);
}
