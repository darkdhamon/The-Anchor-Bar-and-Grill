namespace Anchor.Domain.Publicity;

public interface IHomepagePublicityService
{
    Task<HomepagePublicityAdminView> GetHomepageAdminViewAsync(CancellationToken cancellationToken = default);

    Task<HomepagePublicityContent?> GetPublishedHomepageAsync(CancellationToken cancellationToken = default);

    Task<HomepagePublicityOperationResult> SaveDraftAsync(
        SaveHomepagePublicityRequest request,
        CancellationToken cancellationToken = default);

    Task<HomepagePublicityOperationResult> PublishAsync(
        SaveHomepagePublicityRequest request,
        CancellationToken cancellationToken = default);
}
