using System.Globalization;

namespace Anchor.Web.Components.Shared;

internal static class FlexibleTimeText
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public static string FormatDisplay(TimeOnly value) => value.ToString("h:mm tt", InvariantCulture);

    public static string FormatCanonical(TimeOnly value) => value.ToString("HH:mm", InvariantCulture);

    public static string NormalizeDisplay(string? rawValue) =>
        TryParse(rawValue, out var parsedTime)
            ? FormatDisplay(parsedTime)
            : rawValue ?? string.Empty;

    public static bool TryParse(string? rawValue, out TimeOnly time)
    {
        time = default;

        var raw = (rawValue ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Replace(".", string.Empty, StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var meridiem = GetMeridiem(raw, out var valueWithoutMeridiem);
        var cleaned = valueWithoutMeridiem.Replace(" ", string.Empty, StringComparison.Ordinal);

        int hours;
        int minutes;

        if (cleaned.Contains(':'))
        {
            var parts = cleaned.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length != 2
                || string.IsNullOrWhiteSpace(parts[0])
                || string.IsNullOrWhiteSpace(parts[1])
                || !int.TryParse(parts[0], NumberStyles.None, InvariantCulture, out hours)
                || !int.TryParse(parts[1], NumberStyles.None, InvariantCulture, out minutes))
            {
                return false;
            }
        }
        else if (cleaned.All(char.IsDigit))
        {
            if (cleaned.Length <= 2)
            {
                hours = int.Parse(cleaned, NumberStyles.None, InvariantCulture);
                minutes = 0;
            }
            else if (cleaned.Length == 3)
            {
                hours = int.Parse(cleaned[..1], NumberStyles.None, InvariantCulture);
                minutes = int.Parse(cleaned[1..], NumberStyles.None, InvariantCulture);
            }
            else if (cleaned.Length == 4)
            {
                hours = int.Parse(cleaned[..2], NumberStyles.None, InvariantCulture);
                minutes = int.Parse(cleaned[2..], NumberStyles.None, InvariantCulture);
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        if (minutes is < 0 or > 59)
        {
            return false;
        }

        if (meridiem is not null)
        {
            if (hours is < 1 or > 12)
            {
                return false;
            }

            if (meridiem == "pm" && hours < 12)
            {
                hours += 12;
            }

            if (meridiem == "am" && hours == 12)
            {
                hours = 0;
            }
        }
        else if (hours is < 0 or > 23)
        {
            return false;
        }

        time = new TimeOnly(hours, minutes);
        return true;
    }

    private static string? GetMeridiem(string rawValue, out string valueWithoutMeridiem)
    {
        if (rawValue.EndsWith("am", StringComparison.Ordinal))
        {
            valueWithoutMeridiem = rawValue[..^2];
            return "am";
        }

        if (rawValue.EndsWith("pm", StringComparison.Ordinal))
        {
            valueWithoutMeridiem = rawValue[..^2];
            return "pm";
        }

        if (rawValue.EndsWith('a'))
        {
            valueWithoutMeridiem = rawValue[..^1];
            return "am";
        }

        if (rawValue.EndsWith('p'))
        {
            valueWithoutMeridiem = rawValue[..^1];
            return "pm";
        }

        valueWithoutMeridiem = rawValue;
        return null;
    }
}
