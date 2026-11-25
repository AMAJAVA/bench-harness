using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TeamLeave.Application.Abstractions;
using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;
using TeamLeave.Infrastructure.Audit;
using TeamLeave.Infrastructure.Identity;
using TeamLeave.Infrastructure.Persistence;

namespace TeamLeave.Infrastructure.Services;

public sealed class AuthService(
    TeamLeaveDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IClock clock,
    IOptions<JwtOptions> jwtOptions,
    AuditWriter auditWriter) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return InvalidCredentials();
        }

        var user = await db.Users.SingleOrDefaultAsync(
            u => u.Email == email,
            cancellationToken);

        var now = clock.UtcNow;

        if (user is null)
        {
            // Coût comparable à une vérification réelle pour limiter l'énumération par timing.
            _ = await passwordHasher.HashAsync(password, cancellationToken);
            return InvalidCredentials();
        }

        if (!user.IsActive)
        {
            _ = await passwordHasher.HashAsync(password, cancellationToken);
            return InvalidCredentials();
        }

        if (user.LockoutUntil is not null && user.LockoutUntil > now)
        {
            return InvalidCredentials();
        }

        var valid = await passwordHasher.VerifyAsync(password, user.PasswordHash, cancellationToken);
        if (!valid)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= 5)
            {
                user.LockoutUntil = now.AddMinutes(15);
            }

            user.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            return InvalidCredentials();
        }

        user.FailedLoginCount = 0;
        user.LockoutUntil = null;
        user.LastLoginAt = now;
        user.UpdatedAt = now;

        var response = IssueTokens(user, Guid.CreateVersion7(), ipAddress, userAgent, now);
        await db.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Ok(response);
    }

    public async Task<Result<LoginResponse>> RefreshAsync(
        string? refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return InvalidRefresh();
        }

        var now = clock.UtcNow;
        var hash = SecureTokens.Hash(refreshToken);

        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null)
        {
            return InvalidRefresh();
        }

        if (stored.RevokedAt is not null)
        {
            if (stored.RevokedReason == "ROTATION")
            {
                var family = await db.RefreshTokens
                    .Where(t => t.FamilyId == stored.FamilyId && t.RevokedAt == null)
                    .ToListAsync(cancellationToken);

                foreach (var token in family)
                {
                    token.Revoke("REUSE_DETECTED", now);
                }

                await db.SaveChangesAsync(cancellationToken);
                return Result<LoginResponse>.Fail(
                    ErrorCodes.TOKEN_REUSED,
                    "Jeton de rafraîchissement réutilisé",
                    401,
                    "La session a été révoquée.");
            }

            return InvalidRefresh();
        }

        if (stored.ExpiresAt <= now || stored.User is null || !stored.User.IsActive)
        {
            return InvalidRefresh();
        }

        stored.Revoke("ROTATION", now);

        var response = IssueTokens(
            stored.User,
            stored.FamilyId,
            ipAddress,
            userAgent,
            now,
            stored.Id);

        await db.SaveChangesAsync(cancellationToken);
        return Result<LoginResponse>.Ok(response);
    }

    public async Task<Result<bool>> LogoutAsync(
        string? refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<bool>.Ok(true);
        }

        var hash = SecureTokens.Hash(refreshToken);
        var stored = await db.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is not null && stored.RevokedAt is null)
        {
            stored.Revoke("LOGOUT", clock.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result<bool>.Fail(
                ErrorCodes.INVALID_CREDENTIALS,
                "Authentification refusée",
                401,
                "Identifiants invalides.");
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
            !await passwordHasher.VerifyAsync(request.CurrentPassword, user.PasswordHash, cancellationToken))
        {
            return Result<bool>.Fail(
                ErrorCodes.INVALID_CREDENTIALS,
                "Authentification refusée",
                401,
                "Le mot de passe courant est invalide.");
        }

        if (!PasswordPolicy.IsValid(request.NewPassword, out var reason))
        {
            return Result<bool>.Fail(ErrorCodes.PASSWORD_POLICY, "Mot de passe non conforme", 422, reason);
        }

        var now = clock.UtcNow;
        user.PasswordHash = await passwordHasher.HashAsync(request.NewPassword!, cancellationToken);
        user.MustChangePassword = false;
        user.RenewSecurityStamp();
        user.UpdatedAt = now;

        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke("PASSWORD_CHANGED", now);
        }

        auditWriter.Add(
            userId,
            "PASSWORD_CHANGED",
            "USER",
            userId,
            after: new { mustChangePassword = false });

        await db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<MeResponse>> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Team)
            .Include(u => u.Manager)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result<MeResponse>.Fail(ErrorCodes.NOT_FOUND, "Ressource introuvable", 404, "Utilisateur introuvable.");
        }

        return Result<MeResponse>.Ok(new MeResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Roles.ToList(),
            user.Team is null ? null : new MeTeam(user.Team.Id, user.Team.Name),
            user.Manager is null ? null : new MeManager(user.Manager.Id, user.Manager.FirstName, user.Manager.LastName),
            user.MustChangePassword));
    }

    private LoginResponse IssueTokens(
        User user,
        Guid familyId,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset now,
        Guid? replacesTokenId = null)
    {
        var access = tokenService.CreateAccessToken(user);
        var refreshClear = SecureTokens.CreateRefreshToken();
        var refreshExpiresAt = now.AddDays(_jwt.RefreshTokenDays);

        var refresh = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = SecureTokens.Hash(refreshClear),
            FamilyId = familyId,
            ExpiresAt = refreshExpiresAt,
            CreatedAt = now,
            CreatedByIp = ipAddress,
            UserAgent = userAgent
        };

        db.RefreshTokens.Add(refresh);

        if (replacesTokenId is not null)
        {
            var previous = db.RefreshTokens.Local.FirstOrDefault(t => t.Id == replacesTokenId);
            if (previous is not null)
            {
                previous.ReplacedById = refresh.Id;
            }
        }

        return new LoginResponse(
            access.Token,
            "Bearer",
            (int)Math.Max(0, (access.ExpiresAt - now).TotalSeconds),
            refreshClear,
            refreshExpiresAt,
            new UserSummary(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Roles.ToList(),
                user.TeamId,
                user.MustChangePassword));
    }

    private static Result<LoginResponse> InvalidCredentials() =>
        Result<LoginResponse>.Fail(
            ErrorCodes.INVALID_CREDENTIALS,
            "Authentification refusée",
            401,
            "Identifiants invalides.");

    private static Result<LoginResponse> InvalidRefresh() =>
        Result<LoginResponse>.Fail(
            ErrorCodes.INVALID_REFRESH_TOKEN,
            "Jeton de rafraîchissement invalide",
            401,
            "Le jeton de rafraîchissement est invalide ou expiré.");
}
