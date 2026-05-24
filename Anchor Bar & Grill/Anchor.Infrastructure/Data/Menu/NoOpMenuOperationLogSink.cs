using Anchor.Domain.Menu;

namespace Anchor.Infrastructure.Data.Menu;

public sealed class NoOpMenuOperationLogSink : IMenuOperationLogSink
{
    public Task WriteAsync(MenuOperationLogEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
