using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TeamLeave.Api.Infrastructure;
using TeamLeave.Application.Auth;
using TeamLeave.Application.Contracts;

namespace TeamLeave.Api.Controllers;

/// <summary>POST /auth/login (US-01, §5.2).</summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimiting.LoginPolicy)]
    public Task<TokenResponse> Login([FromBody] LoginRequest request, CancellationToken ct) => auth.LoginAsync(request, ct);
}

/// <summary>Profil et soldes de l'utilisateur courant (§5.2, P-01, P-02).</summary>
[ApiController]
[Authorize]
[Route("api/v1/me")]
public sealed class MeController(AuthService auth) : ControllerBase
{
    [HttpGet]
    [AllowWhenPasswordChangeRequired]
    public Task<MeDto> Get(CancellationToken ct) => auth.GetMeAsync(ct);

    [HttpPut("password")]
    [AllowWhenPasswordChangeRequired]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await auth.ChangePasswordAsync(request, ct);
        return NoContent();
    }

    [HttpGet("balances")]
    public Task<IReadOnlyList<BalanceDto>> Balances(CancellationToken ct) => auth.GetMyBalancesAsync(ct);

    [HttpGet("balances/{typeId:guid}/movements")]
    public Task<PagedResult<MovementDto>> Movements(Guid typeId, [FromQuery] PageQuery page, CancellationToken ct) =>
        auth.GetMyMovementsAsync(typeId, page, ct);
}
