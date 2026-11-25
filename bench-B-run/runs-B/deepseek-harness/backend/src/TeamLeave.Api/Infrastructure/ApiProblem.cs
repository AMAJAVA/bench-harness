using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TeamLeave.Application.Common;

namespace TeamLeave.Api.Infrastructure;

public static class ApiProblem
{
    public static IResult Result<T>(Result<T> result, HttpContext context, Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess is not null ? onSuccess(result.Value!) : Results.Ok(result.Value);
        }

        return FromError(result.Error!, context);
    }

    public static IResult FromError(AppError error, HttpContext context)
    {
        return Results.Json(
            Build(error, context),
            statusCode: error.Status,
            contentType: "application/problem+json");
    }

    public static ProblemDetails Build(AppError error, HttpContext context)
    {
        var problem = new ProblemDetails
        {
            Type = $"https://api.teamleave.example/errors/{error.Code.ToLowerInvariant().Replace('_', '-')}",
            Title = error.Title,
            Status = error.Status,
            Detail = error.Detail,
            Instance = context.Request.Path
        };

        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        if (error.Extensions is not null)
        {
            foreach (var (key, value) in error.Extensions)
            {
                problem.Extensions[key] = value;
            }
        }

        return problem;
    }

    public static async Task WriteAsync(HttpContext context, AppError error)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = error.Status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(
            Build(error, context),
            options: null,
            contentType: "application/problem+json",
            cancellationToken: context.RequestAborted);
    }

    public static AppError Unauthenticated(string detail = "Une authentification valide est requise.") =>
        AppError.Create(ErrorCodes.UNAUTHENTICATED, "Authentification requise", 401, detail);

    public static AppError Forbidden(string detail = "Vous n'êtes pas autorisé à effectuer cette action.") =>
        AppError.Create(ErrorCodes.FORBIDDEN, "Action interdite", 403, detail);

    public static AppError PasswordChangeRequired() =>
        AppError.Create(
            ErrorCodes.PASSWORD_CHANGE_REQUIRED,
            "Changement de mot de passe requis",
            403,
            "Vous devez changer votre mot de passe avant d'utiliser l'application.");
}
