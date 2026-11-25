using TeamLeave.Domain.Common;

namespace TeamLeave.Domain.Users;

/// <summary>Informations minimales sur un manager candidat.</summary>
public sealed record ManagerCandidate(Guid Id, bool IsActive, bool HasManagerRole, Guid? ManagerId);

/// <summary>Validation du rattachement hiérarchique (RB-03, GAP-04).</summary>
public static class HierarchyValidator
{
    /// <summary>
    /// Vérifie qu'on peut rattacher <paramref name="userId"/> à <paramref name="manager"/>.
    /// Le cycle est contrôlé avant la validité du manager (CA-17.3).
    /// </summary>
    /// <param name="managerOf">Renvoie le manager actuel d'un utilisateur (ascendants), ou null.</param>
    public static void Validate(Guid userId, ManagerCandidate manager, Func<Guid, Guid?> managerOf)
    {
        if (manager.Id == userId)
        {
            throw new DomainException(ErrorCodes.HierarchyCycle, "Un utilisateur ne peut pas être son propre manager.");
        }

        var visited = new HashSet<Guid>();
        Guid? cursor = manager.ManagerId;
        while (cursor is { } current && visited.Add(current))
        {
            if (current == userId)
            {
                throw new DomainException(ErrorCodes.HierarchyCycle, "Ce rattachement créerait un cycle hiérarchique.");
            }

            cursor = managerOf(current);
        }

        if (!manager.IsActive || !manager.HasManagerRole)
        {
            throw new DomainException(ErrorCodes.InvalidManager, "Le manager doit être un utilisateur actif ayant le rôle Manager.");
        }
    }
}
