namespace Anchor.Domain.Identity.Configuration;

public sealed class AnchorIdentityOptions
{
    public bool RequireConfirmedAccount { get; set; }

    public BootstrapAdminOptions BootstrapAdmin { get; set; } = new();
}
