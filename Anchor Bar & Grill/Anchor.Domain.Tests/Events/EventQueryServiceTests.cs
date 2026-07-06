using Anchor.Domain.Events;

namespace Anchor.Domain.Tests.Events;

public sealed class EventQueryServiceTests
{
    [Fact]
    public async Task GetUpcomingEventsAsync_filters_passed_occurrences_and_orders_results()
    {
        var today = new DateOnly(2026, 5, 18);
        var repository = new FakeEventQueryRepository
        {
            Events =
            [
                CreateRecord("Earlier Today", today, new TimeOnly(9, 0), 2),
                CreateRecord("Tonight", today, new TimeOnly(20, 0), 3),
                CreateRecord(
                    "Monday Trivia",
                    today,
                    new TimeOnly(19, 0),
                    1,
                    EventRecurrencePattern.Weekly,
                    DayOfWeek.Monday)
            ]
        };

        var results = await new EventQueryService(repository).GetUpcomingEventsAsync(new DateTime(2026, 5, 18, 12, 0, 0), 7);

        Assert.Equal(
            ["Monday Trivia", "Tonight", "Monday Trivia"],
            results.Select(item => item.Title).ToArray());
        Assert.DoesNotContain(results, item => item.Title == "Earlier Today");
    }

    [Fact]
    public async Task GetUpcomingEventsAsync_uses_schedule_summary_for_monthly_recurrence()
    {
        var repository = new FakeEventQueryRepository
        {
            Events =
            [
                CreateRecord(
                    "Steak Night",
                    new DateOnly(2026, 5, 15),
                    new TimeOnly(18, 30),
                    1,
                    EventRecurrencePattern.MonthlyNthWeekday,
                    DayOfWeek.Friday,
                    EventRecurrenceWeek.Third)
            ]
        };

        var results = await new EventQueryService(repository).GetUpcomingEventsAsync(new DateTime(2026, 5, 10, 10, 0, 0), 45);

        Assert.NotEmpty(results);
        Assert.All(
            results,
            occurrence => Assert.Contains("third Friday", occurrence.ScheduleSummary, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetUpcomingEventsAsync_rejects_daysAhead_values_that_would_overflow_date_math()
    {
        var service = new EventQueryService(new FakeEventQueryRepository());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetUpcomingEventsAsync(new DateTime(2026, 5, 10, 10, 0, 0), int.MaxValue));
    }

    [Fact]
    public async Task GetUpcomingEventsAsync_computes_schedule_summary_from_each_emitted_occurrence()
    {
        var repository = new FakeEventQueryRepository
        {
            Events =
            [
                CreateRecord(
                    "Friday Live Music",
                    new DateOnly(2026, 5, 15),
                    new TimeOnly(20, 30),
                    1,
                    EventRecurrencePattern.Weekly,
                    DayOfWeek.Friday)
            ]
        };

        var results = await new EventQueryService(repository).GetUpcomingEventsAsync(new DateTime(2026, 5, 10, 10, 0, 0), 14);

        Assert.Equal(2, results.Count);
        Assert.Contains("next on May 15, 2026", results[0].ScheduleSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("next on May 22, 2026", results[1].ScheduleSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetUpcomingEventsAsync_skips_recurring_rows_with_invalid_persisted_schedule_values()
    {
        var repository = new FakeEventQueryRepository
        {
            Events =
            [
                CreateRecord(
                    "Valid Trivia",
                    new DateOnly(2026, 5, 18),
                    new TimeOnly(19, 0),
                    1,
                    EventRecurrencePattern.Weekly,
                    DayOfWeek.Monday),
                CreateRecord(
                    "Broken Import",
                    new DateOnly(2026, 5, 15),
                    new TimeOnly(20, 0),
                    2,
                    EventRecurrencePattern.MonthlyNthWeekday,
                    (DayOfWeek)99,
                    (EventRecurrenceWeek)99,
                    recurrenceInterval: 0)
            ]
        };

        var results = await new EventQueryService(repository).GetUpcomingEventsAsync(new DateTime(2026, 5, 10, 10, 0, 0), 14);

        Assert.DoesNotContain(results, item => item.Title == "Broken Import");
        Assert.Contains(results, item => item.Title == "Valid Trivia");
    }

    [Fact]
    public async Task GetUpcomingEventsWindowAsync_reports_the_next_window_boundary_and_future_availability()
    {
        var today = new DateOnly(2026, 5, 18);
        var repository = new FakeEventQueryRepository
        {
            Events =
            [
                CreateRecord("Tonight", today, new TimeOnly(20, 0), 1),
                CreateRecord("Labor Day Patio Party", new DateOnly(2026, 9, 5), new TimeOnly(18, 0), 2)
            ]
        };

        var result = await new EventQueryService(repository).GetUpcomingEventsWindowAsync(
            new DateTime(2026, 5, 18, 12, 0, 0),
            today,
            30);

        Assert.Equal(["Tonight"], result.Events.Select(item => item.Title).ToArray());
        Assert.Equal(new DateOnly(2026, 6, 18), result.NextFromDate);
        Assert.True(result.HasMore);
    }

    [Fact]
    public async Task GetUpcomingEventsWindowAsync_can_skip_empty_windows_for_progressive_loading()
    {
        var repository = new FakeEventQueryRepository
        {
            Events =
            [
                CreateRecord("Labor Day Patio Party", new DateOnly(2026, 9, 5), new TimeOnly(18, 0), 1)
            ]
        };

        var result = await new EventQueryService(repository).GetUpcomingEventsWindowAsync(
            new DateTime(2026, 5, 18, 12, 0, 0),
            new DateOnly(2026, 8, 1),
            30,
            skipEmptyWindows: true);

        Assert.Single(result.Events);
        Assert.Equal("Labor Day Patio Party", result.Events[0].Title);
        Assert.Equal(new DateOnly(2026, 10, 2), result.NextFromDate);
    }

    private static EventRecord CreateRecord(
        string title,
        DateOnly startsOn,
        TimeOnly startsAt,
        int sortOrder,
        EventRecurrencePattern recurrencePattern = EventRecurrencePattern.None,
        DayOfWeek? recursOnDayOfWeek = null,
        EventRecurrenceWeek? recursOnWeekOfMonth = null,
        int recurrenceInterval = 1) =>
        new(
            Guid.NewGuid(),
            title,
            "Summary",
            "Description",
            null,
            null,
            startsOn,
            startsAt,
            null,
            false,
            sortOrder,
            EventPublicationState.Published,
            recurrencePattern,
            recurrenceInterval,
            recursOnDayOfWeek,
            recursOnWeekOfMonth,
            null);

    private sealed class FakeEventQueryRepository : IEventQueryRepository
    {
        public IReadOnlyList<EventRecord> Events { get; init; } = [];

        public Task<IReadOnlyList<EventRecord>> GetUpcomingPublicEventCandidatesAsync(
            DateOnly fromDate,
            DateOnly throughDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Events);

        public Task<bool> HasUpcomingPublicEventCandidatesAsync(
            DateOnly fromDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Events.Any(item =>
                    item.RecurrencePattern == EventRecurrencePattern.None
                        ? item.StartsOn >= fromDate
                        : IsRenderableRecurringRow(item) && (item.RecursUntil == null || item.RecursUntil >= fromDate)));

        private static bool IsRenderableRecurringRow(EventRecord item)
        {
            if (item.RecurrencePattern == EventRecurrencePattern.Weekly)
            {
                return EventScheduleRules.IsSupportedRecurringInterval(EventRecurrencePattern.Weekly, item.RecurrenceInterval)
                    && item.RecursOnDayOfWeek is >= DayOfWeek.Sunday and <= DayOfWeek.Saturday;
            }

            if (item.RecurrencePattern == EventRecurrencePattern.MonthlyNthWeekday)
            {
                return EventScheduleRules.IsSupportedRecurringInterval(EventRecurrencePattern.MonthlyNthWeekday, item.RecurrenceInterval)
                    && item.RecursOnDayOfWeek is >= DayOfWeek.Sunday and <= DayOfWeek.Saturday
                    && item.RecursOnWeekOfMonth is >= EventRecurrenceWeek.First and <= EventRecurrenceWeek.Last;
            }

            return false;
        }
    }
}
