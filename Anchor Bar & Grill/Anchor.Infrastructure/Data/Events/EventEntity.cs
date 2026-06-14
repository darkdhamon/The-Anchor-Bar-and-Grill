using Anchor.Domain.Events;

namespace Anchor.Infrastructure.Data.Events;

public sealed class EventEntity
{
    public Guid EventId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? PromoBadge { get; set; }

    public string? ImagePath { get; set; }

    public DateOnly StartsOn { get; set; }

    public TimeOnly StartsAt { get; set; }

    public TimeOnly? EndsAt { get; set; }

    public bool EndsNextDay { get; set; }

    public string? TimingNotes { get; set; }

    public int SortOrder { get; set; }

    public EventPublicationState PublicationState { get; set; }

    public EventRecurrencePattern RecurrencePattern { get; set; }

    public int RecurrenceInterval { get; set; }

    public DayOfWeek? RecursOnDayOfWeek { get; set; }

    public EventRecurrenceWeek? RecursOnWeekOfMonth { get; set; }

    public DateOnly? RecursUntil { get; set; }
}
