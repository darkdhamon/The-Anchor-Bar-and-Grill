using Anchor.Domain.Events;
using Microsoft.AspNetCore.Components;

namespace Anchor.Web.Components.Pages;

public partial class Events
{
    private const int PublicEventsWindowDays = 90;
    private const string PublicEventsFeedPath = "/events/feed";

    private IReadOnlyList<PublicEventCardView> upcomingEvents = [];
    private bool hasMoreWindows;
    private DateOnly nextWindowFromDate;

    [Inject]
    public IEventQueryService EventQueryService { get; set; } = default!;

    [Inject]
    public TimeProvider TimeProvider { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var localNow = TimeProvider.GetLocalNow().DateTime;
        var initialWindow = await EventQueryService.GetUpcomingEventsWindowAsync(
            localNow,
            DateOnly.FromDateTime(localNow),
            PublicEventsWindowDays);

        upcomingEvents = initialWindow.Events
            .Select(PublicEventCardMapper.Map)
            .ToArray();
        hasMoreWindows = initialWindow.HasMore;
        nextWindowFromDate = initialWindow.NextFromDate;
    }

    private string NextWindowFromDateValue => nextWindowFromDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private bool ShowUpcomingWindowEmptyState => upcomingEvents.Count == 0 && hasMoreWindows;

    private bool ShowPublishedCalendarEmptyState => upcomingEvents.Count == 0 && !hasMoreWindows;

    private string PublicEventsFeedUrl => PublicEventsFeedPath;
}
