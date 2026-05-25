namespace Anchor.Domain.Events;

public sealed record SaveEventRequest(
    Guid? EventId,
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
    DateOnly? RecursUntil);
