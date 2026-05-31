namespace Anchor.Domain.Events;

public interface IEventManagementRepository
{
    Task<IReadOnlyList<EventRecord>> GetEventsAsync(CancellationToken cancellationToken = default);

    Task<Guid> UpsertEventAsync(SaveEventRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
