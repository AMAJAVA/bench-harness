using System.Security.Claims;
using TeamLeave.Application.Abstractions;

namespace TeamLeave.Api.Infrastructure;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue("sub")
                        ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue("email") ?? Principal?.FindFirstValue(ClaimTypes.Email);

    public IReadOnlyList<string> Roles => Principal?
        .FindAll("role")
        .Concat(Principal.FindAll(ClaimTypes.Role))
        .Select(c => c.Value)
        .Distinct(StringComparer.Ordinal)
        .ToList() ?? [];

    public bool MustChangePassword =>
        string.Equals(Principal?.FindFirstValue("mcp"), "true", StringComparison.OrdinalIgnoreCase);

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.Ordinal);
}
