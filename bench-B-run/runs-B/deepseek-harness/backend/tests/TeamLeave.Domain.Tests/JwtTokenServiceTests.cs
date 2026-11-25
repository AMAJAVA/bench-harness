using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TeamLeave.Application.Abstractions;
using TeamLeave.Domain.Entities;
using TeamLeave.Domain.Enums;
using TeamLeave.Infrastructure.Identity;
using Xunit;

namespace TeamLeave.Domain.Tests;

public sealed class JwtTokenServiceTests
{
    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 11, 25, 9, 0, 0, TimeSpan.Zero);
        public DateOnly TodayInParis => new(2026, 11, 25);
    }

    private static JwtTokenService CreateService(string key) =>
        new(
            Options.Create(new JwtOptions
            {
                Issuer = "https://api.teamleave.example",
                Audience = "teamleave-api",
                SigningKey = key,
                CurrentKid = "k1",
                AccessTokenMinutes = 15
            }),
            new FixedClock());

    [Fact]
    public void Access_token_contains_identity_roles_and_security_stamp()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "alice@example.test",
            SecurityStamp = Guid.NewGuid(),
            MustChangePassword = false,
            Roles = [RoleNames.EMPLOYE, RoleNames.MANAGER]
        };

        var service = CreateService(new string('k', 64));
        var result = service.CreateAccessToken(user);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal(user.Id.ToString(), token.Claims.Single(c => c.Type == "sub").Value);
        Assert.Contains(token.Claims, c => c.Type == "role" && c.Value == RoleNames.EMPLOYE);
        Assert.Contains(token.Claims, c => c.Type == "role" && c.Value == RoleNames.MANAGER);
        Assert.Equal(user.SecurityStamp.ToString(), token.Claims.Single(c => c.Type == "sv").Value);
        Assert.Equal("k1", token.Header["kid"]);
    }

    [Fact]
    public void Access_token_signature_and_audience_are_valid()
    {
        var key = new string('k', 64);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "alice@example.test",
            SecurityStamp = Guid.NewGuid(),
            Roles = [RoleNames.EMPLOYE]
        };

        var result = CreateService(key).CreateAccessToken(user);
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

        var principal = handler.ValidateToken(result.Token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://api.teamleave.example",
            ValidateAudience = true,
            ValidAudience = "teamleave-api",
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)),
            RoleClaimType = "role",
            NameClaimType = "sub"
        }, out _);

        Assert.Equal(user.Id.ToString(), principal.FindFirst("sub")!.Value);
        Assert.True(principal.IsInRole(RoleNames.EMPLOYE));
    }

    [Fact]
    public void Short_signing_key_is_rejected()
    {
        var service = CreateService("short");
        var user = new User { Email = "alice@example.test", Roles = [RoleNames.EMPLOYE] };

        Assert.Throws<InvalidOperationException>(() => service.CreateAccessToken(user));
    }
}
