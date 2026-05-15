namespace Anchor.Domain.Identity.Configuration;

public interface IConfirmedAccountAccessService
{
    Task<ConfirmedAccountAccessDecision> EvaluateAsync(bool accountConfirmed, CancellationToken cancellationToken = default);
}
