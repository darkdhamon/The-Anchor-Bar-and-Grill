namespace Anchor.Domain.Publicity;

public sealed class HomepagePublicityService(
    IHomepagePublicityRepository repository,
    TimeProvider timeProvider) : IHomepagePublicityService
{
    public Task<HomepagePublicityAdminView> GetHomepageAdminViewAsync(CancellationToken cancellationToken = default) =>
        repository.GetHomepageAdminViewAsync(cancellationToken);

    public Task<HomepagePublicityContent?> GetPublishedHomepageAsync(CancellationToken cancellationToken = default) =>
        repository.GetPublishedHomepageAsync(cancellationToken);

    public async Task<HomepagePublicityOperationResult> SaveDraftAsync(
        SaveHomepagePublicityRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = HomepagePublicityValidation.Validate(request);
        if (validationErrors.Count > 0)
        {
            return HomepagePublicityOperationResult.Failure(validationErrors);
        }

        await repository.SaveDraftAsync(CreateContent(request), timeProvider.GetUtcNow(), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return HomepagePublicityOperationResult.Success();
    }

    public async Task<HomepagePublicityOperationResult> PublishAsync(
        SaveHomepagePublicityRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = HomepagePublicityValidation.Validate(request);
        if (validationErrors.Count > 0)
        {
            return HomepagePublicityOperationResult.Failure(validationErrors);
        }

        await repository.PublishAsync(CreateContent(request), timeProvider.GetUtcNow(), cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return HomepagePublicityOperationResult.Success();
    }

    private static HomepagePublicityContent CreateContent(SaveHomepagePublicityRequest request) =>
        new(
            request.Eyebrow.Trim(),
            request.Headline.Trim(),
            request.Summary.Trim());
}
