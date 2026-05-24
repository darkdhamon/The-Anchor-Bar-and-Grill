using Anchor.Domain.Events;

namespace Anchor.Domain.Tests.Events;

public sealed class EventManagementServiceTests
{
    [Fact]
    public async Task SaveEventAsync_returns_validation_errors_without_writing()
    {
        var repository = new FakeEventManagementRepository();
        var service = new EventManagementService(repository);

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
    public async Task DeleteEventAsync_reports_missing_event()
    {
        var repository = new FakeEventManagementRepository { DeleteResult = false };
        var service = new EventManagementService(repository);

        var result = await service.DeleteEventAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, error => error.Contains("not found", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeEventManagementRepository : IEventManagementRepository
    {
        public bool DeleteResult { get; init; } = true;

        public bool WasSaved { get; private set; }

        public Task<IReadOnlyList<EventRecord>> GetEventsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EventRecord>>([]);

        public Task<Guid> UpsertEventAsync(SaveEventRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(request.EventId ?? Guid.NewGuid());

        public Task<bool> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            Task.FromResult(DeleteResult);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            WasSaved = true;
            return Task.CompletedTask;
        }
    }
}
