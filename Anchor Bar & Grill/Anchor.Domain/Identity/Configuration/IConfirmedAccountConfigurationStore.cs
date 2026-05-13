namespace Anchor.Domain.Identity.Configuration;

public interface IConfirmedAccountConfigurationStore
{
    bool? GetEnvironmentOverride();

    Task<bool> GetFallbackRequireConfirmedAccountAsync(CancellationToken cancellationToken = default);

    Task SetFallbackRequireConfirmedAccountAsync(bool requireConfirmedAccount, CancellationToken cancellationToken = default);
}
