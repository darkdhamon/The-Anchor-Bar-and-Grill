using System.Globalization;

namespace Anchor.Domain.Events;

public static class EventScheduleRules
{
    public static IReadOnlyList<string> Validate(SaveEventRequest request)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add("Event title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Summary))
        {
            errors.Add("Event summary is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors.Add("Event description is required.");
        }

        if (request.EndsAt.HasValue && !request.EndsNextDay && request.EndsAt.Value <= request.StartsAt)
        {
            errors.Add("End time must be later than the start time unless the event ends the next day.");
        }

        if (request.RecurrenceInterval < 1)
        {
            errors.Add("Recurring events must use an interval of at least 1.");
        }

        if (request.RecursUntil is { } recursUntil && recursUntil < request.StartsOn)
        {
            errors.Add("Recurs until cannot be earlier than the first event date.");
        }

        switch (request.RecurrencePattern)
        {
            case EventRecurrencePattern.None:
                if (request.RecursOnDayOfWeek is not null || request.RecursOnWeekOfMonth is not null || request.RecursUntil is not null)
                {
                    errors.Add("One-time events cannot include recurring schedule fields.");
                }

                break;

            case EventRecurrencePattern.Weekly:
                if (request.RecursOnDayOfWeek is null)
                {
                    errors.Add("Weekly recurring events must include a day of week.");
                }

                if (request.RecursOnWeekOfMonth is not null)
                {
                    errors.Add("Weekly recurring events cannot include a week-of-month value.");
                }

                break;

            case EventRecurrencePattern.MonthlyNthWeekday:
                if (request.RecursOnDayOfWeek is null)
                {
                    errors.Add("Monthly recurring events must include a day of week.");
                }

                if (request.RecursOnWeekOfMonth is null)
                {
                    errors.Add("Monthly recurring events must include a week-of-month value.");
                }

                break;
        }

        return errors;
    }

    public static IReadOnlyList<DateOnly> GetOccurrences(EventRecord record, DateOnly fromDate, DateOnly throughDate)
    {
        if (throughDate < fromDate)
        {
            return [];
        }

        if (record.RecurrencePattern == EventRecurrencePattern.None)
        {
            return record.StartsOn >= fromDate && record.StartsOn <= throughDate
                ? [record.StartsOn]
                : [];
        }

        var finalDate = record.RecursUntil is { } recursUntil && recursUntil < throughDate
            ? recursUntil
            : throughDate;

        if (finalDate < fromDate)
        {
            return [];
        }

        return record.RecurrencePattern switch
        {
            EventRecurrencePattern.Weekly when record.RecursOnDayOfWeek is { } dayOfWeek
                => GetWeeklyOccurrences(record.StartsOn, fromDate, finalDate, dayOfWeek, record.RecurrenceInterval),
            EventRecurrencePattern.MonthlyNthWeekday when record.RecursOnDayOfWeek is { } monthlyDayOfWeek && record.RecursOnWeekOfMonth is { } weekOfMonth
                => GetMonthlyNthWeekdayOccurrences(record.StartsOn, fromDate, finalDate, monthlyDayOfWeek, weekOfMonth, record.RecurrenceInterval),
            _ => []
        };
    }

    public static DateOnly? GetNextOccurrence(EventRecord record, DateTime localNow, int daysAhead = 365)
    {
        var fromDate = DateOnly.FromDateTime(localNow);
        var throughDate = fromDate.AddDays(daysAhead);

        foreach (var occursOn in GetOccurrences(record, fromDate, throughDate))
        {
            if (occursOn.ToDateTime(record.StartsAt) >= localNow)
            {
                return occursOn;
            }
        }

        return null;
    }

    public static string GetScheduleSummary(EventRecord record, DateTime localNow)
    {
        if (!record.IsRecurring)
        {
            return $"One-time event on {record.StartsOn:MMM d, yyyy} at {record.StartsAt.ToString("h:mm tt", CultureInfo.InvariantCulture)}";
        }

        var timeLabel = record.StartsAt.ToString("h:mm tt", CultureInfo.InvariantCulture);
        var cadence = record.RecurrencePattern switch
        {
            EventRecurrencePattern.Weekly when record.RecursOnDayOfWeek is { } dayOfWeek && record.RecurrenceInterval == 1
                => $"Recurring every {GetDayLabel(dayOfWeek)} at {timeLabel}",
            EventRecurrencePattern.Weekly when record.RecursOnDayOfWeek is { } dayOfWeek && record.RecurrenceInterval == 2
                => $"Recurring every other {GetDayLabel(dayOfWeek)} at {timeLabel}",
            EventRecurrencePattern.Weekly when record.RecursOnDayOfWeek is { } dayOfWeek
                => $"Recurring every {record.RecurrenceInterval} weeks on {GetDayLabel(dayOfWeek)} at {timeLabel}",
            EventRecurrencePattern.MonthlyNthWeekday when record.RecursOnDayOfWeek is { } monthlyDayOfWeek && record.RecursOnWeekOfMonth is { } weekOfMonth && record.RecurrenceInterval == 1
                => $"Recurring on the {GetWeekLabel(weekOfMonth).ToLowerInvariant()} {GetDayLabel(monthlyDayOfWeek)} of each month at {timeLabel}",
            EventRecurrencePattern.MonthlyNthWeekday when record.RecursOnDayOfWeek is { } nthDayOfWeek && record.RecursOnWeekOfMonth is { } nthWeekOfMonth
                => $"Recurring every {record.RecurrenceInterval} months on the {GetWeekLabel(nthWeekOfMonth).ToLowerInvariant()} {GetDayLabel(nthDayOfWeek)} at {timeLabel}",
            _ => $"Recurring at {timeLabel}"
        };

        return GetNextOccurrence(record, localNow) is { } nextDate
            ? $"{cadence} - next on {nextDate:MMM d, yyyy}"
            : cadence;
    }

    private static IReadOnlyList<DateOnly> GetWeeklyOccurrences(
        DateOnly startDate,
        DateOnly fromDate,
        DateOnly throughDate,
        DayOfWeek dayOfWeek,
        int interval)
    {
        List<DateOnly> occurrences = [];
        var anchorDate = NextOccurrenceOnOrAfter(startDate, dayOfWeek);
        var occurrenceDate = anchorDate > fromDate ? anchorDate : NextOccurrenceOnOrAfter(fromDate, dayOfWeek);

        if (interval > 1)
        {
            var weeksFromAnchor = (occurrenceDate.DayNumber - anchorDate.DayNumber) / 7;
            var remainder = weeksFromAnchor % interval;

            if (remainder != 0)
            {
                occurrenceDate = occurrenceDate.AddDays((interval - remainder) * 7);
            }
        }

        while (occurrenceDate <= throughDate)
        {
            occurrences.Add(occurrenceDate);
            occurrenceDate = occurrenceDate.AddDays(interval * 7);
        }

        return occurrences;
    }

    private static IReadOnlyList<DateOnly> GetMonthlyNthWeekdayOccurrences(
        DateOnly startDate,
        DateOnly fromDate,
        DateOnly throughDate,
        DayOfWeek dayOfWeek,
        EventRecurrenceWeek weekOfMonth,
        int interval)
    {
        List<DateOnly> occurrences = [];
        var anchorMonth = new DateOnly(startDate.Year, startDate.Month, 1);
        var cursorDate = startDate > fromDate ? startDate : fromDate;
        var cursorMonth = new DateOnly(cursorDate.Year, cursorDate.Month, 1);
        var finalMonth = new DateOnly(throughDate.Year, throughDate.Month, 1);

        while (cursorMonth <= finalMonth)
        {
            var monthOffset = ((cursorMonth.Year - anchorMonth.Year) * 12) + cursorMonth.Month - anchorMonth.Month;

            if (monthOffset >= 0 && monthOffset % interval == 0)
            {
                var occurrenceDate = GetNthWeekdayOfMonth(cursorMonth.Year, cursorMonth.Month, dayOfWeek, weekOfMonth);
                if (occurrenceDate >= startDate && occurrenceDate >= fromDate && occurrenceDate <= throughDate)
                {
                    occurrences.Add(occurrenceDate);
                }
            }

            cursorMonth = cursorMonth.AddMonths(1);
        }

        return occurrences;
    }

    private static DateOnly NextOccurrenceOnOrAfter(DateOnly fromDate, DayOfWeek dayOfWeek)
    {
        var offset = ((int)dayOfWeek - (int)fromDate.DayOfWeek + 7) % 7;
        return fromDate.AddDays(offset);
    }

    private static DateOnly GetNthWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek, EventRecurrenceWeek weekOfMonth)
    {
        if (weekOfMonth == EventRecurrenceWeek.Last)
        {
            var lastDayOfMonth = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            var offsetFromEnd = ((int)lastDayOfMonth.DayOfWeek - (int)dayOfWeek + 7) % 7;
            return lastDayOfMonth.AddDays(-offsetFromEnd);
        }

        var firstDayOfMonth = new DateOnly(year, month, 1);
        var offsetFromStart = ((int)dayOfWeek - (int)firstDayOfMonth.DayOfWeek + 7) % 7;
        var candidate = firstDayOfMonth.AddDays(offsetFromStart + (7 * ((int)weekOfMonth - 1)));

        return candidate.Month == month ? candidate : firstDayOfMonth;
    }

    private static string GetDayLabel(DayOfWeek dayOfWeek) => dayOfWeek switch
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

    private static string GetWeekLabel(EventRecurrenceWeek weekOfMonth) => weekOfMonth switch
    {
        EventRecurrenceWeek.First => "First",
        EventRecurrenceWeek.Second => "Second",
        EventRecurrenceWeek.Third => "Third",
        EventRecurrenceWeek.Fourth => "Fourth",
        EventRecurrenceWeek.Last => "Last",
        _ => "First"
    };
}
