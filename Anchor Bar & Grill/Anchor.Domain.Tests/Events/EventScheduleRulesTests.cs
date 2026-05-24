using Anchor.Domain.Events;

namespace Anchor.Domain.Tests.Events;

public sealed class EventScheduleRulesTests
{
    [Fact]
    public void Validate_rejects_one_time_events_with_recurring_fields()
    {
        var request = new SaveEventRequest(
            null,
            "Patio Party",
            "Season opener",
            "Kick off patio season.",
            "Seasonal",
            null,
            new DateOnly(2026, 6, 1),
            new TimeOnly(18, 0),
            null,
            false,
            1,
            EventPublicationState.Published,
            EventRecurrencePattern.None,
            1,
            DayOfWeek.Friday,
            null,
            new DateOnly(2026, 7, 1));

        var errors = EventScheduleRules.Validate(request);

        Assert.Contains(errors, error => error.Contains("One-time events", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetOccurrences_expands_open_ended_every_other_week_schedule()
    {
        var record = CreateRecord(
            EventRecurrencePattern.Weekly,
            startsOn: new DateOnly(2026, 5, 1),
            recursOnDayOfWeek: DayOfWeek.Friday,
            recurrenceInterval: 2);

        var occurrences = EventScheduleRules.GetOccurrences(
            record,
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 6, 12));

        Assert.Equal(
            [
                new DateOnly(2026, 5, 1),
                new DateOnly(2026, 5, 15),
                new DateOnly(2026, 5, 29),
                new DateOnly(2026, 6, 12)
            ],
            occurrences);
    }

    [Fact]
    public void GetOccurrences_expands_monthly_nth_weekday_schedule()
    {
        var record = CreateRecord(
            EventRecurrencePattern.MonthlyNthWeekday,
            startsOn: new DateOnly(2026, 5, 15),
            recursOnDayOfWeek: DayOfWeek.Friday,
            recursOnWeekOfMonth: EventRecurrenceWeek.Third,
            recurrenceInterval: 1);

        var occurrences = EventScheduleRules.GetOccurrences(
            record,
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 7, 31));

        Assert.Equal(
            [
                new DateOnly(2026, 5, 15),
                new DateOnly(2026, 6, 19),
                new DateOnly(2026, 7, 17)
            ],
            occurrences);
    }

    [Fact]
    public void GetNextOccurrence_skips_event_times_that_already_passed_today()
    {
        var record = CreateRecord(
            EventRecurrencePattern.Weekly,
            startsOn: new DateOnly(2026, 5, 18),
            startsAt: new TimeOnly(17, 0),
            recursOnDayOfWeek: DayOfWeek.Monday,
            recurrenceInterval: 1);

        var nextOccurrence = EventScheduleRules.GetNextOccurrence(
            record,
            new DateTime(2026, 5, 18, 18, 0, 0));

        Assert.Equal(new DateOnly(2026, 5, 25), nextOccurrence);
    }

    private static EventRecord CreateRecord(
        EventRecurrencePattern recurrencePattern,
        DateOnly startsOn,
        TimeOnly? startsAt = null,
        DayOfWeek? recursOnDayOfWeek = null,
        EventRecurrenceWeek? recursOnWeekOfMonth = null,
        int recurrenceInterval = 1) =>
        new(
            Guid.NewGuid(),
            "Test Event",
            "Summary",
            "Description",
            null,
            null,
            startsOn,
            startsAt ?? new TimeOnly(19, 0),
            null,
            false,
            1,
            EventPublicationState.Published,
            recurrencePattern,
            recurrenceInterval,
            recursOnDayOfWeek,
            recursOnWeekOfMonth,
            null);
}
