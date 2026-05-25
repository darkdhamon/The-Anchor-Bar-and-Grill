namespace Anchor.Infrastructure.Data.Publicity;

public sealed class HomepagePublicityEntity
{
    public int HomepagePublicityId { get; set; }

    public string? DraftEyebrow { get; set; }

    public string? DraftHeadline { get; set; }

    public string? DraftSummary { get; set; }

    public DateTimeOffset? DraftUpdatedAtUtc { get; set; }

    public string? PublishedEyebrow { get; set; }

    public string? PublishedHeadline { get; set; }

    public string? PublishedSummary { get; set; }

    public DateTimeOffset? PublishedUpdatedAtUtc { get; set; }
}
