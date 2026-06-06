using System.Globalization;
using Anchor.Domain.Events;
using Anchor.Domain.Menu;
using Anchor.Domain.Publicity;
using Anchor.Web.Components.Site;
using Microsoft.AspNetCore.Components;

namespace Anchor.Web.Components.Pages;

public partial class Home
{
    private const int HomeEventsPreviewDays = 30;
    private const int MaxHomeSpecials = 5;
    private const int MaxHomeEvents = 4;
    private static readonly IReadOnlyList<HomepageCarouselSlide> PlaceholderCarouselSlides =
    [
        new(
            "/images/home-carousel/live-music-stage.jpg",
            "Live band performing on the patio stage while guests watch from nearby tables.",
            "Live Music",
            "Patio nights with a crowd",
            "Bands set up right off the deck so the whole patio feels like part of the show."),
        new(
            "/images/home-carousel/outdoor-bar.jpg",
            "Open-air bar seating area with bright stools and a long stone counter.",
            "Outdoor Bar",
            "A warm-weather stop for drinks",
            "The outside bar gives guests a quick place to gather before events, dinner, or a full patio night."),
        new(
            "/images/home-carousel/exterior-trail-view.jpg",
            "Exterior view of The Anchor building from the nearby walking path.",
            "Exterior",
            "The Anchor from the trail",
            "The building stays easy to spot from the path, with quick access to the patio and front entrance."),
        new(
            "/images/home-carousel/patio-and-parking.jpg",
            "Patio building, flags, and nearby parking spaces viewed from the side of the lot.",
            "Arrival",
            "Parking, patio, and walk-up view",
            "This angle shows how close the patio and parking are once guests pull in."),
        new(
            "/images/home-carousel/main-dining-room.jpg",
            "Main indoor dining room with tables, high-top seating, and televisions.",
            "Inside",
            "Main dining room ready for game day",
            "Inside seating keeps the room casual and open, with TVs and high-tops mixed into the regular dining area.")
    ];

    private static readonly IReadOnlyList<HomeActionLink> homeActions =
    [
        new("/menu", "Browse the Menu", true),
        new("/events", "See Upcoming Events", false),
        new("/contact", "Plan Your Visit", false)
    ];

    private IReadOnlyList<HomeSpecialCardView> homeSpecialItems = [];
    private IReadOnlyList<HomeEventCardView> homeUpcomingEvents = [];
    private HomepagePublicityContent homepageContent = HomepagePublicityDefaults.Content;
    private IReadOnlyList<string> homepageSummaryParagraphs = HomepagePublicityText.GetParagraphs(HomepagePublicityDefaults.Content.Summary);

    [Inject]
    public IEventQueryService EventQueryService { get; set; } = default!;

    [Inject]
    public IHomepagePublicityService HomepagePublicityService { get; set; } = default!;

    [Inject]
    public IMenuQueryService MenuQueryService { get; set; } = default!;

    [Inject]
    public TimeProvider TimeProvider { get; set; } = default!;

    private static string GetHomeSpecialStatusClass(HomeSpecialCardView special) =>
        special.AvailabilityLabel switch
        {
            "Today" => "status-pill status-pill--today",
            "Now available" => "status-pill status-pill--schedule",
            "Limited-time special" => "status-pill status-pill--limited",
            _ => "status-pill status-pill--schedule"
        };

    protected override async Task OnInitializedAsync()
    {
        var localNow = TimeProvider.GetLocalNow();
        var today = DateOnly.FromDateTime(localNow.DateTime);

        homeSpecialItems = await LoadHomeSpecialsAsync(today);
        homeUpcomingEvents = await LoadHomeEventsAsync(localNow.DateTime, today);
        homepageContent = await HomepagePublicityService.GetPublishedHomepageAsync() ?? HomepagePublicityDefaults.Content;
        homepageSummaryParagraphs = HomepagePublicityText.GetParagraphs(homepageContent.Summary);
    }

    private async Task<IReadOnlyList<HomeSpecialCardView>> LoadHomeSpecialsAsync(DateOnly today)
    {
        var liveSpecials = await MenuQueryService.GetHomeSpecialsAsync(today);
        if (liveSpecials.Count > 0)
        {
            return liveSpecials
                .Take(MaxHomeSpecials)
                .Select(MapSpecial)
                .ToArray();
        }

        return MockupContent.RecurringSpecials
            .Take(MaxHomeSpecials)
            .Select(item => MapFallbackSpecial(item, today))
            .ToArray();
    }

    private async Task<IReadOnlyList<HomeEventCardView>> LoadHomeEventsAsync(DateTime localNow, DateOnly today)
    {
        var liveEvents = await EventQueryService.GetUpcomingEventsAsync(localNow, HomeEventsPreviewDays);
        if (liveEvents.Count > 0)
        {
            return liveEvents
                .Take(MaxHomeEvents)
                .Select(MapEvent)
                .ToArray();
        }

        return MockupContent.GetUpcomingEvents(today, HomeEventsPreviewDays)
            .Take(MaxHomeEvents)
            .Select(MapFallbackEvent)
            .ToArray();
    }

    private static HomeSpecialCardView MapSpecial(PublicHomeSpecialView special) =>
        new(
            special.BadgeLabel,
            special.Title,
            special.Description,
            special.TimeSummary,
            special.Callout,
            special.AvailabilityLabel,
            special.IsAvailableNow);

    private static HomeSpecialCardView MapFallbackSpecial(RecurringSpecial special, DateOnly today) =>
        new(
            special.DayLabel,
            special.Title,
            special.Description,
            special.TimeLabel,
            special.PriceNote,
            special.IsToday(today) ? "Today" : null,
            special.IsToday(today));

    private static HomeEventCardView MapEvent(EventOccurrenceRecord item) =>
        new(
            string.IsNullOrWhiteSpace(item.PromoBadge) ? "Upcoming Event" : item.PromoBadge,
            item.Title,
            string.IsNullOrWhiteSpace(item.Summary) ? item.Description : item.Summary,
            $"{item.OccursOn.ToString("ddd, MMM d", CultureInfo.InvariantCulture)} at {item.TimeLabel}",
            item.ScheduleSummary,
            item.IsRecurring,
            item.IsRecurring ? "Recurring" : "One Time");

    private static HomeEventCardView MapFallbackEvent(UpcomingEventOccurrence item) =>
        new(
            item.PromoBadge,
            item.Title,
            item.Description,
            item.DateTimeLabel,
            item.ScheduleDetail,
            item.IsRecurring,
            item.ScheduleTypeLabel);

    private sealed record HomeActionLink(string Href, string Label, bool IsPrimary);

    private sealed record HomeSpecialCardView(
        string BadgeLabel,
        string Title,
        string Description,
        string? TimeSummary,
        string? Callout,
        string? AvailabilityLabel,
        bool IsAvailableNow);

    private sealed record HomeEventCardView(
        string PromoBadge,
        string Title,
        string Description,
        string DateTimeLabel,
        string ScheduleSummary,
        bool IsRecurring,
        string ScheduleTypeLabel);
}
