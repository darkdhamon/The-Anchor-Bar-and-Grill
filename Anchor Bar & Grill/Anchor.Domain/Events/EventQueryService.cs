namespace Anchor.Domain.Events;

public sealed class EventQueryService(IEventQueryRepository repository) : IEventQueryService
{
    private const int MaxDaysAhead = 3650;

    public async Task<IReadOnlyList<EventOccurrenceRecord>> GetUpcomingEventsAsync(
        DateTime localNow,
        int daysAhead = 30,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(daysAhead);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(daysAhead, MaxDaysAhead);

        var fromDate = DateOnly.FromDateTime(localNow);
        var throughDate = fromDate.AddDays(daysAhead);
        var candidates = await repository.GetUpcomingPublicEventCandidatesAsync(fromDate, throughDate, cancellationToken);

        List<EventOccurrenceRecord> occurrences = [];
        foreach (var record in candidates)
        {
            foreach (var occursOn in EventScheduleRules.GetOccurrences(record, fromDate, throughDate))
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
                    EventScheduleRules.GetScheduleSummary(record, localNow)));
            }
        }

        return occurrences
            .OrderBy(item => item.StartsAtLocal)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
