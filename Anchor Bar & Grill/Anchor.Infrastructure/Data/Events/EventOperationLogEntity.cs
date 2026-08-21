namespace Anchor.Infrastructure.Data.Events;

public sealed class EventOperationLogEntity
{
    public long EventOperationLogId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string Operation { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public string Summary { get; set; } = string.Empty;
}
