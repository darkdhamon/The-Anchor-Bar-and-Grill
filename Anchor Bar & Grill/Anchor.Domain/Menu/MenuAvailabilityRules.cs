namespace Anchor.Domain.Menu;

public static class MenuAvailabilityRules
{
    public static bool HasRecurringSeason(
        int? seasonStartMonth,
        int? seasonStartDay,
        int? seasonEndMonth,
        int? seasonEndDay) =>
        seasonStartMonth is not null
        && seasonEndMonth is not null
        && seasonStartDay is >= 1 or null
        && seasonEndDay is >= 1 or null;

    public static bool HasRecurringSeason(MenuItemRecord item) =>
        HasRecurringSeason(item.SeasonStartMonth, item.SeasonStartDay, item.SeasonEndMonth, item.SeasonEndDay);

    public static bool IsItemWithinRecurringSeason(MenuItemRecord item, DateOnly today)
    {
        if (!HasRecurringSeason(item))
        {
            return true;
        }

        return IsDateWithinRecurringSeason(
            today,
            item.SeasonStartMonth!.Value,
            item.SeasonStartDay,
            item.SeasonEndMonth!.Value,
            item.SeasonEndDay);
    }

    public static bool IsDateWithinRecurringSeason(
        DateOnly today,
        int seasonStartMonth,
        int? seasonStartDay,
        int seasonEndMonth,
        int? seasonEndDay)
    {
        var year = today.Year;
        var startThisYear = BuildSeasonBoundary(year, seasonStartMonth, seasonStartDay, true);
        var endThisYear = BuildSeasonBoundary(year, seasonEndMonth, seasonEndDay, false);

        if (startThisYear <= endThisYear)
        {
            return today >= startThisYear && today <= endThisYear;
        }

        var wrappedEnd = BuildSeasonBoundary(year + 1, seasonEndMonth, seasonEndDay, false);
        if (today >= startThisYear)
        {
            return today <= wrappedEnd;
        }

        var previousWrappedStart = BuildSeasonBoundary(year - 1, seasonStartMonth, seasonStartDay, true);
        return today >= previousWrappedStart && today <= endThisYear;
    }

    public static int GetEffectiveDay(int year, int month, int? requestedDay, bool isStartBoundary)
    {
        var maxDay = DateTime.DaysInMonth(year, month);
        if (requestedDay is null)
        {
            return isStartBoundary ? 1 : maxDay;
        }

        return Math.Clamp(requestedDay.Value, 1, maxDay);
    }

    public static DateOnly BuildSeasonBoundary(int year, int month, int? day, bool isStartBoundary) =>
        new(year, month, GetEffectiveDay(year, month, day, isStartBoundary));
}
