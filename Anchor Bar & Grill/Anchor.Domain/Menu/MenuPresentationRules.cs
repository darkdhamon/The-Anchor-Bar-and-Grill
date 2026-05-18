using System.Globalization;

namespace Anchor.Domain.Menu;

internal static class MenuPresentationRules
{
    private static readonly CultureInfo UsCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly DayOfWeek[] OrderedDays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    ];

    public static IReadOnlyList<DayOfWeek> DayOrder => OrderedDays;

    public static string GetTabLabel(MenuTab tab) =>
        tab switch
        {
            MenuTab.Breakfast => "Breakfast",
            MenuTab.Lunch => "Lunch",
            MenuTab.Dinner => "Dinner",
            MenuTab.Drinks => "Drinks",
            _ => tab.ToString()
        };

    public static string GetTabQueryValue(MenuTab tab) => GetTabLabel(tab).ToLowerInvariant();

    public static bool TryParseTab(string? value, out MenuTab tab)
    {
        tab = MenuTab.Lunch;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "breakfast" => Assign(MenuTab.Breakfast, out tab),
            "lunch" => Assign(MenuTab.Lunch, out tab),
            "dinner" => Assign(MenuTab.Dinner, out tab),
            "drinks" => Assign(MenuTab.Drinks, out tab),
            _ => false
        };
    }

    public static string GetDayLabel(DayOfWeek dayOfWeek) =>
        dayOfWeek switch
        {
            DayOfWeek.Monday => "Monday",
            DayOfWeek.Tuesday => "Tuesday",
            DayOfWeek.Wednesday => "Wednesday",
            DayOfWeek.Thursday => "Thursday",
            DayOfWeek.Friday => "Friday",
            DayOfWeek.Saturday => "Saturday",
            DayOfWeek.Sunday => "Sunday",
            _ => dayOfWeek.ToString()
        };

    public static string? FormatOfferDateSummary(MenuItemRecord item, DateOnly today)
    {
        if (item.OfferStartsOn is null)
        {
            return item.OfferEndsOn is null
                ? null
                : item.OfferEndsOn.Value < today
                    ? $"Expired on {item.OfferEndsOn.Value:MMM d}"
                    : $"Available through {item.OfferEndsOn.Value:MMM d}";
        }

        if (item.OfferEndsOn is null)
        {
            return item.OfferStartsOn > today
                ? $"Expected on {item.OfferStartsOn.Value:MMM d}"
                : $"Available since {item.OfferStartsOn.Value:MMM d}";
        }

        return item.OfferStartsOn > today
            ? $"Offered {item.OfferStartsOn.Value:MMM d} - {item.OfferEndsOn.Value:MMM d}"
            : item.OfferEndsOn.Value < today
                ? $"Expired on {item.OfferEndsOn.Value:MMM d}"
                : $"Available through {item.OfferEndsOn.Value:MMM d}";
    }

    public static IReadOnlyList<string> GetPublicStatusLabels(MenuItemRecord item, DateOnly today)
    {
        List<string> labels = [];

        if (item.OfferStartsOn is { } startsOn && startsOn > today && startsOn <= today.AddDays(30))
        {
            labels.Add("Coming Soon");
        }

        if (IsActiveDatedOffer(item, today))
        {
            labels.Add(item.IsSeasonal ? "Seasonal" : "Limited Time");
        }

        return labels;
    }

    public static IReadOnlyList<string> GetAdminStatusLabels(MenuItemRecord item, DateOnly today)
    {
        List<string> labels = [];

        if (!item.IsVisibleToGuests || item.OfferEndsOn is { } endsOn && endsOn < today || item.IsArchived)
        {
            labels.Add("Hidden");
        }

        if (item.IsArchived)
        {
            labels.Add("Archived");
        }

        if (item.OfferEndsOn is { } expiredOn && expiredOn < today)
        {
            labels.Add("Expired");
        }

        foreach (var label in GetPublicStatusLabels(item, today))
        {
            labels.Add(label);
        }

        return labels.Distinct(StringComparer.Ordinal).ToArray();
    }

    public static IReadOnlyList<string> GetAdminStatusLabels(MenuSectionRecord section)
    {
        List<string> labels = [];

        if (!section.IsVisibleToGuests || section.IsArchived)
        {
            labels.Add("Hidden");
        }

        if (section.IsArchived)
        {
            labels.Add("Archived");
        }

        return labels;
    }

    public static IReadOnlyList<string> GetAdminStatusLabels(MenuRecurringSpecialRecord special, DateOnly today)
    {
        List<string> labels = [];

        if (!special.IsVisibleToGuests || special.IsArchived)
        {
            labels.Add("Hidden");
        }

        if (special.IsArchived)
        {
            labels.Add("Archived");
        }

        if (special.DayOfWeek == today.DayOfWeek)
        {
            labels.Add("Today");
        }

        return labels;
    }

    public static string FormatServiceWindow(MenuServiceWindowRecord window)
    {
        if (!window.IsAvailable || window.OpensAt is null || window.ClosesAt is null)
        {
            return "Not served";
        }

        var suffix = window.ClosesNextDay ? " next day" : string.Empty;
        return $"{window.OpensAt.Value.ToString("h:mm tt", UsCulture)} - {window.ClosesAt.Value.ToString("h:mm tt", UsCulture)}{suffix}";
    }

    public static string GetPlacementSummary(MenuRecurringSpecialRecord special) =>
        string.IsNullOrWhiteSpace(special.LinkedMenuItemName)
            ? special.SectionName
            : $"{special.SectionName} - Menu item: {special.LinkedMenuItemName}";

    private static bool Assign(MenuTab value, out MenuTab target)
    {
        target = value;
        return true;
    }

    private static bool IsActiveDatedOffer(MenuItemRecord item, DateOnly today) =>
        item.OfferEndsOn is not null
        && (item.OfferStartsOn is null || item.OfferStartsOn <= today)
        && item.OfferEndsOn >= today;
}
