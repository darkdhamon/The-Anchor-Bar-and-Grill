using Anchor.Domain.Identity.Users;

namespace Anchor.Domain.Identity.Configuration;

public sealed class ConfirmedAccountConfigurationService(IConfirmedAccountConfigurationStore store) : IConfirmedAccountConfigurationService
{
    public async Task<ConfirmedAccountConfigurationState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        var environmentOverride = store.GetEnvironmentOverride();
        var fallbackValue = await store.GetFallbackRequireConfirmedAccountAsync(cancellationToken);

        return new ConfirmedAccountConfigurationState(
            EffectiveRequireConfirmedAccount: environmentOverride ?? fallbackValue,
            FallbackRequireConfirmedAccount: fallbackValue,
            IsEnvironmentOverride: environmentOverride.HasValue);
    }

    public async Task<bool> IsConfirmationRequiredAsync(CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync(cancellationToken);
        return state.EffectiveRequireConfirmedAccount;
    }

    public async Task<IdentityOperationResult> SetFallbackRequireConfirmedAccountAsync(bool requireConfirmedAccount, CancellationToken cancellationToken = default)
    {
        try
        {
            await store.SetFallbackRequireConfirmedAccountAsync(requireConfirmedAccount, cancellationToken);
            return IdentityOperationResult.Success();
        }
        catch (Exception ex)
        {
            return IdentityOperationResult.Failure($"Unable to update appsettings.json: {ex.Message}");
        }
    }
}
