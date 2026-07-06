using Anchor.Domain.Menu;

namespace Anchor.Web.Components.Pages;

public sealed record MenuHoursDisplayRow(string Label, string Summary, bool IncludesToday);

public sealed record MenuHoursCardView(IReadOnlyList<MenuHoursDisplayRow> Rows);

public static class MenuHoursPresentation
{
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

    public static MenuHoursCardView Create(IReadOnlyList<MenuServiceWindowView> serviceHours)
    {
        if (serviceHours.Count == 0)
        {
            return new MenuHoursCardView(Array.Empty<MenuHoursDisplayRow>());
        }

        var orderedHours = serviceHours
            .OrderBy(window => GetDayIndex(window.DayOfWeek))
            .ToArray();

        var rows = BuildRows(orderedHours);

        return new MenuHoursCardView(rows);
    }

    private static IReadOnlyList<MenuHoursDisplayRow> BuildRows(IReadOnlyList<MenuServiceWindowView> orderedHours)
    {
        List<List<MenuServiceWindowView>> groupedWindows = [];

        foreach (var window in orderedHours)
        {
            if (groupedWindows.Count > 0 && string.Equals(groupedWindows[^1][0].Summary, window.Summary, StringComparison.Ordinal))
            {
                groupedWindows[^1].Add(window);
                continue;
            }

            groupedWindows.Add([window]);
        }

        if (groupedWindows.Count > 1
            && string.Equals(groupedWindows[0][0].Summary, groupedWindows[^1][0].Summary, StringComparison.Ordinal))
        {
            var mergedWindowGroup = new List<MenuServiceWindowView>(groupedWindows[^1].Count + groupedWindows[0].Count);
            mergedWindowGroup.AddRange(groupedWindows[^1]);
            mergedWindowGroup.AddRange(groupedWindows[0]);

            groupedWindows[0] = mergedWindowGroup;
            groupedWindows.RemoveAt(groupedWindows.Count - 1);
        }

        return groupedWindows
            .Select(group => new MenuHoursDisplayRow(
                GetLabel(group),
                group[0].Summary,
                group.Any(window => window.IsToday)))
            .ToArray();
    }

    private static string GetLabel(IReadOnlyList<MenuServiceWindowView> group)
    {
        if (group.Count == OrderedDays.Length)
        {
            return "Daily";
        }

        var firstLabel = GetDayLabel(group[0].DayOfWeek);
        if (group.Count == 1)
        {
            return firstLabel;
        }

        var lastLabel = GetDayLabel(group[^1].DayOfWeek);
        return group.Count == 2
            ? $"{firstLabel} & {lastLabel}"
            : $"{firstLabel}-{lastLabel}";
    }

    private static int GetDayIndex(DayOfWeek dayOfWeek) => Array.IndexOf(OrderedDays, dayOfWeek);

    private static string GetDayLabel(DayOfWeek dayOfWeek) =>
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
}
