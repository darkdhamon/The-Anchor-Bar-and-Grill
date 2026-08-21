namespace Anchor.Domain.Events;

public sealed class EventManagementService(IEventManagementRepository repository) : IEventManagementService
{
    public Task<EventManagementPage> GetEventsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        return repository.GetEventsAsync((pageNumber - 1) * pageSize, pageSize, cancellationToken);
    }

    public async Task<EventOperationResult> SaveEventAsync(SaveEventRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = EventScheduleRules.Validate(request);
        if (validationErrors.Count > 0)
        {
            return EventOperationResult.Failure(validationErrors);
        }

        var eventId = await repository.UpsertEventAsync(request, cancellationToken);
        if (eventId is null)
        {
            return EventOperationResult.Failure("The requested event was deleted or could not be found. Reload the editor before saving again.");
        }

        await repository.SaveChangesAsync(cancellationToken);

        return EventOperationResult.Success(eventId.Value);
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
