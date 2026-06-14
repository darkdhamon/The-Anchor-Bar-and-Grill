using Anchor.Domain.Events;
using Anchor.Infrastructure.Data.Events;
using Anchor.Infrastructure.Tests.Support;

namespace Anchor.Infrastructure.Tests.Data;

public sealed class EventRepositoriesTests
{
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
                new DateOnly(2026, 7, 31)));
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
}
