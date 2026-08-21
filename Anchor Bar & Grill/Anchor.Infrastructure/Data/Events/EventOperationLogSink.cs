using Anchor.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Anchor.Infrastructure.Data.Events;

public sealed class EventOperationLogSink(
    ApplicationDbContext dbContext,
    ILogger<EventOperationLogSink> logger) : IEventOperationLogSink
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    public async Task WriteAsync(EventOperationLogEntry entry, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Event management operation {Operation} completed for event {EventId}: {Summary}",
            entry.Operation,
            entry.EventId,
            entry.Summary);

        var occurredAtUtc = DateTimeOffset.UtcNow;
        try
        {
            dbContext.EventOperationLogs.Add(new EventOperationLogEntity
            {
                OccurredAtUtc = occurredAtUtc,
                Operation = entry.Operation,
                EventId = entry.EventId,
                Summary = entry.Summary
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }
        catch (Exception exception) when (exception is InvalidOperationException or Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            logger.LogError(exception, "Could not persist the event operation log to the database; using the fallback file.");
        }

        var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        var logPath = Path.Combine(logDirectory, "event-operations.log");
        var lockAcquired = false;
        try
        {
            await FileLock.WaitAsync(cancellationToken);
            lockAcquired = true;
            Directory.CreateDirectory(logDirectory);
            var line = $"{occurredAtUtc:O}\t{entry.Operation}\t{entry.EventId}\t{entry.Summary}{Environment.NewLine}";
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


    public async Task<IReadOnlyList<EventOperationLogRecord>> GetRecentAsync(int count, CancellationToken cancellationToken = default) =>
        await dbContext.EventOperationLogs
            .AsNoTracking()
            .OrderByDescending(item => item.EventOperationLogId)
            .Take(count)
            .Select(item => new EventOperationLogRecord(item.OccurredAtUtc, item.Operation, item.EventId, item.Summary))
            .ToListAsync(cancellationToken);
}
