using TeamLeave.Domain.Common;
using TeamLeave.Domain.Overlap;

namespace TeamLeave.Domain.Leave;

public enum LeaveRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
}

/// <summary>Demande de congé et sa machine à états (RB-13 à RB-15, RB-18).</summary>
public sealed class LeaveRequest : IVersioned
{
    public const int MaxRequesterCommentLength = 500;
    public const int MaxDecisionCommentLength = 1000;
    public const string DeactivationReason = "Compte désactivé";

    private readonly List<LeaveRequestHistoryEntry> _history = [];

    private LeaveRequest()
    {
    }

    public Guid Id { get; private set; }

    public Guid RequesterId { get; private set; }

    public Guid LeaveTypeId { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    /// <summary>Jours ouvrés figés à la soumission (RB-06).</summary>
    public int DaysCount { get; private set; }

    public string? RequesterComment { get; private set; }

    public LeaveRequestStatus Status { get; private set; }

    public DateTime SubmittedAtUtc { get; private set; }

    public bool SubmissionOverlapAlert { get; private set; }

    public int SubmissionOverlapMaxAbsent { get; private set; }

    public int SubmissionOverlapTeamSize { get; private set; }

    public Guid? DecidedById { get; private set; }

    public DateTime? DecidedAtUtc { get; private set; }

    public string? DecisionComment { get; private set; }

    public Guid? OverlapAcknowledgedById { get; private set; }

    public DateTime? OverlapAcknowledgedAtUtc { get; private set; }

    public int? OverlapAcknowledgedAbsent { get; private set; }

    public int? OverlapAcknowledgedTeamSize { get; private set; }

    public Guid? CancelledById { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public string? CancellationReason { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyCollection<LeaveRequestHistoryEntry> History => _history;

    public DateRange Period => new(StartDate, EndDate);

    public static LeaveRequest Submit(
        Guid id,
        Guid requesterId,
        Guid leaveTypeId,
        DateRange period,
        int daysCount,
        string? comment,
        OverlapAnalysis submissionAnalysis,
        DateTime nowUtc)
    {
        if (daysCount < 1)
        {
            throw new DomainException(ErrorCodes.NoWorkingDay, "La période ne contient aucun jour ouvré.");
        }

        var request = new LeaveRequest
        {
            Id = id,
            RequesterId = requesterId,
            LeaveTypeId = leaveTypeId,
            StartDate = period.Start,
            EndDate = period.End,
            DaysCount = daysCount,
            RequesterComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            Status = LeaveRequestStatus.Pending,
            SubmittedAtUtc = nowUtc,
            SubmissionOverlapAlert = submissionAnalysis.IsAlert,
            SubmissionOverlapMaxAbsent = submissionAnalysis.MaxRate?.Absent ?? 0,
            SubmissionOverlapTeamSize = submissionAnalysis.Applicable ? submissionAnalysis.TeamSize : 0,
        };
        request._history.Add(LeaveRequestHistoryEntry.Create(id, null, LeaveRequestStatus.Pending, requesterId, nowUtc, null));
        return request;
    }

    /// <summary>Normalise et valide un commentaire de refus (RB-15, H-29).</summary>
    public static string NormalizeRejectionComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new DomainException(ErrorCodes.RejectionCommentRequired, "Un commentaire est obligatoire pour refuser une demande.");
        }

        var trimmed = comment.Trim();
        if (trimmed.Length > MaxDecisionCommentLength)
        {
            throw new ValidationException("comment", $"Le commentaire est limité à {MaxDecisionCommentLength} caractères.");
        }

        return trimmed;
    }

    /// <summary>Contrôle d'état avant décision (409, RB-13, RB-15).</summary>
    public void EnsurePending()
    {
        if (Status != LeaveRequestStatus.Pending)
        {
            throw new DomainException(ErrorCodes.InvalidStatusTransition, "La demande n'est plus en attente.");
        }
    }

    /// <summary>
    /// Approbation (RB-12, RB-18). <paramref name="recomputed"/> est l'analyse recalculée au moment de la décision (RB-17).
    /// </summary>
    public void Approve(Guid deciderId, string? comment, OverlapAnalysis recomputed, bool overlapAcknowledged, DateTime nowUtc)
    {
        EnsurePending();
        EnsureNotSelf(deciderId);
        if (recomputed.IsAlert && !overlapAcknowledged)
        {
            throw new DomainException(
                ErrorCodes.OverlapNotAcknowledged,
                "Plus de 50 % de l'équipe est absente sur au moins un jour de la demande. Confirmez l'approbation en connaissance de cause.");
        }

        var normalizedComment = NormalizeOptionalComment(comment);
        if (recomputed.IsAlert && recomputed.MaxRate is { } rate)
        {
            OverlapAcknowledgedById = deciderId;
            OverlapAcknowledgedAtUtc = nowUtc;
            OverlapAcknowledgedAbsent = rate.Absent;
            OverlapAcknowledgedTeamSize = rate.TeamSize;
        }

        Transition(LeaveRequestStatus.Approved, deciderId, nowUtc, normalizedComment);
        DecidedById = deciderId;
        DecidedAtUtc = nowUtc;
        DecisionComment = normalizedComment;
    }

    /// <summary>Refus : commentaire obligatoire, aucun acquittement exigé (RB-15, RB-18, CA-11.4).</summary>
    public void Reject(Guid deciderId, string? comment, DateTime nowUtc)
    {
        var normalized = NormalizeRejectionComment(comment);
        EnsurePending();
        EnsureNotSelf(deciderId);
        Transition(LeaveRequestStatus.Rejected, deciderId, nowUtc, normalized);
        DecidedById = deciderId;
        DecidedAtUtc = nowUtc;
        DecisionComment = normalized;
    }

    /// <summary>
    /// Annulation par le demandeur (RB-14). Renvoie le statut d'origine pour que l'appelant applique les effets (recrédit, email).
    /// </summary>
    public LeaveRequestStatus CancelByRequester(Guid requesterId, DateOnly today, DateTime nowUtc)
    {
        var previous = Status;
        switch (Status)
        {
            case LeaveRequestStatus.Pending:
                break;
            case LeaveRequestStatus.Approved when StartDate > today:
                break;
            case LeaveRequestStatus.Approved:
                throw new DomainException(ErrorCodes.AlreadyStarted, "La demande approuvée a déjà commencé : elle ne peut plus être annulée.");
            default:
                throw new DomainException(ErrorCodes.InvalidStatusTransition, "Une demande refusée ou annulée ne peut pas être annulée.");
        }

        Transition(LeaveRequestStatus.Cancelled, requesterId, nowUtc, null);
        CancelledById = requesterId;
        CancelledAtUtc = nowUtc;
        return previous;
    }

    /// <summary>Annulation par le système à la désactivation du compte (RB-27, CA-17.5).</summary>
    public void CancelBySystem(string reason, DateTime nowUtc)
    {
        EnsurePending();
        Transition(LeaveRequestStatus.Cancelled, null, nowUtc, reason);
        CancelledById = null;
        CancelledAtUtc = nowUtc;
        CancellationReason = reason;
    }

    public void Touch() => Version++;

    private static string? NormalizeOptionalComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        var trimmed = comment.Trim();
        if (trimmed.Length > MaxDecisionCommentLength)
        {
            throw new ValidationException("comment", $"Le commentaire est limité à {MaxDecisionCommentLength} caractères.");
        }

        return trimmed;
    }

