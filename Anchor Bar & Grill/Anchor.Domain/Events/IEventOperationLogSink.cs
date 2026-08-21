namespace Anchor.Domain.Events;

public interface IEventOperationLogSink
{
    Task WriteAsync(EventOperationLogEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EventOperationLogRecord>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
}

public sealed record EventOperationLogEntry(string Operation, Guid EventId, string Summary);

public sealed record EventOperationLogRecord(DateTimeOffset OccurredAtUtc, string Operation, Guid EventId, string Summary);
