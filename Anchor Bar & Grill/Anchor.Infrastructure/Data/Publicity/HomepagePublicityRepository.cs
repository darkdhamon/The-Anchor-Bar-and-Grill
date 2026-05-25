using Anchor.Domain.Publicity;
using Microsoft.EntityFrameworkCore;

namespace Anchor.Infrastructure.Data.Publicity;

public sealed class HomepagePublicityRepository(ApplicationDbContext dbContext) : IHomepagePublicityRepository
{
    private const int SingletonId = 1;

    public async Task<HomepagePublicityAdminView> GetHomepageAdminViewAsync(CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.HomepagePublicity
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.HomepagePublicityId == SingletonId, cancellationToken);

        return entity is null
            ? new HomepagePublicityAdminView(null, null, null, null)
            : new HomepagePublicityAdminView(
                CreateDraftContent(entity),
                entity.DraftUpdatedAtUtc,
                CreatePublishedContent(entity),
                entity.PublishedUpdatedAtUtc);
    }

    public async Task<HomepagePublicityContent?> GetPublishedHomepageAsync(CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.HomepagePublicity
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.HomepagePublicityId == SingletonId, cancellationToken);

        return entity is null ? null : CreatePublishedContent(entity);
    }

    public async Task SaveDraftAsync(
        HomepagePublicityContent content,
        DateTimeOffset savedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetOrCreateEntityAsync(cancellationToken);
        entity.DraftEyebrow = NormalizeOptional(content.Eyebrow);
        entity.DraftHeadline = content.Headline;
        entity.DraftSummary = content.Summary;
        entity.DraftUpdatedAtUtc = savedAtUtc;
    }

    public async Task PublishAsync(
        HomepagePublicityContent content,
        DateTimeOffset publishedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetOrCreateEntityAsync(cancellationToken);
        var normalizedEyebrow = NormalizeOptional(content.Eyebrow);

        entity.DraftEyebrow = normalizedEyebrow;
        entity.DraftHeadline = content.Headline;
        entity.DraftSummary = content.Summary;
        entity.DraftUpdatedAtUtc = publishedAtUtc;

        entity.PublishedEyebrow = normalizedEyebrow;
        entity.PublishedHeadline = content.Headline;
        entity.PublishedSummary = content.Summary;
        entity.PublishedUpdatedAtUtc = publishedAtUtc;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private async Task<HomepagePublicityEntity> GetOrCreateEntityAsync(CancellationToken cancellationToken)
    {
        var entity = await dbContext.HomepagePublicity
            .SingleOrDefaultAsync(item => item.HomepagePublicityId == SingletonId, cancellationToken);

        if (entity is not null)
        {
            return entity;
        }

        entity = new HomepagePublicityEntity
        {
            HomepagePublicityId = SingletonId
        };

        dbContext.HomepagePublicity.Add(entity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return entity;
        }
        catch (DbUpdateException)
        {
            if (!await SingletonRowExistsAsync(cancellationToken))
            {
                throw;
            }

            dbContext.Entry(entity).State = EntityState.Detached;

            return await dbContext.HomepagePublicity
                .SingleAsync(item => item.HomepagePublicityId == SingletonId, cancellationToken);
        }
    }

    private static HomepagePublicityContent? CreateDraftContent(HomepagePublicityEntity entity) =>
        string.IsNullOrWhiteSpace(entity.DraftHeadline) || string.IsNullOrWhiteSpace(entity.DraftSummary)
            ? null
            : new HomepagePublicityContent(
                entity.DraftEyebrow ?? string.Empty,
                entity.DraftHeadline,
                entity.DraftSummary);

    private static HomepagePublicityContent? CreatePublishedContent(HomepagePublicityEntity entity) =>
        string.IsNullOrWhiteSpace(entity.PublishedHeadline) || string.IsNullOrWhiteSpace(entity.PublishedSummary)
            ? null
            : new HomepagePublicityContent(
                entity.PublishedEyebrow ?? string.Empty,
                entity.PublishedHeadline,
                entity.PublishedSummary);

    private Task<bool> SingletonRowExistsAsync(CancellationToken cancellationToken) =>
        dbContext.HomepagePublicity
            .AsNoTracking()
            .AnyAsync(item => item.HomepagePublicityId == SingletonId, cancellationToken);

    private static string? NormalizeOptional(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
