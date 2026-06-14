namespace Anchor.Domain.Events;

public sealed class EventQueryService(IEventQueryRepository repository) : IEventQueryService
{
    private const int MaxDaysAhead = 3650;

    public async Task<IReadOnlyList<EventOccurrenceRecord>> GetUpcomingEventsAsync(
        DateTime localNow,
        int daysAhead = 30,
        CancellationToken cancellationToken = default)
    {
        var fromDate = DateOnly.FromDateTime(localNow);
        return await LoadUpcomingEventsAsync(localNow, fromDate, daysAhead, cancellationToken);
    }

    public async Task<UpcomingEventWindowResult> GetUpcomingEventsWindowAsync(
        DateTime localNow,
        DateOnly fromDate,
        int daysAhead = 30,
        bool skipEmptyWindows = false,
        CancellationToken cancellationToken = default)
    {
        ValidateDaysAhead(daysAhead);

        var earliestDate = DateOnly.FromDateTime(localNow);
        var maxSearchDate = earliestDate.AddDays(MaxDaysAhead);
        var windowStart = fromDate < earliestDate ? earliestDate : fromDate;

        if (windowStart > maxSearchDate)
        {
            return new UpcomingEventWindowResult([], windowStart, false);
        }

        while (true)
        {
            var events = await LoadUpcomingEventsAsync(localNow, windowStart, daysAhead, cancellationToken);
            var nextFromDate = windowStart.AddDays(daysAhead + 1);
            var hasMore = nextFromDate <= maxSearchDate
                && await repository.HasUpcomingPublicEventCandidatesAsync(nextFromDate, cancellationToken);

            if (events.Count > 0 || !skipEmptyWindows || !hasMore)
            {
                return new UpcomingEventWindowResult(events, nextFromDate, hasMore);
            }

            windowStart = nextFromDate;
        }
    }

    private async Task<IReadOnlyList<EventOccurrenceRecord>> LoadUpcomingEventsAsync(
        DateTime localNow,
        DateOnly fromDate,
        int daysAhead,
        CancellationToken cancellationToken)
    {
        ValidateDaysAhead(daysAhead);

        var throughDate = fromDate.AddDays(daysAhead);
        var candidates = await repository.GetUpcomingPublicEventCandidatesAsync(fromDate, throughDate, cancellationToken);
        return ExpandOccurrences(candidates, localNow, fromDate, throughDate);
    }

    private static IReadOnlyList<EventOccurrenceRecord> ExpandOccurrences(
        IReadOnlyList<EventRecord> candidates,
        DateTime localNow,
        DateOnly fromDate,
        DateOnly throughDate)
    {
        List<EventOccurrenceRecord> occurrences = [];
        foreach (var record in candidates)
        {
            var expandedOccurrences = EventScheduleRules.GetOccurrences(record, fromDate, throughDate);
            foreach (var occursOn in expandedOccurrences)
            {
                var occursAt = occursOn.ToDateTime(record.StartsAt);
                if (occursAt < localNow)
                {
                    continue;
                }

                occurrences.Add(new EventOccurrenceRecord(
                    record.EventId,
                    record.Title,
                    record.Summary,
                    record.Description,
                    record.PromoBadge,
                    record.ImagePath,
                    occursOn,
                    record.StartsAt,
                    record.EndsAt,
                    record.EndsNextDay,
                    record.SortOrder,
                    record.IsRecurring,
                    EventScheduleRules.GetScheduleSummaryForOccurrence(record, occursOn)));
            }
        }

        return occurrences
            .OrderBy(item => item.StartsAtLocal)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void ValidateDaysAhead(int daysAhead)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(daysAhead);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(daysAhead, MaxDaysAhead);
    }
}
