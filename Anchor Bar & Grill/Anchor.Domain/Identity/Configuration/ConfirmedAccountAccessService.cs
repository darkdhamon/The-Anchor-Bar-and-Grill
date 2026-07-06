namespace Anchor.Domain.Identity.Configuration;

public sealed class ConfirmedAccountAccessService(IConfirmedAccountConfigurationService configurationService) : IConfirmedAccountAccessService
{
    public async Task<ConfirmedAccountAccessDecision> EvaluateAsync(bool accountConfirmed, CancellationToken cancellationToken = default)
    {
        var requireConfirmedAccount = await configurationService.IsConfirmationRequiredAsync(cancellationToken);
        return new ConfirmedAccountAccessDecision(requireConfirmedAccount, accountConfirmed);
    }
}
