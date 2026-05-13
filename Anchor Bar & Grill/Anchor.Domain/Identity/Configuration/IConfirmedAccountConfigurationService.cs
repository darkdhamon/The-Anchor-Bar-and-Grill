using Anchor.Domain.Identity.Users;

namespace Anchor.Domain.Identity.Configuration;

public interface IConfirmedAccountConfigurationService
{
    Task<ConfirmedAccountConfigurationState> GetStateAsync(CancellationToken cancellationToken = default);

    Task<bool> IsConfirmationRequiredAsync(CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> SetFallbackRequireConfirmedAccountAsync(bool requireConfirmedAccount, CancellationToken cancellationToken = default);
}
