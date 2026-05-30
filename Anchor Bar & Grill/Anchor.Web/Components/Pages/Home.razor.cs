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

    private static readonly IReadOnlyList<HomeActionLink> homeActions =
    [
        new("/menu", "Browse the Menu", true),
        new("/events", "See Upcoming Events", false),
        new("/contact", "Plan Your Visit", false)
    ];

    private IReadOnlyList<HomeSpecialCardView> homeSpecialItems = [];
    private IReadOnlyList<HomeEventCardView> homeUpcomingEvents = [];
    private HomepagePublicityContent homepageContent = HomepagePublicityDefaults.Content;

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