    private void EnsureNotSelf(Guid deciderId)
    {
        if (deciderId == RequesterId)
        {
            throw new ForbiddenException(ErrorCodes.SelfApprovalForbidden, "Vous ne pouvez pas décider de votre propre demande.");
        }
    }

    private void Transition(LeaveRequestStatus to, Guid? authorId, DateTime nowUtc, string? comment)
    {
        var from = Status;
        Status = to;
        _history.Add(LeaveRequestHistoryEntry.Create(Id, from, to, authorId, nowUtc, comment));
    }
}

/// <summary>Entrée d'historique de statut, en ajout seul (RB-13, RB-26). AuthorId null = Système.</summary>
public sealed class LeaveRequestHistoryEntry
{
    private LeaveRequestHistoryEntry()
    {
    }

    public long Id { get; private set; }

    public Guid LeaveRequestId { get; private set; }

    public LeaveRequestStatus? FromStatus { get; private set; }

    public LeaveRequestStatus ToStatus { get; private set; }

    public Guid? AuthorId { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public string? Comment { get; private set; }

    internal static LeaveRequestHistoryEntry Create(
        Guid leaveRequestId,
        LeaveRequestStatus? from,
        LeaveRequestStatus to,
        Guid? authorId,
        DateTime nowUtc,
        string? comment) =>
        new()
        {
            LeaveRequestId = leaveRequestId,
            FromStatus = from,
            ToStatus = to,
            AuthorId = authorId,
            OccurredAtUtc = nowUtc,
            Comment = comment,
        };
}
