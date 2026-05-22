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
        if (item.Special is not null)
        {
            return null;
        }

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
        if (item.Special is { } special)
        {
            List<string> specialLabels = [];
            if (special.ScheduleKind == MenuItemSpecialScheduleKind.Dated
                && special.StartDate > today
                && special.StartDate <= today.AddDays(30))
            {
                specialLabels.Add("Coming Soon");
            }

            return specialLabels;
        }

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

        if (!item.IsVisibleToGuests || item.IsArchived || item.Special is null && item.OfferEndsOn is { } endsOn && endsOn < today)
        {
            labels.Add("Hidden");
        }

        if (item.IsArchived)
        {
            labels.Add("Archived");
        }

        if (item.Special is null && item.OfferEndsOn is { } expiredOn && expiredOn < today)
        {
            labels.Add("Expired");
        }

        if (item.Special is not null)
        {
            labels.Add("Special");
            foreach (var label in GetAdminStatusLabels(item.Special, today))
            {
                labels.Add(label);
            }
        }
        else
        {
            foreach (var label in GetPublicStatusLabels(item, today))
            {
                labels.Add(label);
            }
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

    public static IReadOnlyList<string> GetAdminStatusLabels(MenuItemSpecialRecord special, DateOnly today)
    {
        List<string> labels = [];

        if (IsSpecialToday(special, today))
        {
            labels.Add("Today");
        }

        if (special.ScheduleKind == MenuItemSpecialScheduleKind.Dated
            && special.StartDate > today
            && special.StartDate <= today.AddDays(30))
        {
            labels.Add("Coming Soon");
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

    public static string GetSpecialBadgeLabel(MenuItemSpecialRecord special) =>
        special.ScheduleKind switch
        {
            MenuItemSpecialScheduleKind.WeeklyRecurring => GetDayLabel(special.DayOfWeek ?? DayOfWeek.Monday),
            MenuItemSpecialScheduleKind.Dated when special.EndDate is { } endDate && endDate > special.StartDate
                => $"{special.StartDate:MMM d} - {endDate:MMM d}",
            _ => $"{special.StartDate:MMM d}"
        };

    public static string FormatSpecialScheduleSummary(MenuItemSpecialRecord special)
    {
        if (special.ScheduleKind == MenuItemSpecialScheduleKind.WeeklyRecurring)
        {
            var baseSummary = $"Every {GetDayLabel(special.DayOfWeek ?? DayOfWeek.Monday)}";
            if (special.EndDate is { } endDate)
            {
                return $"{baseSummary} through {endDate:MMM d}";
            }

            return special.StartDate > DateOnly.MinValue
                ? $"{baseSummary} starting {special.StartDate:MMM d}"
                : baseSummary;
        }

        if (special.EndDate is { } datedEnd && datedEnd > special.StartDate)
        {
            return $"{special.StartDate:MMM d} - {datedEnd:MMM d}";
        }

        return $"{special.StartDate:MMM d} only";
    }

    public static string? FormatSpecialTimeSummary(MenuItemSpecialRecord special)
    {
        if (special.StartsAt is null && special.EndsAt is null)
        {
            return null;
        }

        if (special.StartsAt is { } startsAt && special.EndsAt is { } endsAt)
        {
            var suffix = special.ClosesNextDay ? " next day" : string.Empty;
            return $"{startsAt.ToString("h:mm tt", UsCulture)} - {endsAt.ToString("h:mm tt", UsCulture)}{suffix}";
        }

        if (special.StartsAt is { } startOnly)
        {
            return $"After {startOnly.ToString("h:mm tt", UsCulture)}";
        }

        var endOnly = special.EndsAt!.Value;
        var endSuffix = special.ClosesNextDay ? " next day" : string.Empty;
        return $"Until {endOnly.ToString("h:mm tt", UsCulture)}{endSuffix}";
    }

    public static bool IsSpecialToday(MenuItemSpecialRecord special, DateOnly today)
    {
        if (special.ScheduleKind == MenuItemSpecialScheduleKind.WeeklyRecurring)
        {
            return special.DayOfWeek == today.DayOfWeek
                && special.StartDate <= today
                && (special.EndDate is null || special.EndDate >= today);
        }

        var effectiveEnd = special.EndDate ?? special.StartDate;
        return special.StartDate <= today && effectiveEnd >= today;
    }

    public static string GetPlacementSummary(MenuItemRecord item) =>
        string.Join(", ",
            item.SectionAssignments
                .Select(assignment => assignment.SectionName)
                .Distinct(StringComparer.OrdinalIgnoreCase));

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
