using Anchor.Domain.Events;

namespace Anchor.Domain.Tests.Events;

public sealed class EventManagementServiceTests
{
    [Fact]
    public async Task SaveEventAsync_returns_validation_errors_without_writing()
    {
        var repository = new FakeEventManagementRepository();
        var service = new EventManagementService(repository, new FakeEventOperationLogSink());

        var result = await service.SaveEventAsync(
            new SaveEventRequest(
                null,
                "",
                "",
                "",
                null,
                null,
                new DateOnly(2026, 5, 18),
                new TimeOnly(18, 0),
                null,
                false,
                1,
                EventPublicationState.Published,
                EventRecurrencePattern.Weekly,
                0,
                null,
                null,
                null));

        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Errors);
        Assert.False(repository.WasSaved);
    }

    [Fact]
    public async Task SaveEventAsync_persists_valid_requests_and_saves_changes()
    {
        var repository = new FakeEventManagementRepository();
        var logSink = new FakeEventOperationLogSink();
        var service = new EventManagementService(repository, logSink);

        var result = await service.SaveEventAsync(
            new SaveEventRequest(
                null,
                "Friday Live Music",
                "Weekly preview copy.",
                "A recurring music feature for Friday nights.",
                "Live Music",
                null,
                new DateOnly(2026, 5, 22),
                new TimeOnly(20, 0),
                null,
                false,
                1,
                EventPublicationState.Draft,
                EventRecurrencePattern.Weekly,
                1,
                DayOfWeek.Friday,
                null,
                null));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.EventId);
        Assert.True(repository.WasSaved);
        Assert.NotNull(repository.LastSavedRequest);
        Assert.Equal(EventPublicationState.Draft, repository.LastSavedRequest!.PublicationState);
        Assert.Contains(logSink.Entries, entry => entry.Operation == "save-draft" && entry.EventId == result.EventId);
    }

    [Fact]
    public async Task SaveEventAsync_rejects_an_update_when_the_event_no_longer_exists()
    {
        var repository = new FakeEventManagementRepository { RejectUpsert = true };
        var service = new EventManagementService(repository, new FakeEventOperationLogSink());

        var result = await service.SaveEventAsync(new SaveEventRequest(
            Guid.NewGuid(), "Deleted event", "Summary", "Description", null, null,
            new DateOnly(2026, 5, 22), new TimeOnly(20, 0), null, false, 1,
            EventPublicationState.Draft, EventRecurrencePattern.None, 1, null, null, null));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("deleted", StringComparison.OrdinalIgnoreCase));
        Assert.False(repository.WasSaved);
    }

    [Fact]
    public async Task DeleteEventAsync_reports_missing_event()
    {
        var repository = new FakeEventManagementRepository { DeleteResult = false };
        var service = new EventManagementService(repository, new FakeEventOperationLogSink());

        var result = await service.DeleteEventAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DeleteEventAsync_successfully_deletes_event_and_saves()
    {
        var eventId = Guid.NewGuid();
        var repository = new FakeEventManagementRepository();
        var logSink = new FakeEventOperationLogSink();
        var service = new EventManagementService(repository, logSink);

        var result = await service.DeleteEventAsync(eventId);

        Assert.True(result.Succeeded);
        Assert.Equal(eventId, result.EventId);
        Assert.Empty(result.Errors);
        Assert.True(repository.WasSaved);
        Assert.Equal(eventId, repository.LastDeletedEventId);
        Assert.Contains(logSink.Entries, entry => entry.Operation == "delete" && entry.EventId == eventId);
    }

    private sealed class FakeEventManagementRepository : IEventManagementRepository
    {
        public bool DeleteResult { get; init; } = true;
        public bool RejectUpsert { get; init; }
        public bool WasSaved { get; private set; }

        public SaveEventRequest? LastSavedRequest { get; private set; }

        public Guid? LastDeletedEventId { get; private set; }

        public Task<EventManagementPage> GetEventsAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult(new EventManagementPage([], 0, 0, []));

        public Task<EventRecord?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            Task.FromResult<EventRecord?>(null);

        public Task<int?> GetEventIndexAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            Task.FromResult<int?>(null);

        public Task<Guid?> UpsertEventAsync(SaveEventRequest request, CancellationToken cancellationToken = default)
        {
            LastSavedRequest = request;
            return Task.FromResult<Guid?>(RejectUpsert ? null : request.EventId ?? Guid.NewGuid());
        }

        public Task<bool> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            LastDeletedEventId = eventId;
            return Task.FromResult(DeleteResult);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            WasSaved = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEventOperationLogSink : IEventOperationLogSink
    {
        public List<EventOperationLogEntry> Entries { get; } = [];

        public Task WriteAsync(EventOperationLogEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }
    }
}
