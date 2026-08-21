using Anchor.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Anchor.Infrastructure.Data.Events;

public sealed class EventOperationLogSink : IEventOperationLogSink
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);
    private readonly ApplicationDbContext dbContext;
    private readonly ILogger<EventOperationLogSink> logger;
    private readonly string fallbackLogPath;

    public EventOperationLogSink(ApplicationDbContext dbContext, ILogger<EventOperationLogSink> logger)
        : this(dbContext, logger, Path.Combine(AppContext.BaseDirectory, "logs", "event-operations.log"))
    {
    }

    public EventOperationLogSink(ApplicationDbContext dbContext, ILogger<EventOperationLogSink> logger, string fallbackLogPath)
    {
        this.dbContext = dbContext;
        this.logger = logger;
        this.fallbackLogPath = fallbackLogPath;
    }

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
        catch (Exception exception) when (exception is InvalidOperationException or ObjectDisposedException or Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            try
            {
                dbContext.ChangeTracker.Clear();
            }
            catch (ObjectDisposedException)
            {
                // The fallback remains available even when the scoped context has already been disposed.
            }
            logger.LogError(exception, "Could not persist the event operation log to the database; using the fallback file.");
        }

        var logPath = fallbackLogPath;
        var logDirectory = Path.GetDirectoryName(logPath) ?? AppContext.BaseDirectory;
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


    public async Task<IReadOnlyList<EventOperationLogRecord>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        var databaseRecords = await dbContext.EventOperationLogs
            .AsNoTracking()
            .OrderByDescending(item => item.EventOperationLogId)
            .Take(count)
            .Select(item => new EventOperationLogRecord(item.OccurredAtUtc, item.Operation, item.EventId, item.Summary))
            .ToListAsync(cancellationToken);

        var fallbackRecords = await ReadFallbackRecordsAsync(cancellationToken);
        return databaseRecords
            .Concat(fallbackRecords)
            .OrderByDescending(item => item.OccurredAtUtc)
            .Take(count)
            .ToArray();
    }

    private async Task<IReadOnlyList<EventOperationLogRecord>> ReadFallbackRecordsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(fallbackLogPath))
        {
            return [];
        }

        try
        {
            var records = new List<EventOperationLogRecord>();
            foreach (var line in await File.ReadAllLinesAsync(fallbackLogPath, cancellationToken))
            {
                var parts = line.Split('\t', 4);
                if (parts.Length == 4
                    && DateTimeOffset.TryParse(parts[0], out var occurredAtUtc)
                    && Guid.TryParse(parts[2], out var eventId))
                {
                    records.Add(new EventOperationLogRecord(occurredAtUtc, parts[1], eventId, parts[3]));
                }
            }

            return records;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            logger.LogError(exception, "Could not read the event operation fallback log at {LogPath}.", fallbackLogPath);
            return [];
        }
    }
}
