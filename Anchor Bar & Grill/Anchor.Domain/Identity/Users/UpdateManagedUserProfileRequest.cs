namespace Anchor.Domain.Identity.Users;

public sealed record UpdateManagedUserProfileRequest(
    string UserId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber);
