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

    private static EventRecord CreateRecord(
        string title,
        DateOnly startsOn,
        TimeOnly startsAt,
        int sortOrder,
        EventRecurrencePattern recurrencePattern = EventRecurrencePattern.None,
        DayOfWeek? recursOnDayOfWeek = null,
        EventRecurrenceWeek? recursOnWeekOfMonth = null) =>
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
            1,
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
    }
}
