namespace Anchor.Domain.Identity.Configuration;

public sealed record ConfirmedAccountConfigurationState(
    bool EffectiveRequireConfirmedAccount,
    bool FallbackRequireConfirmedAccount,
    bool IsEnvironmentOverride)
{
    public string EffectiveSource => IsEnvironmentOverride ? "Environment variable" : "appsettings.json";
}
