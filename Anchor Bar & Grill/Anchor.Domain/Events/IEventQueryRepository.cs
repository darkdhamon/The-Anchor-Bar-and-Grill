namespace Anchor.Domain.Events;

public interface IEventQueryRepository
{
    Task<IReadOnlyList<EventRecord>> GetUpcomingPublicEventCandidatesAsync(
        DateOnly fromDate,
        DateOnly throughDate,
        CancellationToken cancellationToken = default);
}
