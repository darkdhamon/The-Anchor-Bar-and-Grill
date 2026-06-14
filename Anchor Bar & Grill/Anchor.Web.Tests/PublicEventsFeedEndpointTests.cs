using System.Net.Http.Json;
using Anchor.Domain.Events;
using Anchor.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Anchor.Web.Tests;

public sealed class PublicEventsFeedEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=AnchorPublicEventsFeedTests;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
    private static readonly object syncLock = new();
    private static bool databaseReady;
    private readonly WebApplicationFactory<Program> factory;

    public PublicEventsFeedEndpointTests(WebApplicationFactory<Program> factory)
    {
        EnsureDatabaseReady();
        this.factory = factory;
    }

    [Fact]
    public async Task EventsFeed_ReturnsMappedWindowPayload_AndRequestsSkippedWindows()
    {
        var fakeService = new FakeEventQueryService
        {
            WindowResult = new UpcomingEventWindowResult(
            [
                new EventOccurrenceRecord(
                    Guid.Parse("0BE8C2B1-FE05-4518-8699-2870A9E85010"),
                    "Dockside Acoustic Night",
                    "An unplugged evening set that keeps the room lively without overpowering dinner service.",
                    "A stripped-back live set for guests who want music and conversation at the same time.",
                    "Live Music",
                    "/images/home-carousel/live-music-stage.jpg",
                    new DateOnly(2026, 9, 11),
                    new TimeOnly(19, 30),
                    null,
                    false,
                    10,
                    false,
                    "One-time event on Sep 11, 2026 at 7:30 PM")
            ],
                new DateOnly(2026, 12, 11),
                false)
        };

        using var client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEventQueryService>();
                services.AddSingleton<IEventQueryService>(fakeService);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetFromJsonAsync<PublicEventsFeedResponseDto>("/events/feed?from=2026-08-20");

        Assert.NotNull(response);
        var eventCard = Assert.Single(response!.Events);
        Assert.Equal("Dockside Acoustic Night", eventCard.Title);
        Assert.Equal("Live Music", eventCard.PromoBadge);
        Assert.Equal("/images/home-carousel/live-music-stage.jpg", eventCard.ImagePath);
        Assert.Equal("Fri, Sep 11", eventCard.DateLabel);
        Assert.Equal("Fri, Sep 11, 2026 at 7:30 PM", eventCard.DateTimeLabel);
        Assert.Equal("One Time", eventCard.ScheduleTypeLabel);
        Assert.Equal("2026-12-11", response.NextFromDate);
        Assert.False(response.HasMore);
        Assert.Equal(new DateOnly(2026, 8, 20), fakeService.LastRequestedFromDate);
        Assert.True(fakeService.LastRequestedSkipEmptyWindows);
    }

    private static void EnsureDatabaseReady()
    {
        lock (syncLock)
        {
            if (databaseReady)
            {
                return;
            }

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(TestConnectionString)
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options;

            using var context = new ApplicationDbContext(options);
            context.Database.EnsureDeleted();
            context.Database.Migrate();
            databaseReady = true;
        }
    }

    private sealed class FakeEventQueryService : IEventQueryService
    {
        public DateOnly? LastRequestedFromDate { get; private set; }

        public bool LastRequestedSkipEmptyWindows { get; private set; }

        public UpcomingEventWindowResult WindowResult { get; set; } =
            new([], new DateOnly(2026, 12, 11), false);

        public Task<IReadOnlyList<EventOccurrenceRecord>> GetUpcomingEventsAsync(
            DateTime localNow,
            int daysAhead = 30,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EventOccurrenceRecord>>([]);

        public Task<UpcomingEventWindowResult> GetUpcomingEventsWindowAsync(
            DateTime localNow,
            DateOnly fromDate,
            int daysAhead = 30,
            bool skipEmptyWindows = false,
            CancellationToken cancellationToken = default)
        {
            LastRequestedFromDate = fromDate;
            LastRequestedSkipEmptyWindows = skipEmptyWindows;
            return Task.FromResult(WindowResult);
        }
    }

    private sealed record PublicEventsFeedResponseDto(
        IReadOnlyList<PublicEventCardViewDto> Events,
        string NextFromDate,
        bool HasMore);

    private sealed record PublicEventCardViewDto(
        string Title,
        string PromoBadge,
        string? ImagePath,
        string ImageAltText,
        string DateLabel,
        string DateTimeLabel,
        string ScheduleSummary,
        string Summary,
        string? Description,
        bool IsRecurring,
        string ScheduleTypeLabel);
}
