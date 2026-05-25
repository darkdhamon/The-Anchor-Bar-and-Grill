namespace Anchor.Domain.Publicity;

public sealed record HomepagePublicityContent(
    string Eyebrow,
    string Headline,
    string Summary);

public sealed record HomepagePublicityAdminView(
    HomepagePublicityContent? DraftContent,
    DateTimeOffset? DraftUpdatedAtUtc,
    HomepagePublicityContent? PublishedContent,
    DateTimeOffset? PublishedUpdatedAtUtc);

public sealed record SaveHomepagePublicityRequest(
    string Eyebrow,
    string Headline,
    string Summary);
