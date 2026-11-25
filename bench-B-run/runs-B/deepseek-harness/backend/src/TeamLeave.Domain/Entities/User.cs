using TeamLeave.Domain.Enums;

namespace TeamLeave.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [RoleNames.EMPLOYE];
    public Guid TeamId { get; set; }
    public Team? Team { get; set; }
    public Guid? ManagerId { get; set; }
    public User? Manager { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
    public Guid SecurityStamp { get; set; } = Guid.NewGuid();
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockoutUntil { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public bool HasRole(string role) => Roles.Contains(role, StringComparer.Ordinal);

    public void SetRoles(IEnumerable<string> roles)
    {
        var normalized = roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (!normalized.Contains(RoleNames.EMPLOYE, StringComparer.Ordinal))
        {
            normalized.Insert(0, RoleNames.EMPLOYE);
        }

        Roles = normalized;
    }

    public void RenewSecurityStamp() => SecurityStamp = Guid.NewGuid();
}
