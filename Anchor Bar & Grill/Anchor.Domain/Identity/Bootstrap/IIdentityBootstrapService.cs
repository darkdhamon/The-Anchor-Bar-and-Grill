namespace Anchor.Domain.Identity.Bootstrap;

public interface IIdentityBootstrapService
{
    Task BootstrapAsync(CancellationToken cancellationToken = default);
}
