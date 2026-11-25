using TeamLeave.Domain.Enums;
using TeamLeave.Domain.Exceptions;

namespace TeamLeave.Domain.Entities;

public sealed class LeaveRequest
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid EmployeeId { get; set; }
    public User? Employee { get; set; }
    public Guid LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }
    public DateOnly StartDate { get; set; }
    public HalfDay StartHalfDay { get; set; }
    public DateOnly EndDate { get; set; }
    public HalfDay EndHalfDay { get; set; }
    public int TotalHalfDays { get; set; }
    public LeaveStatus Status { get; set; } = LeaveStatus.PENDING;
    public string? Comment { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public Guid? DecidedById { get; set; }
    public User? DecidedBy { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? DecisionComment { get; set; }
    public bool OverlapFlagged { get; set; }
    public DateTimeOffset? OverlapEvaluatedAt { get; set; }
    public Guid? AcknowledgedById { get; set; }
    public User? AcknowledgedBy { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public Guid? CancelledById { get; set; }
    public User? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<LeaveRequestAllocation> Allocations { get; set; } = [];

    public decimal TotalDays => TotalHalfDays / 2m;

    public void Approve(Guid managerId, DateTimeOffset at, string? comment, bool overlapActive, bool overlapAcknowledged)
    {
        if (Status != LeaveStatus.PENDING)
        {
            throw new DomainException("INVALID_STATE", "Seule une demande en attente peut être approuvée.");
        }

        if (managerId == EmployeeId)
        {
            throw new DomainException("FORBIDDEN", "Un utilisateur ne peut pas approuver sa propre demande.");
        }

        if (overlapActive && !overlapAcknowledged)
        {
            throw new DomainException("OVERLAP_ACK_REQUIRED", "Le signal de chevauchement doit être acquitté.");
        }

        Status = LeaveStatus.APPROVED;
        DecidedById = managerId;
        DecidedAt = at;
        DecisionComment = NormalizeComment(comment);
        OverlapFlagged = overlapActive;
        OverlapEvaluatedAt = at;
        UpdatedAt = at;
        Version++;

        if (overlapActive && overlapAcknowledged)
        {
            AcknowledgedById = managerId;
            AcknowledgedAt = at;
        }
    }

    public void Reject(Guid managerId, DateTimeOffset at, string? comment)
    {
        if (Status != LeaveStatus.PENDING)
        {
            throw new DomainException("INVALID_STATE", "Seule une demande en attente peut être refusée.");
        }

        if (managerId == EmployeeId)
        {
            throw new DomainException("FORBIDDEN", "Un utilisateur ne peut pas refuser sa propre demande.");
        }

        var normalized = NormalizeComment(comment);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new DomainException("COMMENT_REQUIRED", "Un commentaire est obligatoire pour un refus.");
        }

        Status = LeaveStatus.REJECTED;
        DecidedById = managerId;
        DecidedAt = at;
        DecisionComment = normalized;
        UpdatedAt = at;
        Version++;
    }

    public void Cancel(Guid actorId, DateTimeOffset at, string reason)
    {
        if (Status is not (LeaveStatus.PENDING or LeaveStatus.APPROVED))
        {
            throw new DomainException("INVALID_STATE", "Cette demande ne peut pas être annulée.");
        }

        Status = LeaveStatus.CANCELLED;
        CancelledById = actorId;
        CancelledAt = at;
        CancellationReason = reason;
        UpdatedAt = at;
        Version++;
    }

    public void RecordOverlap(bool active, DateTimeOffset evaluatedAt)
    {
        OverlapFlagged = active;
        OverlapEvaluatedAt = evaluatedAt;
    }

    private static string? NormalizeComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        return comment.Trim();
    }
}
