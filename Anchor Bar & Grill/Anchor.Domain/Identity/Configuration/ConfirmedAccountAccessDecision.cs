namespace Anchor.Domain.Identity.Configuration;

public sealed record ConfirmedAccountAccessDecision(
    bool IsConfirmationRequired,
    bool IsAccountConfirmed)
{
    public bool IsAccessAllowed => !IsConfirmationRequired || IsAccountConfirmed;
}
