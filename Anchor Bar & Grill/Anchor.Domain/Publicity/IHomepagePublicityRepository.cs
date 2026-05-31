namespace Anchor.Domain.Publicity;

public interface IHomepagePublicityRepository
{
    Task<HomepagePublicityAdminView> GetHomepageAdminViewAsync(CancellationToken cancellationToken = default);

    Task<HomepagePublicityContent?> GetPublishedHomepageAsync(CancellationToken cancellationToken = default);

    Task SaveDraftAsync(
        HomepagePublicityContent content,
        DateTimeOffset savedAtUtc,
        CancellationToken cancellationToken = default);

    Task PublishAsync(
        HomepagePublicityContent content,
        DateTimeOffset publishedAtUtc,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
