using Anchor.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Anchor.Infrastructure.Data.Events;

public sealed class EventOperationLogSink(ILogger<EventOperationLogSink> logger) : IEventOperationLogSink
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    public async Task WriteAsync(EventOperationLogEntry entry, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Event management operation {Operation} completed for event {EventId}: {Summary}",
            entry.Operation,
            entry.EventId,
            entry.Summary);

        var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        var logPath = Path.Combine(logDirectory, "event-operations.log");
        var lockAcquired = false;
        try
        {
            await FileLock.WaitAsync(cancellationToken);
            lockAcquired = true;
            Directory.CreateDirectory(logDirectory);
            var line = $"{DateTimeOffset.UtcNow:O}\t{entry.Operation}\t{entry.EventId}\t{entry.Summary}{Environment.NewLine}";
            await File.AppendAllTextAsync(logPath, line, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Could not write the event operation fallback log at {LogPath}.", logPath);
        }
        finally
        {
            if (lockAcquired)
            {
                FileLock.Release();
            }
        }
    }
}
