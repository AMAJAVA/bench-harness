using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Domain.Leave;
using TeamLeave.Domain.Notifications;
using TeamLeave.Domain.Users;

namespace TeamLeave.Application.Common;

/// <summary>
/// Compose les emails et les met en outbox dans la transaction du cas d'usage (RB-21, RB-22, ADR-03).
/// Texte simple en français (H-28). Le demandeur ne reçoit jamais l'analyse de chevauchement (RB-22).
/// </summary>
public sealed class NotificationComposer(ITeamLeaveDbContext db, Clock clock, AppSettings settings)
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public async Task QueueSubmissionAsync(LeaveRequest request, User requester, LeaveType type, CancellationToken ct)
    {
        var body = new StringBuilder()
            .AppendLine("Bonjour,")
            .AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"{requester.FullName} a soumis une demande de congé.")
            .AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"Type : {type.Label}")
            .AppendLine(CultureInfo.InvariantCulture, $"Période : du {Format(request.StartDate)} au {Format(request.EndDate)}")
            .AppendLine(CultureInfo.InvariantCulture, $"Jours décomptés : {request.DaysCount}");
        if (request.SubmissionOverlapAlert)
        {
            // CA-13.2 : l'approbateur est averti de l'alerte calculée à la soumission.
            body.AppendLine().AppendLine("Attention : Chevauchement d'équipe supérieur à 50 %.");
        }

        body.AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"Consulter la demande : {Link(request.Id)}")
            .AppendLine()
            .AppendLine("TeamLeave");

        var subject = $"[TeamLeave] Nouvelle demande de congé de {requester.FullName}";
        foreach (var email in await ApproverEmailsAsync(requester, ct))
        {
            Queue(NotificationEventType.Submission, request.Id, email, subject, body.ToString());
        }
    }

    public void QueueDecision(LeaveRequest request, User requester, LeaveType type)
    {
        var approved = request.Status == LeaveRequestStatus.Approved;
        var body = new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"Bonjour {requester.FirstName},")
            .AppendLine()
            .AppendLine(approved ? "Votre demande de congé a été approuvée." : "Votre demande de congé a été refusée.")
            .AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"Type : {type.Label}")
            .AppendLine(CultureInfo.InvariantCulture, $"Période : du {Format(request.StartDate)} au {Format(request.EndDate)}")
            .AppendLine(CultureInfo.InvariantCulture, $"Jours décomptés : {request.DaysCount}");
        if (!approved)
        {
            body.AppendLine(CultureInfo.InvariantCulture, $"Commentaire : {request.DecisionComment}");
        }

        body.AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"Consulter la demande : {Link(request.Id)}")
            .AppendLine()
            .AppendLine("TeamLeave");

        Queue(
            approved ? NotificationEventType.Approval : NotificationEventType.Rejection,
            request.Id,
            requester.Email,
            approved ? "[TeamLeave] Demande approuvée" : "[TeamLeave] Demande refusée",
            body.ToString());
    }

    /// <summary>Annulation d'une demande approuvée : manager actuel, ou admins RH à défaut (H-13, GAP-11).</summary>
    public async Task QueueApprovedCancellationAsync(LeaveRequest request, User requester, LeaveType type, CancellationToken ct)
    {
        var body = new StringBuilder()
            .AppendLine("Bonjour,")
            .AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"{requester.FullName} a annulé une absence approuvée ; il ou elle sera finalement présent(e).")
            .AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"Type : {type.Label}")
            .AppendLine(CultureInfo.InvariantCulture, $"Période : du {Format(request.StartDate)} au {Format(request.EndDate)}")
            .AppendLine(CultureInfo.InvariantCulture, $"Jours recrédités : {request.DaysCount}")
            .AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"Consulter la demande : {Link(request.Id)}")
            .AppendLine()
            .AppendLine("TeamLeave")
            .ToString();
        var subject = $"[TeamLeave] Annulation d'une absence approuvée de {requester.FullName}";
        foreach (var email in await ApproverEmailsAsync(requester, ct))
        {
            Queue(NotificationEventType.ApprovedCancellation, request.Id, email, subject, body);
        }
    }

    private static string Format(DateOnly date) => date.ToString("dddd d MMMM yyyy", French);

    private async Task<IReadOnlyList<string>> ApproverEmailsAsync(User requester, CancellationToken ct)
    {
        if (requester.ManagerId is { } managerId)
        {
            var managerEmail = await db.Users.AsNoTracking()
                .Where(u => u.Id == managerId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(ct);
            return managerEmail is null ? [] : [managerEmail];
        }

        // RB-21 / CA-13.3 : tous les admins RH actifs sauf le demandeur.
        return await db.Users.AsNoTracking()
            .Where(u => u.IsActive && u.Id != requester.Id && u.Roles.Any(r => r.Role == Role.HrAdmin))
            .OrderBy(u => u.Email)
            .Select(u => u.Email)
            .ToListAsync(ct);
    }

    private string Link(Guid requestId) => $"{settings.PublicBaseUrl.TrimEnd('/')}/demandes/{requestId}";

    private void Queue(NotificationEventType type, Guid requestId, string to, string subject, string body) =>
        db.Notifications.Add(Notification.Queue(Guid.CreateVersion7(), type, requestId, to, subject, body, clock.UtcNow));
}
