namespace Anchor.Domain.Identity;

public static class ApplicationRoles
{
    public const string Admin = "Admin";
    public const string EventManager = "EventManager";
    public const string MenuManager = "MenuManager";
    public const string It = "IT";

    public static readonly IReadOnlyList<string> All =
    [
        Admin,
        EventManager,
        MenuManager,
        It
    ];

    public static bool IsManagedRole(string roleName) =>
        All.Contains(roleName, StringComparer.Ordinal);
}
