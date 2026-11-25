using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TeamLeave.Application.Abstractions;
using TeamLeave.Domain.Common;
using TeamLeave.Domain.Users;
using TeamLeave.Infrastructure;
using TeamLeave.Infrastructure.Security;

namespace TeamLeave.Api.Infrastructure;

public static class Policies
{
    public const string ManagerOrHrAdmin = "ManagerOrHrAdmin";
    public const string HrAdmin = "HrAdmin";
}

/// <summary>Marque les endpoints accessibles malgré <c>MustChangePassword</c> (GAP-06) : GET /me, PUT /me/password.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AllowWhenPasswordChangeRequiredAttribute : Attribute;

/// <summary>Utilisateur courant lu dans le jeton validé.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid Id =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue("sub"), out var id)
            ? id
            : throw new UnauthenticatedException();

    public bool IsInRole(Role role) => accessor.HttpContext?.User.IsInRole(role.ToString()) ?? false;
}

/// <summary>
/// Configuration du porteur JWT (§7.1) : HS256 uniquement, iss/aud vérifiés, durée de vie contrôlée avec l'horloge
/// applicative, puis contrôle en base de IsActive et du security stamp à chaque requête (ENF-S2, GAP-07).
/// </summary>
public sealed class ConfigureJwtBearer(IOptions<JwtOptions> jwtOptions, JwtSigningKeys keys, TimeProvider timeProvider)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options) => Configure(options);

    public void Configure(JwtBearerOptions options)
    {
        var settings = jwtOptions.Value;
        var skew = TimeSpan.FromSeconds(settings.ClockSkewSeconds);
        options.MapInboundClaims = false;
        options.TimeProvider = timeProvider;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = keys.ValidationKeys,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateLifetime = true,
            ClockSkew = skew,
            NameClaimType = "sub",
            RoleClaimType = "role",
            LifetimeValidator = (notBefore, expires, _, _) =>
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;
                return expires is { } exp && now <= exp.ToUniversalTime() + skew
                    && (notBefore is not { } nbf || now + skew >= nbf.ToUniversalTime());
            },
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal!;
                if (!Guid.TryParse(principal.FindFirstValue("sub"), out var userId)
                    || !Guid.TryParse(principal.FindFirstValue(JwtTokenIssuer.SecurityStampClaim), out var stamp))
                {
                    context.Fail("Jeton incomplet.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<ITeamLeaveDbContext>();
                var state = await db.Users.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.IsActive, u.SecurityStamp })
                    .FirstOrDefaultAsync(context.HttpContext.RequestAborted);
                if (state is null || !state.IsActive || state.SecurityStamp != stamp)
                {
                    context.Fail("Utilisateur inactif ou jeton révoqué.");
                }
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                await Problems.WriteAsync(context.HttpContext, ErrorCodes.Unauthenticated, "Jeton absent, invalide ou expiré.");
            },
            OnForbidden = context =>
                Problems.WriteAsync(context.HttpContext, ErrorCodes.Forbidden, "Vous n'avez pas le rôle requis pour cette action."),
        };
    }
}

/// <summary>Refus de tout endpoint hors GET /me et PUT /me/password tant que le mot de passe doit être changé (GAP-06).</summary>
public sealed class PasswordChangeRequiredMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (context.User.Identity?.IsAuthenticated == true
            && string.Equals(context.User.FindFirstValue(JwtTokenIssuer.MustChangePasswordClaim), "true", StringComparison.OrdinalIgnoreCase)
            && endpoint is not null
            && endpoint.Metadata.GetMetadata<AllowWhenPasswordChangeRequiredAttribute>() is null
            && endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>() is null)
        {
            await Problems.WriteAsync(context, ErrorCodes.PasswordChangeRequired, "Vous devez changer votre mot de passe avant de continuer.");
            return;
        }

        await next(context);
    }
}
