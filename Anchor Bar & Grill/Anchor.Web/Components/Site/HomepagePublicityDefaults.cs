using Anchor.Domain.Publicity;

namespace Anchor.Web.Components.Site;

public static class HomepagePublicityDefaults
{
    public static readonly HomepagePublicityContent Content = new(
        "Guest Welcome",
        "Welcome aboard The Anchor.",
        "This homepage mockup is intentionally separate from the menu so first-time guests can understand what The Anchor Bar & Grill offers before they dive into food, events, or contact details.");
}
