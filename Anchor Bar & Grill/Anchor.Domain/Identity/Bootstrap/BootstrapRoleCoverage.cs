namespace Anchor.Domain.Identity.Bootstrap;

public sealed record BootstrapRoleCoverage(int AdminUserCount, int ItUserCount)
{
    public bool HasMinimumCoverage => AdminUserCount > 0 && ItUserCount > 0;
}
