using TeamLeave.Domain.Common;

namespace TeamLeave.Domain.Users;

public enum Role
{
    Employee,
    Manager,
    HrAdmin,
}

/// <summary>Rôle cumulatif d'un utilisateur (RB-01), table UserRoles.</summary>
public sealed class UserRole
{
    private UserRole()
    {
    }

    internal UserRole(Guid userId, Role role)
    {
        UserId = userId;
        Role = role;
    }

    public Guid UserId { get; private set; }

    public Role Role { get; private set; }
}

/// <summary>Utilisateur : identité, rôles, rattachement hiérarchique, état d'authentification.</summary>
public sealed class User : IVersioned
{
    public const int MaxFailedLogins = 5;
    public static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly List<UserRole> _roles = [];

    private User()
    {
        Email = string.Empty;
        NormalizedEmail = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        PasswordHash = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; }

    public string NormalizedEmail { get; private set; }

    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public Guid? ManagerId { get; private set; }

    public bool IsActive { get; private set; }

    public string PasswordHash { get; private set; }

    public Guid SecurityStamp { get; private set; }

    public bool MustChangePassword { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTime? FirstFailedLoginAtUtc { get; private set; }

    public DateTime? LockoutEndUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? DeactivatedAtUtc { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyCollection<UserRole> Roles => _roles;

    public string FullName => $"{FirstName} {LastName}";

    public static string Normalize(string email) => email.Trim().ToUpperInvariant();

    public static User Create(
        Guid id,
        string email,
        string firstName,
        string lastName,
        Guid? managerId,
        IEnumerable<Role> roles,
        string passwordHash,
        bool mustChangePassword,
        DateTime nowUtc)
    {
        if (managerId == id)
        {
            throw new DomainException(ErrorCodes.HierarchyCycle, "Un utilisateur ne peut pas être son propre manager.");
        }

        var user = new User
        {
            Id = id,
            ManagerId = managerId,
            IsActive = true,
            PasswordHash = passwordHash,
            SecurityStamp = Guid.NewGuid(),
            MustChangePassword = mustChangePassword,
            CreatedAtUtc = nowUtc,
        };
        user.SetProfile(email, firstName, lastName);
        user.ReplaceRoles(roles);
        return user;
    }

    public bool HasRole(Role role) => _roles.Any(r => r.Role == role);

    public IReadOnlyList<Role> RoleList() => _roles.Select(r => r.Role).OrderBy(r => r).ToList();

    public void SetProfile(string email, string firstName, string lastName)
    {
        Email = email.Trim();
        NormalizedEmail = Normalize(email);
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    /// <summary>Remplace les rôles ; Employé est toujours présent (RB-01). Régénère le tampon si les rôles changent (GAP-07).</summary>
    public bool ReplaceRoles(IEnumerable<Role> roles)
    {
        var target = new HashSet<Role>(roles) { Role.Employee };
        var current = _roles.Select(r => r.Role).ToHashSet();
        if (current.SetEquals(target))
        {
            return false;
        }

        _roles.RemoveAll(r => !target.Contains(r.Role));
        foreach (var role in target.Where(r => !current.Contains(r)))
        {
            _roles.Add(new UserRole(Id, role));
        }

        RegenerateSecurityStamp();
        return true;
    }

    public void SetManager(Guid? managerId)
    {
        if (managerId == Id)
        {
            throw new DomainException(ErrorCodes.HierarchyCycle, "Un utilisateur ne peut pas être son propre manager.");
        }

        ManagerId = managerId;
    }

    public void Deactivate(DateTime nowUtc)
    {
        if (!IsActive)
        {
            throw new DomainException(ErrorCodes.UserAlreadyInactive, "Le compte est déjà désactivé.");
        }

        IsActive = false;
        DeactivatedAtUtc = nowUtc;
        RegenerateSecurityStamp();
    }

    public void SetPassword(string passwordHash, bool mustChangePassword)
    {
        PasswordHash = passwordHash;
        MustChangePassword = mustChangePassword;
        RegenerateSecurityStamp();
    }

    /// <summary>Réinitialisation par l'admin RH : mot de passe temporaire, verrouillage levé (GAP-06).</summary>
    public void ResetPassword(string passwordHash)
    {
        if (!IsActive)
        {
            throw new DomainException(ErrorCodes.UserAlreadyInactive, "Le compte est désactivé.");
        }

        SetPassword(passwordHash, mustChangePassword: true);
        ClearFailures();
    }

    /// <summary>Rehachage transparent (format ancien) sans révoquer les jetons.</summary>
    public void UpgradePasswordHash(string passwordHash) => PasswordHash = passwordHash;

    public bool IsLockedOut(DateTime nowUtc) => LockoutEndUtc is { } end && end > nowUtc;

    /// <summary>Échec de connexion : compteur sur une fenêtre de 15 min, verrouillage au 5e échec (ENF-S5, GAP-19).</summary>
    public void RegisterFailedLogin(DateTime nowUtc)
    {
        if (FirstFailedLoginAtUtc is null || nowUtc - FirstFailedLoginAtUtc.Value > FailureWindow)
        {
            FailedLoginCount = 0;
            FirstFailedLoginAtUtc = nowUtc;
        }

        FailedLoginCount++;
        if (FailedLoginCount >= MaxFailedLogins)
        {
            LockoutEndUtc = nowUtc + LockoutDuration;
            FailedLoginCount = 0;
            FirstFailedLoginAtUtc = null;
        }
    }

    public void RegisterSuccessfulLogin() => ClearFailures();

    public void Touch() => Version++;

    private void ClearFailures()
    {
        FailedLoginCount = 0;
        FirstFailedLoginAtUtc = null;
        LockoutEndUtc = null;
    }

    private void RegenerateSecurityStamp() => SecurityStamp = Guid.NewGuid();
}
