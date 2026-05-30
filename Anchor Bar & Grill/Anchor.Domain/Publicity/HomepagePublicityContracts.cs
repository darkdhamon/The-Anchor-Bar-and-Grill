namespace Anchor.Domain.Publicity;

public static class HomepagePublicityConstraints
{
    public const int EyebrowMaxLength = 80;
    public const int HeadlineMaxLength = 120;
    public const int SummaryMaxLength = 4000;
}

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
