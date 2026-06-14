using System.Globalization;
using Anchor.Domain.Events;
using Anchor.Web.Components.Pages;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Routing;

internal static class PublicEventsEndpointRouteBuilderExtensions
{
    private const int PublicEventsWindowDays = 90;

    public static IEndpointConventionBuilder MapPublicEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapGet("/events/feed", async (
            [FromQuery] DateOnly? from,
            [FromServices] IEventQueryService eventQueryService,
            [FromServices] TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var localNow = timeProvider.GetLocalNow().DateTime;
            var fromDate = from ?? DateOnly.FromDateTime(localNow).AddDays(PublicEventsWindowDays + 1);
            var window = await eventQueryService.GetUpcomingEventsWindowAsync(
                localNow,
                fromDate,
                PublicEventsWindowDays,
                skipEmptyWindows: true,
                cancellationToken);

            var response = new PublicEventsFeedResponse(
                window.Events.Select(PublicEventCardMapper.Map).ToArray(),
                window.NextFromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                window.HasMore);

            return TypedResults.Json(response);
        });
    }
}
