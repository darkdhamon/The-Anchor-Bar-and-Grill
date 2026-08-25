namespace Anchor.Domain.Events;

public interface IEventManagementRepository
{
    Task<EventManagementPage> GetEventsAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<EventRecord?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<int?> GetEventIndexAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<Guid?> UpsertEventAsync(SaveEventRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteEventAsync(Guid eventId, Guid expectedRevision, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed class EventConcurrencyException : Exception
{
    public EventConcurrencyException(Exception innerException)
        : base("The event changed before the update could be committed.", innerException)
    {
    }
}
