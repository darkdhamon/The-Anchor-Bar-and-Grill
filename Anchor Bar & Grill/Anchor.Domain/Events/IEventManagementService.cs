namespace Anchor.Domain.Events;

public interface IEventManagementService
{
    Task<EventManagementPage> GetEventsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<EventRecord?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<int?> GetEventPageNumberAsync(Guid eventId, int pageSize, CancellationToken cancellationToken = default);

    Task<EventOperationResult> SaveEventAsync(SaveEventRequest request, CancellationToken cancellationToken = default);

    Task<EventOperationResult> DeleteEventAsync(Guid eventId, Guid expectedRevision, CancellationToken cancellationToken = default);
}
