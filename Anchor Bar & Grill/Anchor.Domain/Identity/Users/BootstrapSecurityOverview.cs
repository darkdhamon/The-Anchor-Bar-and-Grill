namespace Anchor.Domain.Identity.Users;

public sealed record BootstrapSecurityOverview(
    int AdminUserCount,
    int ItUserCount,
    int BootstrapAccountCount)
{
    public bool HasMinimumCoverage => AdminUserCount > 0 && ItUserCount > 0;
}
