namespace Anchor.Domain.Identity.Users;

public sealed record CreateManagedUserRequest(
    string Email,
    string TemporaryPassword,
    bool EmailConfirmed);
