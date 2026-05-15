using Anchor.Domain.Identity.Configuration;

namespace Anchor.Domain.Tests.Identity.Configuration;

public sealed class ConfirmedAccountConfigurationServiceTests
{
    [Fact]
    public async Task GetStateAsync_prefers_environment_override_over_fallback()
    {
        var store = new FakeConfirmedAccountConfigurationStore
        {
            EnvironmentOverride = true,
            FallbackValue = false
        };
        var service = new ConfirmedAccountConfigurationService(store);

        var state = await service.GetStateAsync();

        Assert.True(state.EffectiveRequireConfirmedAccount);
        Assert.False(state.FallbackRequireConfirmedAccount);
        Assert.True(state.IsEnvironmentOverride);
        Assert.Equal("Environment variable", state.EffectiveSource);
    }

    [Fact]
    public async Task IsConfirmationRequiredAsync_uses_fallback_value_when_override_missing()
    {
        var store = new FakeConfirmedAccountConfigurationStore
        {
            EnvironmentOverride = null,
            FallbackValue = true
        };
        var service = new ConfirmedAccountConfigurationService(store);

        var result = await service.IsConfirmationRequiredAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task SetFallbackRequireConfirmedAccountAsync_updates_store_when_save_succeeds()
    {
        var store = new FakeConfirmedAccountConfigurationStore();
        var service = new ConfirmedAccountConfigurationService(store);

        var result = await service.SetFallbackRequireConfirmedAccountAsync(true);

        Assert.True(result.Succeeded);
        Assert.True(store.FallbackValue);
    }

    [Fact]
    public async Task SetFallbackRequireConfirmedAccountAsync_returns_failure_when_store_throws()
    {
        var store = new FakeConfirmedAccountConfigurationStore
        {
            ExceptionToThrow = new InvalidOperationException("Disk unavailable.")
        };
        var service = new ConfirmedAccountConfigurationService(store);

        var result = await service.SetFallbackRequireConfirmedAccountAsync(true);

        Assert.False(result.Succeeded);
        Assert.Contains("Unable to update appsettings.json", result.Errors.Single(), StringComparison.Ordinal);
    }

    private sealed class FakeConfirmedAccountConfigurationStore : IConfirmedAccountConfigurationStore
    {
        public bool? EnvironmentOverride { get; set; }

        public bool FallbackValue { get; set; }

        public Exception? ExceptionToThrow { get; set; }

        public bool? GetEnvironmentOverride() => EnvironmentOverride;

        public Task<bool> GetFallbackRequireConfirmedAccountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(FallbackValue);

        public Task SetFallbackRequireConfirmedAccountAsync(bool requireConfirmedAccount, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            FallbackValue = requireConfirmedAccount;
            return Task.CompletedTask;
        }
    }
}
