namespace Anchor.Web.Components.Site;

public static class SiteMap
{
    public static IReadOnlyList<RouteLink> PublicLinks { get; } =
    [
        new("/", "Home", "Welcome guests and point them toward the most useful sections."),
        new("/menu", "Menu", "Preview the restaurant menu with feature sections and category callouts."),
        new("/events", "Events", "Showcase recurring programming and special nights."),
        new("/about", "About", "Tell the story of The Anchor and the guest experience."),
        new("/contact", "Contact", "Share location, phone, hours, and guest inquiry details.")
    ];

    public static IReadOnlyList<RouteLink> AdminLinks { get; } =
    [
        new("/admin/events", "Events Admin", "Plan, edit, and retire event listings."),
        new("/admin/menu", "Menu Admin", "Create, edit, and archive menu items and feature sections."),
        new("/admin/about", "About Admin", "Maintain the restaurant story, amenities, and guest notes."),
        new("/admin/contact", "Contact Admin", "Manage contact details, hours, and guest response guidance.")
    ];
}

public sealed record RouteLink(string Href, string Label, string Description);
