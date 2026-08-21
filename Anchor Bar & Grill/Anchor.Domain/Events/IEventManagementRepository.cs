namespace Anchor.Domain.Events;

public interface IEventManagementRepository
{
    Task<EventManagementPage> GetEventsAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<Guid?> UpsertEventAsync(SaveEventRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
