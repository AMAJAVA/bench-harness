namespace TeamLeave.Domain.Enums;

public static class RoleNames
{
    public const string EMPLOYE = "EMPLOYE";
    public const string MANAGER = "MANAGER";
    public const string ADMIN_RH = "ADMIN_RH";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        EMPLOYE,
        MANAGER,
        ADMIN_RH
    };
}
