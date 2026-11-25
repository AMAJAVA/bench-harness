using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TeamLeave.Application.Abstractions;
using TeamLeave.Domain.Common;

namespace TeamLeave.Api.Infrastructure;

/// <summary>Catalogue code → statut HTTP et titre (step2_arch §5.8).</summary>
public static class ErrorCatalog
{
    private static readonly Dictionary<string, (int Status, string Title)> Entries = new()
    {
        [ErrorCodes.ValidationError] = (400, "Requête invalide"),
        [ErrorCodes.RejectionCommentRequired] = (400, "Commentaire de refus obligatoire"),
        [ErrorCodes.RangeTooLong] = (400, "Période trop longue"),
        [ErrorCodes.Unauthenticated] = (401, "Authentification requise"),
        [ErrorCodes.InvalidCredentials] = (401, "Identifiants invalides"),
        [ErrorCodes.Forbidden] = (403, "Accès refusé"),
        [ErrorCodes.SelfApprovalForbidden] = (403, "Auto-décision interdite"),
        [ErrorCodes.NotApprover] = (403, "Vous n'êtes pas l'approbateur"),
        [ErrorCodes.NotRequester] = (403, "Vous n'êtes pas le demandeur"),
        [ErrorCodes.PasswordChangeRequired] = (403, "Changement de mot de passe obligatoire"),
        [ErrorCodes.NotFound] = (404, "Ressource introuvable"),
        [ErrorCodes.InvalidStatusTransition] = (409, "Transition de statut interdite"),
        [ErrorCodes.ConcurrencyConflict] = (409, "Conflit de concurrence"),
        [ErrorCodes.EmailAlreadyUsed] = (409, "Email déjà utilisé"),
        [ErrorCodes.LeaveTypeCodeAlreadyUsed] = (409, "Code de type déjà utilisé"),
        [ErrorCodes.LeaveTypeInUse] = (409, "Type de congé utilisé"),
        [ErrorCodes.SystemLeaveType] = (409, "Type de congé initial"),
        [ErrorCodes.HolidayAlreadyExists] = (409, "Jour férié déjà défini"),
        [ErrorCodes.UserAlreadyInactive] = (409, "Compte déjà désactivé"),
        [ErrorCodes.NoWorkingDay] = (422, "Aucun jour ouvré"),
        [ErrorCodes.PastDateNotAllowed] = (422, "Date passée non autorisée"),
        [ErrorCodes.PeriodTooLong] = (422, "Période trop longue"),
        [ErrorCodes.LeaveTypeInactive] = (422, "Type de congé désactivé"),
        [ErrorCodes.LeaveTypeNotTracked] = (422, "Type non soumis à décompte"),
        [ErrorCodes.InsufficientBalance] = (422, "Solde insuffisant"),
        [ErrorCodes.BalanceBelowPending] = (422, "Solde inférieur aux jours en attente"),
        [ErrorCodes.OverlapsOwnRequest] = (422, "Chevauchement avec une de vos demandes"),
        [ErrorCodes.AlreadyStarted] = (422, "Absence déjà commencée"),
        [ErrorCodes.OverlapNotAcknowledged] = (422, "Chevauchement d'équipe à acquitter"),
        [ErrorCodes.HierarchyCycle] = (422, "Cycle hiérarchique"),
        [ErrorCodes.HasActiveReports] = (422, "Subordonnés actifs"),
        [ErrorCodes.InvalidManager] = (422, "Manager invalide"),
        [ErrorCodes.LastHrAdmin] = (422, "Dernier admin RH actif"),
        [ErrorCodes.LeaveTypeHasBalances] = (422, "Soldes non nuls"),
        [ErrorCodes.LeaveTypeHasPendingRequests] = (422, "Demandes en attente"),
        [ErrorCodes.InvalidCurrentPassword] = (422, "Mot de passe actuel incorrect"),
        [ErrorCodes.AccountLocked] = (429, "Compte temporairement verrouillé"),
        [ErrorCodes.RateLimited] = (429, "Trop de requêtes"),
    };

