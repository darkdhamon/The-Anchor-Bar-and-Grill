using System.Globalization;

namespace Anchor.Domain.Events;

public sealed record EventRecord(
    Guid EventId,
    string Title,
    string Summary,
    string Description,
    string? PromoBadge,
    string? ImagePath,
    DateOnly StartsOn,
    TimeOnly StartsAt,
    TimeOnly? EndsAt,
    bool EndsNextDay,
    int SortOrder,
    EventPublicationState PublicationState,
    EventRecurrencePattern RecurrencePattern,
    int RecurrenceInterval,
    DayOfWeek? RecursOnDayOfWeek,
    EventRecurrenceWeek? RecursOnWeekOfMonth,
    DateOnly? RecursUntil)
{
    public bool IsRecurring => RecurrencePattern != EventRecurrencePattern.None;
}

public sealed record EventOccurrenceRecord(
    Guid EventId,
    string Title,
    string Summary,
    string Description,
    string? PromoBadge,
    string? ImagePath,
    DateOnly OccursOn,
    TimeOnly StartsAt,
    TimeOnly? EndsAt,
    bool EndsNextDay,
    int SortOrder,
    bool IsRecurring,
    string ScheduleSummary)
{
    public DateTime StartsAtLocal => OccursOn.ToDateTime(StartsAt);

    public string TimeLabel => StartsAt.ToString("h:mm tt", CultureInfo.InvariantCulture);
}
