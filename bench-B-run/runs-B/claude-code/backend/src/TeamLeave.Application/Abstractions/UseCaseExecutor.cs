using Microsoft.EntityFrameworkCore;
using TeamLeave.Domain.Common;

namespace TeamLeave.Application.Abstractions;

/// <summary>
/// Unité de travail avec rejeu complet du cas d'usage sur conflit de concurrence (step2_arch §6.2 M4, §6.4, ADR-01).
/// Le cas d'usage relit et recontrôle tout à chaque tentative : le perdant d'une course reçoit l'erreur métier exacte.
/// </summary>
public sealed class UseCaseExecutor(ITeamLeaveDbContext db)
{
    public const int MaxAttempts = 3;

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> useCase, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var result = await useCase(cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
                return result;
            }
            catch (Exception ex) when (IsRetryable(ex))
            {
                db.ResetTracking();
                if (attempt >= MaxAttempts)
                {
                    throw new DomainException(
                        ErrorCodes.ConcurrencyConflict,
                        "L'opération est entrée en conflit avec une modification concurrente. Réessayez.");
                }

                if (attempt > 1)
                {
                    await Task.Delay(Random.Shared.Next(10, 51), cancellationToken);
                }
            }
            catch
            {
                db.ResetTracking();
                throw;
            }
        }
    }

    public Task ExecuteAsync(Func<CancellationToken, Task> useCase, CancellationToken cancellationToken) =>
        ExecuteAsync<bool>(
            async ct =>
            {
                await useCase(ct);
                return true;
            },
            cancellationToken);

    /// <summary>
    /// Conflits rejoués : jeton de version (M2) ; doublon de solde « lire ou créer » (S8) ; garde-fou « un Débit/Recrédit
    /// par demande » (M5), qui peut se déclencher avant le contrôle de version selon l'ordre des commandes du lot :
    /// le rejeu restitue alors l'erreur métier exacte (409 INVALID_STATUS_TRANSITION, CA-10.3).
    /// </summary>
    private static bool IsRetryable(Exception ex) =>
        ex is DbUpdateConcurrencyException
        || ex is UniqueConstraintViolationException
        {
            ConstraintName: ConstraintNames.BalancesUserLeaveType or ConstraintNames.BalanceMovementsRequestKind,
        };
}
