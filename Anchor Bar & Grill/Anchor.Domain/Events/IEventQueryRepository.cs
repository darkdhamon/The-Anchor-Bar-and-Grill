namespace Anchor.Domain.Events;

public interface IEventQueryRepository
{
    Task<IReadOnlyList<EventRecord>> GetUpcomingPublicEventCandidatesAsync(
        DateOnly fromDate,
        DateOnly throughDate,
        CancellationToken cancellationToken = default);

    Task<bool> HasUpcomingPublicEventCandidatesAsync(
        DateOnly fromDate,
        CancellationToken cancellationToken = default);
}
