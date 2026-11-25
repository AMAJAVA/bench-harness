namespace TeamLeave.Domain.Common;

/// <summary>
/// Erreur métier portant un code stable (step2_arch §5.8). La couche Api traduit le code en statut HTTP.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string code, string message, IReadOnlyDictionary<string, object?>? extensions = null)
        : base(message)
    {
        Code = code;
        Extensions = extensions ?? new Dictionary<string, object?>();
    }

    public string Code { get; }

    /// <summary>Membres additionnels ajoutés au Problem Details (ex. analyse de chevauchement, fin de verrouillage).</summary>
    public IReadOnlyDictionary<string, object?> Extensions { get; }
}

/// <summary>Ressource inexistante ou hors périmètre de lecture (ENF-S7) : toujours 404.</summary>
public sealed class NotFoundException(string message = "Ressource introuvable.")
    : DomainException(ErrorCodes.NotFound, message);

/// <summary>Action interdite sur une ressource visible (403).</summary>
public sealed class ForbiddenException(string code, string message)
    : DomainException(code, message);

/// <summary>Jeton valide mais utilisateur devenu inactif pendant le cas d'usage (401).</summary>
public sealed class UnauthenticatedException(string message = "Authentification requise.")
    : DomainException(ErrorCodes.Unauthenticated, message);

/// <summary>Erreur de format détectée après la liaison du modèle (400 VALIDATION_ERROR).</summary>
public sealed class ValidationException : DomainException
{
    public ValidationException(string field, string message)
        : base(ErrorCodes.ValidationError, message)
    {
        Errors = new Dictionary<string, string[]> { [field] = [message] };
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
