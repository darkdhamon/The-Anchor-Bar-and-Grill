namespace Anchor.Domain.Identity.Users;

public sealed record ManagedUserSummary(
    string UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    bool EmailConfirmed,
    bool MustChangePassword,
    bool IsBootstrapAccount,
    IReadOnlyCollection<string> Roles)
{
    public string DisplayName =>
        string.Join(
            " ",
            new[] { FirstName, LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()));
}
