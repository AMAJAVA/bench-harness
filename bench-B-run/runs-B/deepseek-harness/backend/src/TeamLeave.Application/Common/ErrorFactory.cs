namespace TeamLeave.Application.Common;

public static class ErrorFactory
{
    public static AppError Validation(string detail, IReadOnlyDictionary<string, string[]>? errors = null)
    {
        var error = AppError.Create(ErrorCodes.VALIDATION_ERROR, "Validation invalide", 422, detail);
        return errors is null ? error : error.With("errors", errors);
    }

    public static AppError NotFound(string detail = "La ressource demandée est introuvable.") =>
        AppError.Create(ErrorCodes.NOT_FOUND, "Ressource introuvable", 404, detail);

    public static AppError Forbidden(string detail = "Vous n'êtes pas autorisé à effectuer cette action.") =>
        AppError.Create(ErrorCodes.FORBIDDEN, "Action interdite", 403, detail);

    public static AppError Conflict(string code, string title, string detail) =>
        AppError.Create(code, title, 409, detail);

    public static AppError Unprocessable(string code, string title, string detail) =>
        AppError.Create(code, title, 422, detail);

    public static AppError InvalidState(string detail = "L'état courant de la ressource ne permet pas cette action.") =>
        AppError.Create(ErrorCodes.INVALID_STATE, "Transition invalide", 409, detail);
}
