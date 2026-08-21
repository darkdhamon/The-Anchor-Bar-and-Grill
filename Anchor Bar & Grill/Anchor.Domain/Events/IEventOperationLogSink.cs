namespace Anchor.Domain.Events;

public interface IEventOperationLogSink
{
    Task WriteAsync(EventOperationLogEntry entry, CancellationToken cancellationToken = default);
}

public sealed record EventOperationLogEntry(string Operation, Guid EventId, string Summary);
