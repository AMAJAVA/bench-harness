using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace TeamLeave.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.TryParse(id, out var userId)
                ? userId
                : throw new UnauthorizedAccessException("User ID not found in token.");
        }
    }

    protected IReadOnlyList<string> CurrentRoles =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
}
