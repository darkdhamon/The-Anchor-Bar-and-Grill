namespace Anchor.Domain.Events;

public sealed class EventManagementService(
    IEventManagementRepository repository,
    IEventOperationLogSink logSink) : IEventManagementService
{
    public Task<EventManagementPage> GetEventsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        return repository.GetEventsAsync((pageNumber - 1) * pageSize, pageSize, cancellationToken);
    }

    public Task<EventRecord?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        repository.GetEventAsync(eventId, cancellationToken);

    public async Task<int?> GetEventPageNumberAsync(Guid eventId, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        var index = await repository.GetEventIndexAsync(eventId, cancellationToken);
        return index is null ? null : (index.Value / pageSize) + 1;
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
            return EventOperationResult.Failure("The requested event changed in another session, was deleted, or could not be found. Reload the editor before saving again.");
        }

        try
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
        catch (EventConcurrencyException)
        {
            return EventOperationResult.Failure("The requested event changed in another session. Reload the editor before saving again.");
        }
        await logSink.WriteAsync(
            new EventOperationLogEntry(GetSaveOperation(request), eventId.Value, request.Title.Trim()),
            cancellationToken);

        return EventOperationResult.Success(eventId.Value);
    }

    public async Task<EventOperationResult> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        if (!await repository.DeleteEventAsync(eventId, cancellationToken))
        {
            return EventOperationResult.Failure("The requested event was not found.");
        }

        await repository.SaveChangesAsync(cancellationToken);
        await logSink.WriteAsync(
            new EventOperationLogEntry("delete", eventId, $"Permanently deleted event {eventId}."),
            cancellationToken);
        return EventOperationResult.Success(eventId);
    }

    private static string GetSaveOperation(SaveEventRequest request) =>
        request.SaveAction switch
        {
            EventSaveAction.Publish => "publish",
            EventSaveAction.Archive => "archive",
            EventSaveAction.SaveDraft => "save-draft",
            _ => "save"
        };
}
