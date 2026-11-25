using TeamLeave.Application.Interfaces;

namespace TeamLeave.Infrastructure.Services;

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
