namespace Anchor.Domain.Menu;

public interface IMenuOperationLogSink
{
    Task WriteAsync(MenuOperationLogEntry entry, CancellationToken cancellationToken = default);
}

public sealed record MenuOperationLogEntry(string Operation, string TargetType, Guid? TargetId, string Summary);