    public static IReadOnlyCollection<string> Codes => Entries.Keys;

    public static (int Status, string Title) Resolve(string code) =>
        Entries.TryGetValue(code, out var entry) ? entry : (500, "Erreur interne");
}

/// <summary>Écriture homogène des réponses Problem Details (RFC 9457) avec <c>code</c> et <c>traceId</c>.</summary>
public static class Problems
{
    public static ProblemDetails Create(
        HttpContext context,
        string code,
        string? detail,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IDictionary<string, string[]>? errors = null)
    {
        var (status, title) = ErrorCatalog.Resolve(code);
        ProblemDetails problem = errors is null
            ? new ProblemDetails()
            : new ValidationProblemDetails(errors);
        problem.Type = $"https://teamleave/errors/{code.ToLowerInvariant().Replace('_', '-')}";
        problem.Title = title;
        problem.Status = status;
        problem.Detail = detail;
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        if (extensions is not null)
        {
            foreach (var (key, value) in extensions)
            {
                problem.Extensions[key] = value;
            }
        }

        return problem;
    }

    public static async Task WriteAsync(
        HttpContext context,
        string code,
        string? detail,
        IReadOnlyDictionary<string, object?>? extensions = null,
        IDictionary<string, string[]>? errors = null)
    {
        var problem = Create(context, code, detail, extensions, errors);
        context.Response.StatusCode = problem.Status!.Value;
        context.Response.ContentType = "application/problem+json";
        var options = context.RequestServices.GetRequiredService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>().Value.JsonSerializerOptions;
        await JsonSerializer.SerializeAsync(context.Response.Body, problem, problem.GetType(), options, context.RequestAborted);
    }
}

/// <summary>Traduction des exceptions en Problem Details, jamais dans les contrôleurs (step2_arch §3.4).</summary>
public sealed class ProblemExceptionHandler(ILogger<ProblemExceptionHandler> logger, TimeProvider timeProvider) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case ValidationException validation:
                await Problems.WriteAsync(context, validation.Code, validation.Message, null, validation.Errors.ToDictionary(e => e.Key, e => e.Value));
                return true;

            case DomainException domain:
                if (domain.Code == ErrorCodes.AccountLocked && domain.Extensions.TryGetValue("lockoutEndUtc", out var end) && end is DateTime lockoutEnd)
                {
                    var seconds = Math.Max(1, (int)Math.Ceiling((lockoutEnd - timeProvider.GetUtcNow().UtcDateTime).TotalSeconds));
                    context.Response.Headers.RetryAfter = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                await Problems.WriteAsync(context, domain.Code, domain.Message, domain.Extensions);
                return true;

            case UniqueConstraintViolationException unique:
                var (code, message) = unique.ConstraintName switch
                {
                    ConstraintNames.UsersNormalizedEmail => (ErrorCodes.EmailAlreadyUsed, "Cette adresse email est déjà utilisée."),
                    ConstraintNames.LeaveTypesCode => (ErrorCodes.LeaveTypeCodeAlreadyUsed, "Ce code de type est déjà utilisé."),
                    ConstraintNames.PublicHolidaysDate => (ErrorCodes.HolidayAlreadyExists, "Un jour férié existe déjà à cette date."),
                    _ => (ErrorCodes.ConcurrencyConflict, "Conflit avec une modification concurrente."),
                };
                await Problems.WriteAsync(context, code, message);
                return true;

            case BadHttpRequestException badRequest:
                await Problems.WriteAsync(context, ErrorCodes.ValidationError, badRequest.Message);
                return true;

            case OperationCanceledException when context.RequestAborted.IsCancellationRequested:
                return true;

            default:
                logger.LogError(exception, "Erreur non gérée sur {Path}.", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";
                var problem = new ProblemDetails
                {
                    Type = "https://teamleave/errors/internal",
                    Title = "Erreur interne",
                    Status = 500,
                };
                problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
                await context.Response.WriteAsJsonAsync(problem, (JsonSerializerOptions?)null, "application/problem+json", cancellationToken);
                return true;
        }
    }
}
