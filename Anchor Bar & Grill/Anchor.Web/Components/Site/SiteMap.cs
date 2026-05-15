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

    public static IReadOnlyList<RouteLink> AuthenticatedLinks { get; } =
    [];

    public static IReadOnlyList<RouteLink> EventManagementLinks { get; } =
    [
        new("/admin/events", "Event Editor", "Plan, edit, and retire event listings.")
    ];

    public static IReadOnlyList<RouteLink> MenuManagementLinks { get; } =
    [
        new("/admin/menu", "Menu Editor", "Create, edit, and archive menu items and feature sections.")
    ];

    public static IReadOnlyList<RouteLink> AdminAccessLinks { get; } =
    [
        new("/help", "Help", "Explain the current staff workflow, role responsibilities, and admin tools by subject."),
        new("/admin/about", "Publicity Editor", "Maintain the restaurant story, amenities, and guest notes."),
        new("/admin/contact", "Contact Editor", "Manage contact details, hours, and guest response guidance."),
        new("/admin/users", "User Management", "Confirm staff accounts and assign operational or technical roles."),
        new("/admin/security", "Security", "Manage runtime confirmed-account behavior and review bootstrap coverage.")
    ];

    public static IReadOnlyList<RouteLink> TechnicalLinks { get; } =
    [
        new("/admin/system", "IT / System", "Reserve a technical area for future diagnostics and system tools.")
    ];
}

public sealed record RouteLink(string Href, string Label, string Description);
