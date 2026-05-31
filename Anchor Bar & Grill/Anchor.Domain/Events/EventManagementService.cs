namespace Anchor.Domain.Events;

public sealed class EventManagementService(IEventManagementRepository repository) : IEventManagementService
{
    public Task<IReadOnlyList<EventRecord>> GetEventsAsync(CancellationToken cancellationToken = default) =>
        repository.GetEventsAsync(cancellationToken);

    public async Task<EventOperationResult> SaveEventAsync(SaveEventRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = EventScheduleRules.Validate(request);
        if (validationErrors.Count > 0)
        {
            return EventOperationResult.Failure(validationErrors);
        }

        var eventId = await repository.UpsertEventAsync(request, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return EventOperationResult.Success(eventId);
    }

    public async Task<EventOperationResult> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        if (!await repository.DeleteEventAsync(eventId, cancellationToken))
        {
            return EventOperationResult.Failure("The requested event was not found.");
        }

        await repository.SaveChangesAsync(cancellationToken);
        return EventOperationResult.Success(eventId);
    }
}
