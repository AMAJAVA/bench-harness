using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;

namespace TeamLeave.Application.Abstractions;

public interface ICalendarService
{
    Task<Result<CalendarResponse>> GetTeamCalendarAsync(
        Guid callerId,
        IReadOnlyList<string> callerRoles,
        Guid teamId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);
}
