using System.Text.Json;
using TeamLeave.Application.Contracts;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;
using TeamLeave.Domain.ValueObjects;

namespace TeamLeave.Infrastructure.Mapping;

public static class ContractMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static PersonSummary Person(User user) => new(user.Id, user.FirstName, user.LastName);

    public static PersonSummary? PersonOrNull(User? user) => user is null ? null : Person(user);

    public static LeaveTypeSummary Type(LeaveType type, bool includeColor = true) =>
        new(type.Id, type.Code, type.Label, includeColor ? type.Color : null);

    public static string Status(LeaveStatus status) => status.ToString();

    public static string FormatHalfDay(HalfDay halfDay) => halfDay.ToString();

    public static bool TryParseHalfDay(string? value, out HalfDay halfDay)
    {
        if (string.Equals(value, "AM", StringComparison.OrdinalIgnoreCase))
        {
            halfDay = HalfDay.AM;
            return true;
        }

        if (string.Equals(value, "PM", StringComparison.OrdinalIgnoreCase))
        {
            halfDay = HalfDay.PM;
            return true;
        }

        halfDay = default;
        return false;
    }

    public static bool TryParseStatus(string? value, out LeaveStatus status)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            Enum.GetNames<LeaveStatus>().Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            status = Enum.Parse<LeaveStatus>(value, ignoreCase: true);
            return true;
        }

        status = default;
        return false;
    }

    public static LeaveRequestListItem ToListItem(LeaveRequest request, bool includeOverlapFlag = true) =>
        new(
            request.Id,
            Type(request.LeaveType!),
            request.StartDate,
            FormatHalfDay(request.StartHalfDay),
            request.EndDate,
            FormatHalfDay(request.EndHalfDay),
            request.TotalDays,
            Status(request.Status),
            request.SubmittedAt,
            includeOverlapFlag ? request.OverlapFlagged : null);

    public static LeaveRequestDetail ToDetail(
        LeaveRequest request,
        OverlapDto? overlap = null,
        EmployeeBalanceDto? employeeBalance = null,
        IReadOnlyList<OverlapDto>? overlapHistory = null)
    {
        var employee = request.Employee ?? throw new InvalidOperationException("Employee not loaded.");
        var leaveType = request.LeaveType ?? throw new InvalidOperationException("LeaveType not loaded.");

        var timeline = new List<TimelineEventDto>
        {
            new("SUBMITTED", request.SubmittedAt, Person(employee))
        };

        if (request.DecidedAt is not null)
        {
            var decisionEvent = request.Status == LeaveStatus.REJECTED
                ? "REJECTED"
                : "APPROVED";

            timeline.Add(new TimelineEventDto(
                decisionEvent,
                request.DecidedAt.Value,
                PersonOrNull(request.DecidedBy)));
        }

        if (request.CancelledAt is not null)
        {
            timeline.Add(new TimelineEventDto(
                "CANCELLED",
                request.CancelledAt.Value,
                PersonOrNull(request.CancelledBy)));
        }

        return new LeaveRequestDetail(
            request.Id,
            Person(employee),
            Type(leaveType),
            request.StartDate,
            FormatHalfDay(request.StartHalfDay),
            request.EndDate,
            FormatHalfDay(request.EndHalfDay),
            request.TotalDays,
            Status(request.Status),
            request.Comment,
            request.SubmittedAt,
            PersonOrNull(request.DecidedBy),
            request.DecidedAt,
            request.DecisionComment,
            request.CancelledAt,
            request.CancellationReason,
            request.Allocations
                .OrderBy(a => a.Year)
                .Select(a => new AllocationDto(a.Year, a.HalfDays / 2m))
                .ToList(),
            timeline.OrderBy(e => e.At).ToList(),
            overlap,
            employeeBalance,
            overlapHistory);
    }

    public static OverlapDto ToOverlapDto(
        OverlapResult result,
        Guid teamId,
        DateTimeOffset evaluatedAt,
        AcknowledgementDto? acknowledgement = null) =>
        new(
            result.Applicable,
            result.Active,
            teamId,
            result.TeamSize,
            result.Applicable ? result.MaxRate : null,
            result.BreachingHalfDays
                .OrderBy(s => s)
                .Select(s => new HalfDaySlotDto(s.Date, FormatHalfDay(s.HalfDay)))
                .ToList(),
            evaluatedAt,
            acknowledgement);

    public static OverlapDto ToOverlapDto(
        OverlapEvaluation evaluation,
        User? acknowledgedBy = null,
        DateTimeOffset? acknowledgedAt = null)
    {
        List<HalfDaySlotDto> breaching;
        try
        {
            breaching = JsonSerializer.Deserialize<List<HalfDaySlotDto>>(
                evaluation.BreachingHalfDaysJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            breaching = [];
        }

        AcknowledgementDto? acknowledgement = null;
        if (acknowledgedBy is not null && acknowledgedAt is not null)
        {
            acknowledgement = new AcknowledgementDto(Person(acknowledgedBy), acknowledgedAt.Value);
        }

        return new OverlapDto(
            evaluation.TeamSize >= 2,
            breaching.Count > 0,
            evaluation.TeamId,
            evaluation.TeamSize,
            evaluation.TeamSize >= 2 ? evaluation.MaxRate : null,
            breaching,
            evaluation.EvaluatedAt,
            acknowledgement);
    }

    public static string BreachingJson(OverlapResult result) =>
        JsonSerializer.Serialize(
            result.BreachingHalfDays.OrderBy(s => s)
                .Select(s => new HalfDaySlotDto(s.Date, FormatHalfDay(s.HalfDay))).ToList(),
            JsonOptions);

    public static EmployeeBalanceDto ToEmployeeBalance(AnnualBalance balance) =>
        new(
            balance.Year,
            balance.LeaveTypeId,
            balance.GrantedHalfDays / 2m,
            balance.ConsumedHalfDays / 2m,
            balance.AvailableHalfDays / 2m);

    public static BalanceDto ToBalance(LeaveType type, AnnualBalance? balance) =>
        new(
            type.Id,
            type.Code,
            type.Label,
            type.Color,
            (balance?.GrantedHalfDays ?? 0) / 2m,
            (balance?.ConsumedHalfDays ?? 0) / 2m,
            (balance?.AvailableHalfDays ?? 0) / 2m);
}
