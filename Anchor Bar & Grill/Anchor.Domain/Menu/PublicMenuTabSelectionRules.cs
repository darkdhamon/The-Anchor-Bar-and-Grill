namespace Anchor.Domain.Menu;

internal static class PublicMenuTabSelectionRules
{
    private static readonly MenuTab[] TabPriority =
    [
        MenuTab.Breakfast,
        MenuTab.Lunch,
        MenuTab.Dinner,
        MenuTab.Drinks
    ];

    public static MenuTab GetSuggestedTab(
        IReadOnlyList<MenuServiceWindowRecord> windows,
        DateOnly today,
        TimeOnly currentTime)
    {
        var windowsByTab = windows
            .GroupBy(window => window.Tab)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyDictionary<DayOfWeek, MenuServiceWindowRecord>)group.ToDictionary(window => window.DayOfWeek));

        foreach (var tab in TabPriority)
        {
            if (IsActive(windowsByTab.GetValueOrDefault(tab), today, currentTime))
            {
                return tab;
            }
        }

        var now = today.ToDateTime(currentTime);
        var nextOpening = TabPriority
            .Select((tab, index) => new
            {
                Tab = tab,
                Priority = index,
                OpensAt = GetNextOpening(windowsByTab.GetValueOrDefault(tab), today, now)
            })
            .Where(candidate => candidate.OpensAt is not null)
            .OrderBy(candidate => candidate.OpensAt)
            .ThenBy(candidate => candidate.Priority)
            .FirstOrDefault();

        return nextOpening?.Tab ?? MenuTab.Lunch;
    }

    private static bool IsActive(
        IReadOnlyDictionary<DayOfWeek, MenuServiceWindowRecord>? windowsByDay,
        DateOnly today,
        TimeOnly currentTime)
    {
        if (windowsByDay is null)
        {
            return false;
        }

        var now = today.ToDateTime(currentTime);

        return TryCreateInterval(windowsByDay, today, out var todayInterval) && todayInterval.Contains(now)
            || TryCreateInterval(windowsByDay, today.AddDays(-1), out var previousDayInterval)
                && previousDayInterval.Window.ClosesNextDay
                && previousDayInterval.Contains(now);
    }

    private static DateTime? GetNextOpening(
        IReadOnlyDictionary<DayOfWeek, MenuServiceWindowRecord>? windowsByDay,
        DateOnly today,
        DateTime now)
    {
        if (windowsByDay is null)
        {
            return null;
        }

        for (var dayOffset = 0; dayOffset <= 7; dayOffset++)
        {
            var date = today.AddDays(dayOffset);
            if (!TryCreateInterval(windowsByDay, date, out var interval))
            {
                continue;
            }

            if (interval.Start > now)
            {
                return interval.Start;
            }
        }

        return null;
    }

    private static bool TryCreateInterval(
        IReadOnlyDictionary<DayOfWeek, MenuServiceWindowRecord> windowsByDay,
        DateOnly date,
        out ServiceWindowInterval interval)
    {
        if (!windowsByDay.TryGetValue(date.DayOfWeek, out var window)
            || !window.IsAvailable
            || window.OpensAt is null
            || window.ClosesAt is null)
        {
            interval = default;
            return false;
        }

        var start = date.ToDateTime(window.OpensAt.Value);
        var end = (window.ClosesNextDay ? date.AddDays(1) : date).ToDateTime(window.ClosesAt.Value);
        if (end <= start)
        {
            interval = default;
            return false;
        }

        interval = new ServiceWindowInterval(window, start, end);
        return true;
    }

    private readonly record struct ServiceWindowInterval(MenuServiceWindowRecord Window, DateTime Start, DateTime End)
    {
        public bool Contains(DateTime value) => value >= Start && value < End;
    }
}
