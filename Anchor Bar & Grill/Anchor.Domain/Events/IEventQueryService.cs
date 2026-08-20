namespace Anchor.Domain.Events;

public interface IEventQueryService
{
    Task<IReadOnlyList<EventOccurrenceRecord>> GetUpcomingEventsAsync(
        DateTime localNow,
        int daysAhead = 30,
        CancellationToken cancellationToken = default);

    Task<UpcomingEventWindowResult> GetUpcomingEventsWindowAsync(
        DateTime localNow,
        DateOnly fromDate,
        int daysAhead = 30,
        bool skipEmptyWindows = false,
        CancellationToken cancellationToken = default);
}
