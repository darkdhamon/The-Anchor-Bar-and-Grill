namespace Anchor.Domain.Events;

public interface IEventQueryService
{
    Task<IReadOnlyList<EventOccurrenceRecord>> GetUpcomingEventsAsync(
        DateTime localNow,
        int daysAhead = 30,
        CancellationToken cancellationToken = default);
}
