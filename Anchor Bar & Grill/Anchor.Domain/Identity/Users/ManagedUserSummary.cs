namespace Anchor.Domain.Identity.Users;

public sealed record ManagedUserSummary(
    string UserId,
    string Email,
    bool EmailConfirmed,
    bool MustChangePassword,
    bool IsBootstrapAccount,
    IReadOnlyCollection<string> Roles);
