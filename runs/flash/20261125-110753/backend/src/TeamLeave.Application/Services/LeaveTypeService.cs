using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Exceptions;
using TeamLeave.Application.Interfaces;
using TeamLeave.Domain.Entities;

namespace TeamLeave.Application.Services;

public class LeaveTypeService
{
    private readonly ITeamLeaveDbContext _db;
    private readonly IClock _clock;

    public LeaveTypeService(ITeamLeaveDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<LeaveTypeDto>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = _db.LeaveTypes.AsQueryable();
        if (!includeInactive) query = query.Where(t => t.IsActive);
        var types = await query.OrderBy(t => t.DisplayOrder).ToListAsync(cancellationToken);
        return types.Select(t => new LeaveTypeDto(t.Id, t.Code, t.Label, t.IsActive, t.TrackBalance, t.DisplayOrder)).ToList();
    }

    public async Task<LeaveTypeDto> CreateAsync(CreateLeaveTypeRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.LeaveTypes.AnyAsync(t => t.Code == code, cancellationToken))
            throw new AppException(409, "LEAVE_TYPE_CODE_EXISTS", $"Le code '{code}' existe déjà.");

        var type = new LeaveType
        {
            Id = Guid.NewGuid(),
            Code = code,
            Label = request.Label,
            IsActive = request.IsActive,
            TrackBalance = request.TrackBalance,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow
        };
        _db.LeaveTypes.Add(type);
        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = "LEAVE_TYPE_CREATED",
            EntityType = nameof(LeaveType),
            EntityId = type.Id,
            OccurredAt = _clock.UtcNow,
            Details = $"Code={type.Code};Label={type.Label};IsActive={type.IsActive};TrackBalance={type.TrackBalance}"
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new LeaveTypeDto(type.Id, type.Code, type.Label, type.IsActive, type.TrackBalance, type.DisplayOrder);
    }

    public async Task<LeaveTypeDto> UpdateAsync(Guid id, UpdateLeaveTypeRequest request, Guid actorId, CancellationToken cancellationToken = default)
    {
        var type = await _db.LeaveTypes.FindAsync(new object?[] { id }, cancellationToken)
            ?? throw new AppException(404, "LEAVE_TYPE_NOT_FOUND", "Type de congé introuvable.");

        var before = $"Label={type.Label};IsActive={type.IsActive};TrackBalance={type.TrackBalance};DisplayOrder={type.DisplayOrder}";
        type.Label = request.Label;
        type.IsActive = request.IsActive;
        type.TrackBalance = request.TrackBalance;
        type.DisplayOrder = request.DisplayOrder;
        type.UpdatedAt = _clock.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            Action = "LEAVE_TYPE_UPDATED",
            EntityType = nameof(LeaveType),
            EntityId = type.Id,
            OccurredAt = _clock.UtcNow,
            Details = $"Before: {before}; After: Label={type.Label};IsActive={type.IsActive};TrackBalance={type.TrackBalance};DisplayOrder={type.DisplayOrder}"
        });
        await _db.SaveChangesAsync(cancellationToken);
        return new LeaveTypeDto(type.Id, type.Code, type.Label, type.IsActive, type.TrackBalance, type.DisplayOrder);
    }
}
