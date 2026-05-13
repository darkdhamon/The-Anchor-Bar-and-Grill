namespace Anchor.Domain.Identity.Bootstrap;

public sealed record BootstrapUserSeed(
    string Email,
    string Password,
    bool EmailConfirmed,
    bool MustChangePassword,
    bool IsBootstrapAccount,
    IReadOnlyCollection<string> Roles);
