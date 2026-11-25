using Microsoft.EntityFrameworkCore;
using TeamLeave.Application.DTOs;
using TeamLeave.Application.Exceptions;
using TeamLeave.Application.Interfaces;

namespace TeamLeave.Application.Services;

public class AuthService
{
    private readonly ITeamLeaveDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(ITeamLeaveDbContext db, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AppException(401, "INVALID_CREDENTIALS", "Email ou mot de passe invalide.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role).ToList();
        var (token, expiresAt) = _jwtTokenGenerator.Generate(user, roles);
        var expiresIn = (int)(expiresAt - DateTimeOffset.UtcNow).TotalSeconds;

        return new AuthResponse(
            token,
            "Bearer",
            expiresIn,
            new UserInfoDto(user.Id, user.Email, user.FirstName, user.LastName, roles, user.TeamId));
    }

    public async Task<MeDto> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .Include(u => u.Team)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null) throw new AppException(404, "USER_NOT_FOUND", "Utilisateur introuvable.");

        return new MeDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.UserRoles.Select(ur => ur.Role).ToList(),
            user.Team == null ? null : new TeamDto(user.Team.Id, user.Team.Name));
    }
}
