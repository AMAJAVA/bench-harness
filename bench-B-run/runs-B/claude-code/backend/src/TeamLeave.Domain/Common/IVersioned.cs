namespace TeamLeave.Domain.Common;

/// <summary>
/// Entité portant un jeton de version applicatif (concurrence optimiste, step2_arch §6.2 M2/M3, ADR-01).
/// </summary>
public interface IVersioned
{
    int Version { get; }

    /// <summary>« Touch » : incrémente la version sans changer le contenu, pour sérialiser les cas d'usage concurrents.</summary>
    void Touch();
}
