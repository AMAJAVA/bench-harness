using TeamLeave.Domain.Entities;

namespace TeamLeave.Application.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTimeOffset ExpiresAt) Generate(User user, IEnumerable<string> roles);
}
