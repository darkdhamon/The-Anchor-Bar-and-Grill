using Anchor.Domain.Events;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Anchor.Infrastructure.Data.Events;

public sealed class EventQueryRepository(ApplicationDbContext dbContext) : IEventQueryRepository
{
    private const int MaxRecurrenceInterval = 1200;

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
                    : item.RecurrencePattern == EventRecurrencePattern.Weekly
                        ? item.RecurrenceInterval is >= 1 and <= MaxRecurrenceInterval
                            && item.RecursOnDayOfWeek != null
                            && (int)item.RecursOnDayOfWeek >= (int)DayOfWeek.Sunday
                            && (int)item.RecursOnDayOfWeek <= (int)DayOfWeek.Saturday
                            && item.StartsOn <= throughDate
                            && (item.RecursUntil == null || item.RecursUntil >= fromDate)
                        : item.RecurrencePattern == EventRecurrencePattern.MonthlyNthWeekday
                            ? item.RecurrenceInterval is >= 1 and <= MaxRecurrenceInterval
                                && item.RecursOnDayOfWeek != null
                                && (int)item.RecursOnDayOfWeek >= (int)DayOfWeek.Sunday
                                && (int)item.RecursOnDayOfWeek <= (int)DayOfWeek.Saturday
                                && item.RecursOnWeekOfMonth != null
                                && (int)item.RecursOnWeekOfMonth >= (int)EventRecurrenceWeek.First
                                && (int)item.RecursOnWeekOfMonth <= (int)EventRecurrenceWeek.Last
                                && item.StartsOn <= throughDate
                                && (item.RecursUntil == null || item.RecursUntil >= fromDate)
                            : false))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.StartsOn)
            .ThenBy(item => item.Title)
            .Select(Projection)
            .ToListAsync(cancellationToken);

    public Task<bool> HasUpcomingPublicEventCandidatesAsync(
        DateOnly fromDate,
        CancellationToken cancellationToken = default) =>
        dbContext.Events
            .AsNoTracking()
            .Where(item => item.PublicationState == EventPublicationState.Published)
            .AnyAsync(
                item => item.RecurrencePattern == EventRecurrencePattern.None
                    ? item.StartsOn >= fromDate
                    : item.RecurrencePattern == EventRecurrencePattern.Weekly
                        ? item.RecurrenceInterval is >= 1 and <= MaxRecurrenceInterval
                            && item.RecursOnDayOfWeek != null
                            && (int)item.RecursOnDayOfWeek >= (int)DayOfWeek.Sunday
                            && (int)item.RecursOnDayOfWeek <= (int)DayOfWeek.Saturday
                            && (item.RecursUntil == null || item.RecursUntil >= fromDate)
                        : item.RecurrencePattern == EventRecurrencePattern.MonthlyNthWeekday
                            ? item.RecurrenceInterval is >= 1 and <= MaxRecurrenceInterval
                                && item.RecursOnDayOfWeek != null
                                && (int)item.RecursOnDayOfWeek >= (int)DayOfWeek.Sunday
                                && (int)item.RecursOnDayOfWeek <= (int)DayOfWeek.Saturday
                                && item.RecursOnWeekOfMonth != null
                                && (int)item.RecursOnWeekOfMonth >= (int)EventRecurrenceWeek.First
                                && (int)item.RecursOnWeekOfMonth <= (int)EventRecurrenceWeek.Last
                                && (item.RecursUntil == null || item.RecursUntil >= fromDate)
                            : false,
                cancellationToken);

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
