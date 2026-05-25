using Anchor.Domain.Events;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Anchor.Infrastructure.Data.Events;

public sealed class EventQueryRepository(ApplicationDbContext dbContext) : IEventQueryRepository
{
    public async Task<IReadOnlyList<EventRecord>> GetUpcomingPublicEventCandidatesAsync(
        DateOnly fromDate,
        DateOnly throughDate,
        CancellationToken cancellationToken = default) =>
        await dbContext.Events
            .AsNoTracking()
            .Where(item => item.PublicationState == EventPublicationState.Published)
            .Where(item =>
                item.RecurrencePattern == EventRecurrencePattern.None
                    ? item.StartsOn >= fromDate && item.StartsOn <= throughDate
                    : item.StartsOn <= throughDate && (item.RecursUntil == null || item.RecursUntil >= fromDate))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.StartsOn)
            .ThenBy(item => item.Title)
            .Select(Projection)
            .ToListAsync(cancellationToken);

    private static readonly Expression<Func<EventEntity, EventRecord>> Projection = item =>
        new EventRecord(
            item.EventId,
            item.Title,
            item.Summary,
            item.Description,
            item.PromoBadge,
            item.ImagePath,
            item.StartsOn,
            item.StartsAt,
            item.EndsAt,
            item.EndsNextDay,
            item.SortOrder,
            item.PublicationState,
            item.RecurrencePattern,
            item.RecurrenceInterval,
            item.RecursOnDayOfWeek,
            item.RecursOnWeekOfMonth,
            item.RecursUntil);
}
