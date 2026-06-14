using System.Globalization;
using Anchor.Domain.Events;
using Anchor.Web.Components.Shared;

namespace Anchor.Web.Components.Pages;

internal sealed record PublicEventCardView(
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

internal sealed record PublicEventsFeedResponse(
    IReadOnlyList<PublicEventCardView> Events,
    string NextFromDate,
    bool HasMore);

internal static class PublicEventCardMapper
{
    public static PublicEventCardView Map(EventOccurrenceRecord item)
    {
        var summary = string.IsNullOrWhiteSpace(item.Summary) ? item.Description : item.Summary;
        var description = string.IsNullOrWhiteSpace(item.Description)
            || string.Equals(item.Description, summary, StringComparison.OrdinalIgnoreCase)
            ? null
            : item.Description;

        return new PublicEventCardView(
            item.Title,
            string.IsNullOrWhiteSpace(item.PromoBadge) ? "Upcoming Event" : item.PromoBadge,
            MenuImagePathDisplay.Normalize(item.ImagePath),
            $"{item.Title} event image",
            item.OccursOn.ToString("ddd, MMM d", CultureInfo.InvariantCulture),
            GetDateTimeLabel(item),
            item.ScheduleSummary,
            summary,
            description,
            item.IsRecurring,
            item.IsRecurring ? "Recurring" : "One Time");
    }

    private static string GetDateTimeLabel(EventOccurrenceRecord item)
    {
        var dateLabel = item.OccursOn.ToString("ddd, MMM d, yyyy", CultureInfo.InvariantCulture);
        var timeLabel = item.EndsAt is { } endsAt
            ? $"{FormatTime(item.StartsAt)} - {FormatTime(endsAt)}{(item.EndsNextDay ? " next day" : string.Empty)}"
            : FormatTime(item.StartsAt);

        return $"{dateLabel} at {timeLabel}";
    }

    private static string FormatTime(TimeOnly time) =>
        time.ToString("h:mm tt", CultureInfo.InvariantCulture);
}
