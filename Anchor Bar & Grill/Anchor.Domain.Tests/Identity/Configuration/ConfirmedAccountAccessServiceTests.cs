using Anchor.Domain.Identity.Configuration;
using Anchor.Domain.Identity.Users;

namespace Anchor.Domain.Tests.Identity.Configuration;

public sealed class ConfirmedAccountAccessServiceTests
{
    [Fact]
    public async Task EvaluateAsync_allows_sign_in_when_confirmation_is_not_required()
    {
        var configurationService = new FakeConfirmedAccountConfigurationService
        {
            IsConfirmationRequired = false
        };
        var service = new ConfirmedAccountAccessService(configurationService);

        var decision = await service.EvaluateAsync(accountConfirmed: false);

        Assert.False(decision.IsConfirmationRequired);
        Assert.False(decision.IsAccountConfirmed);
        Assert.True(decision.IsAccessAllowed);
    }

    [Fact]
    public async Task EvaluateAsync_blocks_sign_in_when_confirmation_is_required_and_account_is_pending()
    {
        var configurationService = new FakeConfirmedAccountConfigurationService
        {
            IsConfirmationRequired = true
        };
        var service = new ConfirmedAccountAccessService(configurationService);

        var decision = await service.EvaluateAsync(accountConfirmed: false);

        Assert.True(decision.IsConfirmationRequired);
        Assert.False(decision.IsAccountConfirmed);
        Assert.False(decision.IsAccessAllowed);
    }

    [Fact]
    public async Task EvaluateAsync_allows_sign_in_when_confirmation_is_required_and_account_is_confirmed()
    {
        var configurationService = new FakeConfirmedAccountConfigurationService
        {
            IsConfirmationRequired = true
        };
        var service = new ConfirmedAccountAccessService(configurationService);

        var decision = await service.EvaluateAsync(accountConfirmed: true);

        Assert.True(decision.IsConfirmationRequired);
        Assert.True(decision.IsAccountConfirmed);
        Assert.True(decision.IsAccessAllowed);
    }

    private sealed class FakeConfirmedAccountConfigurationService : IConfirmedAccountConfigurationService
    {
        public bool IsConfirmationRequired { get; set; }

        public Task<ConfirmedAccountConfigurationState> GetStateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ConfirmedAccountConfigurationState(
                EffectiveRequireConfirmedAccount: IsConfirmationRequired,
                FallbackRequireConfirmedAccount: IsConfirmationRequired,
                IsEnvironmentOverride: false));

        public Task<bool> IsConfirmationRequiredAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(IsConfirmationRequired);

        public Task<IdentityOperationResult> SetFallbackRequireConfirmedAccountAsync(bool requireConfirmedAccount, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityOperationResult.Success());
    }
}
