using Anchor.Domain.Publicity;

namespace Anchor.Domain.Tests.Publicity;

public sealed class HomepagePublicityServiceTests
{
    [Fact]
    public async Task SaveDraftAsync_returns_validation_errors_without_writing()
    {
        var repository = new FakeHomepagePublicityRepository();
        var service = new HomepagePublicityService(repository, new FixedTimeProvider(new DateTimeOffset(2026, 5, 25, 13, 0, 0, TimeSpan.Zero)));

        var result = await service.SaveDraftAsync(new SaveHomepagePublicityRequest("", "", ""));

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
        Assert.False(repository.WasSaved);
    }

    [Fact]
    public async Task SaveDraftAsync_stores_trimmed_draft_without_touching_published_copy()
    {
        var repository = new FakeHomepagePublicityRepository();
        var now = new DateTimeOffset(2026, 5, 25, 13, 0, 0, TimeSpan.Zero);
        var service = new HomepagePublicityService(repository, new FixedTimeProvider(now));

        var result = await service.SaveDraftAsync(
            new SaveHomepagePublicityRequest(" Guest Welcome ", " Welcome aboard. ", " Friendly draft summary. "));

        Assert.True(result.Succeeded);
        Assert.True(repository.WasSaved);
        Assert.Equal(new HomepagePublicityContent("Guest Welcome", "Welcome aboard.", "Friendly draft summary."), repository.DraftContent);
        Assert.Equal(now, repository.DraftUpdatedAtUtc);
        Assert.Null(repository.PublishedContent);
    }

    [Fact]
    public async Task PublishAsync_updates_draft_and_published_copy_together()
    {
        var repository = new FakeHomepagePublicityRepository();
        var now = new DateTimeOffset(2026, 5, 25, 14, 30, 0, TimeSpan.Zero);
        var service = new HomepagePublicityService(repository, new FixedTimeProvider(now));

        var result = await service.PublishAsync(
            new SaveHomepagePublicityRequest("Guest Welcome", "Live now", "Published summary"));

        Assert.True(result.Succeeded);
        Assert.Equal(new HomepagePublicityContent("Guest Welcome", "Live now", "Published summary"), repository.DraftContent);
        Assert.Equal(repository.DraftContent, repository.PublishedContent);
        Assert.Equal(now, repository.DraftUpdatedAtUtc);
        Assert.Equal(now, repository.PublishedUpdatedAtUtc);
    }

    private sealed class FakeHomepagePublicityRepository : IHomepagePublicityRepository
    {
        public HomepagePublicityContent? DraftContent { get; private set; }

        public DateTimeOffset? DraftUpdatedAtUtc { get; private set; }

        public HomepagePublicityContent? PublishedContent { get; private set; }

        public DateTimeOffset? PublishedUpdatedAtUtc { get; private set; }

        public bool WasSaved { get; private set; }

        public Task<HomepagePublicityAdminView> GetHomepageAdminViewAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new HomepagePublicityAdminView(DraftContent, DraftUpdatedAtUtc, PublishedContent, PublishedUpdatedAtUtc));

        public Task<HomepagePublicityContent?> GetPublishedHomepageAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PublishedContent);

        public Task SaveDraftAsync(HomepagePublicityContent content, DateTimeOffset savedAtUtc, CancellationToken cancellationToken = default)
        {
            DraftContent = content;
            DraftUpdatedAtUtc = savedAtUtc;
            return Task.CompletedTask;
        }

        public Task PublishAsync(HomepagePublicityContent content, DateTimeOffset publishedAtUtc, CancellationToken cancellationToken = default)
        {
            DraftContent = content;
            DraftUpdatedAtUtc = publishedAtUtc;
            PublishedContent = content;
            PublishedUpdatedAtUtc = publishedAtUtc;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            WasSaved = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset currentUtcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => currentUtcNow;

        public void SetUtcNow(DateTimeOffset value) => currentUtcNow = value;
    }
}
