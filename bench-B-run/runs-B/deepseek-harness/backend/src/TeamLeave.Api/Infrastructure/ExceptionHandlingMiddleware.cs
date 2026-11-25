using Npgsql;
using TeamLeave.Application.Common;
using TeamLeave.Domain.Exceptions;

namespace TeamLeave.Api.Infrastructure;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Déconnexion du client : rien à écrire.
        }
        catch (DomainException exception)
        {
            logger.LogWarning("Règle métier non traduite : {Code}", exception.Code);
            await ApiProblem.WriteAsync(context, AppError.Create(
                exception.Code,
                "Règle métier",
                exception.Code == "FORBIDDEN" ? 403 : 409,
                exception.Message));
        }
        catch (BadHttpRequestException exception)
        {
            logger.LogInformation(exception, "Requête HTTP mal formée.");
            await ApiProblem.WriteAsync(
                context,
                AppError.Create(
                    ErrorCodes.VALIDATION_ERROR,
                    "Requête mal formée",
                    400,
                    "Le corps ou les paramètres de la requête sont mal formés."));
        }
        catch (NpgsqlException exception)
        {
            logger.LogError(exception, "Base de données indisponible.");
            await ApiProblem.WriteAsync(context, AppError.Create(
                ErrorCodes.SERVICE_UNAVAILABLE,
                "Service temporairement indisponible",
                503,
                "La base de données est temporairement indisponible. Aucune écriture partielle n'a été appliquée."));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Erreur interne non gérée.");
            await ApiProblem.WriteAsync(context, AppError.Create(
                ErrorCodes.INTERNAL_ERROR,
                "Erreur interne",
                500,
                "Une erreur interne est survenue."));
        }
    }
}
