namespace Anchor.Domain.Identity.Users;

public sealed record ResetManagedUserPasswordRequest(
    string UserId,
    string TemporaryPassword);
