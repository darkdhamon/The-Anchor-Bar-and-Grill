using Anchor.Domain.Events;
using Anchor.Infrastructure.Data.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Anchor.Infrastructure.Tests.Support;

namespace Anchor.Infrastructure.Tests.Data;

public sealed class EventRepositoriesTests
{
    [Fact]
    public async Task EventOperationLogSink_persists_and_reads_recent_activity()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var sink = new EventOperationLogSink(context.DbContext, NullLogger<EventOperationLogSink>.Instance);
        var eventId = Guid.NewGuid();

        await sink.WriteAsync(new EventOperationLogEntry("publish", eventId, "Published patio party"));
        var records = await sink.GetRecentAsync(25);

        var record = Assert.Single(records);
        Assert.Equal("publish", record.Operation);
        Assert.Equal(eventId, record.EventId);
        Assert.Equal("Published patio party", record.Summary);
    }

    [Fact]
    public async Task EventOperationLogSink_writes_fallback_file_when_database_is_unavailable()
    {
        var context = await SqliteIdentityTestContext.CreateAsync();
        var fallbackPath = Path.Combine(Path.GetTempPath(), $"anchor-event-log-{Guid.NewGuid():N}.log");
        var sink = new EventOperationLogSink(context.DbContext, NullLogger<EventOperationLogSink>.Instance, fallbackPath);
        var eventId = Guid.NewGuid();
        await context.DisposeAsync();

        try
        {
            await sink.WriteAsync(new EventOperationLogEntry("delete", eventId, "Deleted event"));

            var contents = await File.ReadAllTextAsync(fallbackPath);
            Assert.Contains("delete", contents);
            Assert.Contains(eventId.ToString(), contents);
            Assert.Contains("Deleted event", contents);
        }
        finally
        {
            File.Delete(fallbackPath);
        }
    }

    [Fact]
    public async Task EventOperationLogSink_merges_fallback_activity_into_admin_results()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var fallbackPath = Path.Combine(Path.GetTempPath(), $"anchor-event-log-{Guid.NewGuid():N}.log");
        var eventId = Guid.NewGuid();
        await File.WriteAllTextAsync(
            fallbackPath,
            $"2026-08-21T04:00:00.0000000+00:00\tdelete\t{eventId}\tDeleted during database outage{Environment.NewLine}");

        try
        {
            var sink = new EventOperationLogSink(context.DbContext, NullLogger<EventOperationLogSink>.Instance, fallbackPath);

            var records = await sink.GetRecentAsync(25);

            var record = Assert.Single(records);
            Assert.Equal("delete", record.Operation);
            Assert.Equal(eventId, record.EventId);
            Assert.Equal("Deleted during database outage", record.Summary);
        }
        finally
        {
            File.Delete(fallbackPath);
        }
    }

    [Fact]
    public async Task GetUpcomingPublicEventCandidatesAsync_returns_only_publishable_future_candidates()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();

        context.DbContext.Events.AddRange(
            new EventEntity
            {
                EventId = Guid.NewGuid(),
                Title = "Published Tonight",
                Summary = "Summary",
                Description = "Description",
                StartsOn = new DateOnly(2026, 5, 18),
                StartsAt = new TimeOnly(19, 0),
                SortOrder = 1,
                PublicationState = EventPublicationState.Published,
                RecurrencePattern = EventRecurrencePattern.None,
                RecurrenceInterval = 1
            },
            new EventEntity
            {
                EventId = Guid.NewGuid(),
                Title = "Draft Tonight",
                Summary = "Summary",
                Description = "Description",
                StartsOn = new DateOnly(2026, 5, 18),
                StartsAt = new TimeOnly(20, 0),
                SortOrder = 2,
                PublicationState = EventPublicationState.Draft,
                RecurrencePattern = EventRecurrencePattern.None,
                RecurrenceInterval = 1
            },
            new EventEntity
            {
                EventId = Guid.NewGuid(),
                Title = "Archived Recurring",
                Summary = "Summary",
                Description = "Description",
                StartsOn = new DateOnly(2026, 5, 1),
                StartsAt = new TimeOnly(17, 0),
                SortOrder = 3,
                PublicationState = EventPublicationState.Archived,
                RecurrencePattern = EventRecurrencePattern.Weekly,
                RecurrenceInterval = 1,
                RecursOnDayOfWeek = DayOfWeek.Monday
            },
            new EventEntity
            {
                EventId = Guid.NewGuid(),
                Title = "Published Weekly",
                Summary = "Summary",
                Description = "Description",
                StartsOn = new DateOnly(2026, 5, 4),
                StartsAt = new TimeOnly(18, 0),
                SortOrder = 4,
                PublicationState = EventPublicationState.Published,
                RecurrencePattern = EventRecurrencePattern.Weekly,
                RecurrenceInterval = 1,
                RecursOnDayOfWeek = DayOfWeek.Monday
            });
        await context.DbContext.SaveChangesAsync();

        var repository = new EventQueryRepository(context.DbContext);
        var results = await repository.GetUpcomingPublicEventCandidatesAsync(
            new DateOnly(2026, 5, 18),
            new DateOnly(2026, 5, 25));

        Assert.Equal(
            ["Published Tonight", "Published Weekly"],
            results.Select(item => item.Title).ToArray());
    }

    [Fact]
    public async Task UpsertEventAsync_persists_and_normalizes_recurring_fields()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new EventManagementRepository(context.DbContext);

        var eventId = await repository.UpsertEventAsync(
            new SaveEventRequest(
                null,
                " Friday Live Music ",
                " Rotating acts ",
                " Full description. ",
                " Live Music ",
                " images/events/live-music.svg ",
                new DateOnly(2026, 5, 22),
                new TimeOnly(20, 30),
                new TimeOnly(23, 0),
                false,
                9,
                EventPublicationState.Published,
                EventRecurrencePattern.Weekly,
                2,
                DayOfWeek.Friday,
                EventRecurrenceWeek.Third,
                new DateOnly(2026, 7, 31),
                "  Estimated timing may shift based on parade schedule.  "));
        await repository.SaveChangesAsync();

        var saved = await context.DbContext.Events.FindAsync(eventId);

        Assert.NotNull(saved);
        Assert.Equal("Friday Live Music", saved!.Title);
        Assert.Equal("Rotating acts", saved.Summary);
        Assert.Equal("Full description.", saved.Description);
        Assert.Equal("Live Music", saved.PromoBadge);
        Assert.Equal("images/events/live-music.svg", saved.ImagePath);
        Assert.Equal(EventRecurrencePattern.Weekly, saved.RecurrencePattern);
        Assert.Equal(2, saved.RecurrenceInterval);
        Assert.Equal(DayOfWeek.Friday, saved.RecursOnDayOfWeek);
        Assert.Null(saved.RecursOnWeekOfMonth);
        Assert.Equal(new DateOnly(2026, 7, 31), saved.RecursUntil);
        Assert.Equal("Estimated timing may shift based on parade schedule.", saved.TimingNotes);
    }

    [Fact]
    public async Task UpsertEventAsync_allows_null_ends_time_and_timing_notes()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new EventManagementRepository(context.DbContext);

        var eventId = await repository.UpsertEventAsync(
            new SaveEventRequest(
                null,
                "Paddlefish Day Feature",
                "Parade-linked set",
                "Timing will depend on the downtown float route.",
                "Live Music",
                null,
                new DateOnly(2026, 9, 12),
                new TimeOnly(13, 0),
                null,
                false,
                7,
                EventPublicationState.Published,
                EventRecurrencePattern.None,
                0,
                null,
                null,
                null));
        await repository.SaveChangesAsync();

        var saved = await context.DbContext.Events.FindAsync(eventId);

        Assert.NotNull(saved);
        Assert.Equal("Paddlefish Day Feature", saved!.Title);
        Assert.Null(saved.EndsAt);
        Assert.Null(saved.TimingNotes);
    }

    [Fact]
    public async Task UpsertEventAsync_updates_existing_event_and_publication_state()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new EventManagementRepository(context.DbContext);
        var eventId = Guid.NewGuid();

        context.DbContext.Events.Add(
            new EventEntity
            {
                EventId = eventId,
                Title = "Friday Live Music",
                Summary = "Draft summary",
                Description = "Draft description",
                PromoBadge = "Live Music",
                StartsOn = new DateOnly(2026, 5, 22),
                StartsAt = new TimeOnly(20, 0),
                SortOrder = 3,
                PublicationState = EventPublicationState.Draft,
                RecurrencePattern = EventRecurrencePattern.Weekly,
                RecurrenceInterval = 1,
                RecursOnDayOfWeek = DayOfWeek.Friday
            });
        await context.DbContext.SaveChangesAsync();

        await repository.UpsertEventAsync(
            new SaveEventRequest(
                eventId,
                "Friday Live Music Updated",
                "Published summary",
                "Published description",
                "Community Night",
                null,
                new DateOnly(2026, 5, 22),
                new TimeOnly(20, 30),
                null,
                false,
                1,
                EventPublicationState.Published,
                EventRecurrencePattern.Weekly,
                2,
                DayOfWeek.Friday,
                null,
                new DateOnly(2026, 9, 25),
                "Timing may shift slightly."),
            CancellationToken.None);
        await repository.SaveChangesAsync();

        var saved = await context.DbContext.Events.FindAsync(eventId);

        Assert.NotNull(saved);
        Assert.Equal("Friday Live Music Updated", saved!.Title);
        Assert.Equal("Published summary", saved.Summary);
        Assert.Equal(EventPublicationState.Published, saved.PublicationState);
        Assert.Equal(2, saved.RecurrenceInterval);
        Assert.Equal(new DateOnly(2026, 9, 25), saved.RecursUntil);
        Assert.Equal("Timing may shift slightly.", saved.TimingNotes);
    }

    [Fact]
    public async Task UpsertEventAsync_rejects_an_update_for_a_missing_event()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new EventManagementRepository(context.DbContext);

        var result = await repository.UpsertEventAsync(new SaveEventRequest(
            Guid.NewGuid(), "Deleted event", "Summary", "Description", null, null,
            new DateOnly(2026, 5, 22), new TimeOnly(20, 0), null, false, 1,
            EventPublicationState.Draft, EventRecurrencePattern.None, 1, null, null, null));

        Assert.Null(result);
        Assert.Empty(context.DbContext.Events);
    }

    [Fact]
    public async Task UpsertEventAsync_rejects_a_stale_revision_without_overwriting_the_event()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var eventId = Guid.NewGuid();
        var currentRevision = Guid.NewGuid();
        context.DbContext.Events.Add(new EventEntity
        {
            EventId = eventId,
            Revision = currentRevision,
            Title = "Current title",
            Summary = "Summary",
            Description = "Description",
            StartsOn = new DateOnly(2026, 5, 22),
            StartsAt = new TimeOnly(20, 0),
            SortOrder = 1,
            PublicationState = EventPublicationState.Draft,
            RecurrencePattern = EventRecurrencePattern.None,
            RecurrenceInterval = 1
        });
        await context.DbContext.SaveChangesAsync();
        var repository = new EventManagementRepository(context.DbContext);
        var request = new SaveEventRequest(
            eventId, "Stale title", "Summary", "Description", null, null,
            new DateOnly(2026, 5, 22), new TimeOnly(20, 0), null, false, 1,
            EventPublicationState.Draft, EventRecurrencePattern.None, 1, null, null, null)
        {
            ExpectedRevision = Guid.NewGuid()
        };

        var result = await repository.UpsertEventAsync(request);

        Assert.Null(result);
        var unchangedEvent = context.DbContext.Events.Single();
        Assert.Equal("Current title", unchangedEvent.Title);
        Assert.Equal(currentRevision, unchangedEvent.Revision);
    }

    [Fact]
    public async Task GetEventsAsync_returns_only_the_requested_page_and_catalog_totals()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        context.DbContext.Events.AddRange(Enumerable.Range(1, 12).Select(index => new EventEntity
        {
            EventId = Guid.NewGuid(),
            Title = $"Event {index:D2}",
            Summary = "Summary",
            Description = "Description",
            StartsOn = new DateOnly(2026, 5, 18),
            StartsAt = new TimeOnly(18, 0),
            SortOrder = index,
            PublicationState = EventPublicationState.Draft,
            RecurrencePattern = EventRecurrencePattern.None,
            RecurrenceInterval = 1
        }));
        await context.DbContext.SaveChangesAsync();
        var repository = new EventManagementRepository(context.DbContext);

        var page = await repository.GetEventsAsync(10, 10);

        Assert.Equal(12, page.TotalCount);
        Assert.Equal(12, page.MaxSortOrder);
        Assert.Equal(["Event 11", "Event 12"], page.Items.Select(item => item.Title).ToArray());
    }

    [Fact]
    public async Task GetEventsAsync_uses_event_id_as_a_stable_tiebreaker()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var ids = Enumerable.Range(1, 3).Select(_ => Guid.NewGuid()).Order().ToArray();
        context.DbContext.Events.AddRange(ids.Select(id => new EventEntity
        {
            EventId = id,
            Title = "Tied event",
            Summary = "Summary",
            Description = "Description",
            StartsOn = new DateOnly(2026, 5, 18),
            StartsAt = new TimeOnly(18, 0),
            SortOrder = 1,
            PublicationState = EventPublicationState.Draft,
            RecurrencePattern = EventRecurrencePattern.None,
            RecurrenceInterval = 1
        }));
        await context.DbContext.SaveChangesAsync();
        var repository = new EventManagementRepository(context.DbContext);

        var firstPage = await repository.GetEventsAsync(0, 2);
        var secondPage = await repository.GetEventsAsync(2, 2);

        Assert.Equal(ids.Take(2), firstPage.Items.Select(item => item.EventId));
        Assert.Equal(ids.Skip(2), secondPage.Items.Select(item => item.EventId));
        Assert.Equal(2, await repository.GetEventIndexAsync(ids[2]));
    }

    [Fact]
    public async Task HasUpcomingPublicEventCandidatesAsync_ignores_unpublished_and_expired_records()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();

        context.DbContext.Events.AddRange(
            new EventEntity
            {
                EventId = Guid.NewGuid(),
                Title = "Published Tonight",
                Summary = "Summary",
                Description = "Description",
                StartsOn = new DateOnly(2026, 5, 18),
                StartsAt = new TimeOnly(19, 0),
                SortOrder = 1,
                PublicationState = EventPublicationState.Published,
                RecurrencePattern = EventRecurrencePattern.None,
                RecurrenceInterval = 1
            },
            new EventEntity
            {
                EventId = Guid.NewGuid(),
                Title = "Draft Patio Party",
                Summary = "Summary",
                Description = "Description",
                StartsOn = new DateOnly(2026, 6, 1),
                StartsAt = new TimeOnly(18, 0),
                SortOrder = 2,
                PublicationState = EventPublicationState.Draft,
                RecurrencePattern = EventRecurrencePattern.None,
                RecurrenceInterval = 1
            },
            new EventEntity
            {
                EventId = Guid.NewGuid(),
                Title = "Published Weekly",
                Summary = "Summary",
                Description = "Description",
                StartsOn = new DateOnly(2026, 5, 4),
                StartsAt = new TimeOnly(18, 0),
                SortOrder = 3,
                PublicationState = EventPublicationState.Published,
                RecurrencePattern = EventRecurrencePattern.Weekly,
                RecurrenceInterval = 1,
                RecursOnDayOfWeek = DayOfWeek.Monday,
                RecursUntil = new DateOnly(2026, 5, 18)
            });
        await context.DbContext.SaveChangesAsync();

        var repository = new EventQueryRepository(context.DbContext);

        Assert.True(await repository.HasUpcomingPublicEventCandidatesAsync(new DateOnly(2026, 5, 18)));
        Assert.False(await repository.HasUpcomingPublicEventCandidatesAsync(new DateOnly(2026, 5, 19)));
    }

    [Fact]
    public async Task HasUpcomingPublicEventCandidatesAsync_ignores_invalid_recurring_rows()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();

        context.DbContext.Events.AddRange(
            new EventEntity
            {
                EventId = Guid.NewGuid(),
                Title = "Published Broken Recurring",
                Summary = "Summary",
                Description = "Description",
                StartsOn = new DateOnly(2026, 5, 4),
                StartsAt = new TimeOnly(18, 0),
                SortOrder = 1,
                PublicationState = EventPublicationState.Published,
                RecurrencePattern = EventRecurrencePattern.MonthlyNthWeekday,
                RecurrenceInterval = 0,
                RecursOnDayOfWeek = DayOfWeek.Monday,
                RecursOnWeekOfMonth = (EventRecurrenceWeek)99
            });

        await context.DbContext.SaveChangesAsync();

        var repository = new EventQueryRepository(context.DbContext);

        Assert.False(await repository.HasUpcomingPublicEventCandidatesAsync(new DateOnly(2026, 5, 18)));
    }

    [Fact]
    public async Task HasUpcomingPublicEventCandidatesAsync_ignores_recurring_rows_beyond_one_year_interval_cap()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();

        context.DbContext.Events.AddRange(
            new EventEntity
            {
                EventId = Guid.NewGuid(),
                Title = "Published Long-Gap Weekly",
                Summary = "Summary",
                Description = "Description",
                StartsOn = new DateOnly(2026, 5, 4),
                StartsAt = new TimeOnly(18, 0),
                SortOrder = 1,
                PublicationState = EventPublicationState.Published,
                RecurrencePattern = EventRecurrencePattern.Weekly,
                RecurrenceInterval = EventScheduleRules.MaxWeeklyRecurrenceInterval + 1,
                RecursOnDayOfWeek = DayOfWeek.Monday
            },
            new EventEntity
            {
                EventId = Guid.NewGuid(),
                Title = "Published Long-Gap Monthly",
                Summary = "Summary",
                Description = "Description",
                StartsOn = new DateOnly(2026, 5, 15),
                StartsAt = new TimeOnly(18, 0),
                SortOrder = 2,
                PublicationState = EventPublicationState.Published,
                RecurrencePattern = EventRecurrencePattern.MonthlyNthWeekday,
                RecurrenceInterval = EventScheduleRules.MaxMonthlyRecurrenceInterval + 1,
                RecursOnDayOfWeek = DayOfWeek.Friday,
                RecursOnWeekOfMonth = EventRecurrenceWeek.Third
            });

        await context.DbContext.SaveChangesAsync();

        var repository = new EventQueryRepository(context.DbContext);

        Assert.False(await repository.HasUpcomingPublicEventCandidatesAsync(new DateOnly(2026, 5, 18)));
    }
}
