using TeamLeave.Application.Common;
using TeamLeave.Application.Contracts;

namespace TeamLeave.Application.Abstractions;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<Result<LoginResponse>> RefreshAsync(
        string? refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);

    Task<Result<bool>> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<MeResponse>> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);
}
