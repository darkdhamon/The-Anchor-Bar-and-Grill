namespace Anchor.Domain.Events;

public interface IEventManagementService
{
    Task<IReadOnlyList<EventRecord>> GetEventsAsync(CancellationToken cancellationToken = default);

    Task<EventOperationResult> SaveEventAsync(SaveEventRequest request, CancellationToken cancellationToken = default);

    Task<EventOperationResult> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default);
}
