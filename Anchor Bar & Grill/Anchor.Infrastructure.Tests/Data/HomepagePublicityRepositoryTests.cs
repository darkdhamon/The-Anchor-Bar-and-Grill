using Anchor.Domain.Publicity;
using Anchor.Infrastructure.Data.Publicity;
using Anchor.Infrastructure.Tests.Support;

namespace Anchor.Infrastructure.Tests.Data;

public sealed class HomepagePublicityRepositoryTests
{
    [Fact]
    public async Task SaveDraftAsync_persists_draft_without_exposing_published_copy()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new HomepagePublicityRepository(context.DbContext);
        var longSummary = CreateLongSummary();

        await repository.SaveDraftAsync(
            new HomepagePublicityContent("Guest Welcome", "Draft headline", longSummary),
            new DateTimeOffset(2026, 5, 25, 13, 0, 0, TimeSpan.Zero));
        await repository.SaveChangesAsync();

        var state = await repository.GetHomepageAdminViewAsync();
        var published = await repository.GetPublishedHomepageAsync();

        Assert.Equal("Draft headline", state.DraftContent?.Headline);
        Assert.Equal(longSummary, state.DraftContent?.Summary);
        Assert.NotNull(state.DraftUpdatedAtUtc);
        Assert.Null(state.PublishedContent);
        Assert.Null(published);
    }

    [Fact]
    public async Task PublishAsync_updates_both_draft_and_published_fields()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new HomepagePublicityRepository(context.DbContext);
        var longSummary = CreateLongSummary();

        await repository.PublishAsync(
            new HomepagePublicityContent("Weekend Welcome", "Live headline", longSummary),
            new DateTimeOffset(2026, 5, 25, 14, 0, 0, TimeSpan.Zero));
        await repository.SaveChangesAsync();

        var state = await repository.GetHomepageAdminViewAsync();
        var published = await repository.GetPublishedHomepageAsync();

        Assert.Equal("Live headline", state.DraftContent?.Headline);
        Assert.Equal("Live headline", state.PublishedContent?.Headline);
        Assert.Equal(longSummary, published?.Summary);
        Assert.NotNull(state.PublishedUpdatedAtUtc);
    }

    private static string CreateLongSummary() =>
        string.Join(
            Environment.NewLine + Environment.NewLine,
            Enumerable.Repeat(
                "This is a longer guest-facing welcome paragraph that verifies repository-backed homepage publicity content can persist more than a short summary.",
                10));
}
