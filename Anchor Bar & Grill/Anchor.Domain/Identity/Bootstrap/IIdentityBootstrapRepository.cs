using Anchor.Domain.Identity.Users;

namespace Anchor.Domain.Identity.Bootstrap;

public interface IIdentityBootstrapRepository
{
    Task EnsureRoleExistsAsync(string roleName, CancellationToken cancellationToken = default);

    Task<BootstrapRoleCoverage> GetRoleCoverageAsync(CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> EnsureBootstrapUserAsync(BootstrapUserSeed seed, CancellationToken cancellationToken = default);
}
