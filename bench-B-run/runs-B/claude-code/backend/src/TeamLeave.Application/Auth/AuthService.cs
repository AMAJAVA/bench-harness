using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Users;

namespace TeamLeave.Application.Auth;

/// <summary>Connexion, profil, changement de mot de passe (US-01, step2_arch §5.2 et §7.1).</summary>
public sealed class AuthService(
    ITeamLeaveDbContext db,
    ICurrentUser currentUser,
    Clock clock,
    UseCaseExecutor executor,
    IPasswordHasher hasher,
    ITokenIssuer tokenIssuer,
    BalanceService balances)
{
    private static readonly object DummyLock = new();
    private static string? _dummyHash;

    /// <summary>
    /// §7.1 : verrouillage → hachage → état actif. Le compteur d'échecs est enregistré avant de renvoyer l'erreur.
    /// Compte inconnu, mot de passe faux ou compte inactif : même réponse 401 (CA-01.2, CA-01.3).
    /// </summary>
    public async Task<TokenResponse> LoginAsync(LoginRequest input, CancellationToken ct)
    {
        var normalized = User.Normalize(input.Email!);
        var password = input.Password!;
        var outcome = await executor.ExecuteAsync(
            async token =>
            {
                var user = await db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, token);
                if (user is null)
                {
                    hasher.Verify(DummyHash(), password); // temps de réponse homogène
                    return LoginOutcome.Fail(InvalidCredentials());
                }

                var now = clock.UtcNow;
                if (user.IsLockedOut(now))
                {
                    return LoginOutcome.Fail(new DomainException(
                        ErrorCodes.AccountLocked,
                        $"Trop de tentatives. Réessayez après {Clock.ToParis(user.LockoutEndUtc!.Value):HH:mm} (heure de Paris).",
                        new Dictionary<string, object?> { ["lockoutEndUtc"] = user.LockoutEndUtc }));
                }

                var check = hasher.Verify(user.PasswordHash, password);
                if (check == PasswordCheck.Failed)
                {
                    user.RegisterFailedLogin(now);
                    return LoginOutcome.Fail(InvalidCredentials());
                }

                if (!user.IsActive)
                {
                    return LoginOutcome.Fail(InvalidCredentials());
                }

                user.RegisterSuccessfulLogin();
                if (check == PasswordCheck.SuccessRehashNeeded)
                {
                    user.UpgradePasswordHash(hasher.Hash(password));
                }

                var issued = tokenIssuer.Issue(user);
                return LoginOutcome.Ok(new TokenResponse(issued.AccessToken, "Bearer", issued.ExpiresAtUtc, user.MustChangePassword));
            },
            ct);

        return outcome.Token ?? throw outcome.Error!;
    }

    public async Task<MeDto> GetMeAsync(CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == currentUser.Id, ct)
            ?? throw new UnauthenticatedException();
        PersonRef? manager = null;
        if (user.ManagerId is { } managerId)
        {
            manager = (await db.PeopleAsync([managerId], ct)).GetValueOrDefault(managerId);
        }

        return new MeDto(user.Id, user.Email, user.FirstName, user.LastName, user.RoleList(), manager, user.MustChangePassword);
    }

    /// <summary>PUT /me/password : le tampon est régénéré, le client doit se reconnecter (GAP-06, GAP-07).</summary>
    public async Task ChangePasswordAsync(ChangePasswordRequest input, CancellationToken ct)
    {
        await executor.ExecuteAsync(
            async token =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.Id, token) ?? throw new UnauthenticatedException();
                if (PasswordPolicy.Validate(input.NewPassword, user.Email) is { } policyError)
                {
                    throw new ValidationException("newPassword", policyError);
                }

                if (input.NewPassword == input.CurrentPassword)
                {
                    throw new ValidationException("newPassword", "Le nouveau mot de passe doit être différent de l'actuel.");
                }

                if (hasher.Verify(user.PasswordHash, input.CurrentPassword!) == PasswordCheck.Failed)
                {
                    throw new DomainException(ErrorCodes.InvalidCurrentPassword, "Le mot de passe actuel est incorrect.");
                }

                user.SetPassword(hasher.Hash(input.NewPassword!), mustChangePassword: false);
            },
            ct);
    }

    public Task<IReadOnlyList<BalanceDto>> GetMyBalancesAsync(CancellationToken ct) => balances.GetBalancesAsync(currentUser.Id, ct);

    public Task<PagedResult<MovementDto>> GetMyMovementsAsync(Guid leaveTypeId, PageQuery page, CancellationToken ct) =>
        balances.GetMovementsAsync(currentUser.Id, leaveTypeId, page.Page, page.PageSize, ct);

    private static DomainException InvalidCredentials() =>
        new(ErrorCodes.InvalidCredentials, "Email ou mot de passe incorrect.");

    private string DummyHash()
    {
        lock (DummyLock)
        {
            return _dummyHash ??= hasher.Hash("teamleave-dummy-password-for-timing");
        }
    }

    private sealed record LoginOutcome(TokenResponse? Token, DomainException? Error)
    {
        public static LoginOutcome Ok(TokenResponse token) => new(token, null);

        public static LoginOutcome Fail(DomainException error) => new(null, error);
    }
}
