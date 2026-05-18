using Anchor.Domain.Menu;
using Anchor.Web.Components.Pages;

namespace Anchor.Web.Tests.Components.Pages;

public sealed class MenuHoursPresentationTests
{
    [Fact]
    public void Create_WhenAllDaysMatch_UsesDailyRow()
    {
        var windows = CreateWindows(
            DayOfWeek.Monday,
            new Dictionary<DayOfWeek, string>
            {
                [DayOfWeek.Monday] = "11:00 AM - 5:00 PM",
                [DayOfWeek.Tuesday] = "11:00 AM - 5:00 PM",
                [DayOfWeek.Wednesday] = "11:00 AM - 5:00 PM",
                [DayOfWeek.Thursday] = "11:00 AM - 5:00 PM",
                [DayOfWeek.Friday] = "11:00 AM - 5:00 PM",
                [DayOfWeek.Saturday] = "11:00 AM - 5:00 PM",
                [DayOfWeek.Sunday] = "11:00 AM - 5:00 PM"
            });

        var result = MenuHoursPresentation.Create(windows);

        Assert.Equal("11:00 AM - 5:00 PM", result.TodaySummary);
        var row = Assert.Single(result.Rows);
        Assert.Equal("Daily", row.Label);
        Assert.Equal("11:00 AM - 5:00 PM", row.Summary);
    }

    [Fact]
    public void Create_WhenFirstAndLastGroupsMatch_MergesAcrossWeekBoundary()
    {
        var windows = CreateWindows(
            DayOfWeek.Monday,
            new Dictionary<DayOfWeek, string>
            {
                [DayOfWeek.Monday] = "5:00 PM - 11:00 PM",
                [DayOfWeek.Tuesday] = "5:00 PM - 11:00 PM",
                [DayOfWeek.Wednesday] = "5:00 PM - 11:00 PM",
                [DayOfWeek.Thursday] = "5:00 PM - 11:00 PM",
                [DayOfWeek.Friday] = "5:00 PM - 2:00 AM next day",
                [DayOfWeek.Saturday] = "5:00 PM - 2:00 AM next day",
                [DayOfWeek.Sunday] = "5:00 PM - 11:00 PM"
            });

        var result = MenuHoursPresentation.Create(windows);

        Assert.Equal("5:00 PM - 11:00 PM", result.TodaySummary);
        Assert.Collection(
            result.Rows,
            row =>
            {
                Assert.Equal("Sunday-Thursday", row.Label);
                Assert.Equal("5:00 PM - 11:00 PM", row.Summary);
            },
            row =>
            {
                Assert.Equal("Friday & Saturday", row.Label);
                Assert.Equal("5:00 PM - 2:00 AM next day", row.Summary);
            });
    }

    [Fact]
    public void Create_WhenWeekdaysAreUnavailable_GroupsNotServedRange()
    {
        var windows = CreateWindows(
            DayOfWeek.Saturday,
            new Dictionary<DayOfWeek, string>
            {
                [DayOfWeek.Monday] = "Not served",
                [DayOfWeek.Tuesday] = "Not served",
                [DayOfWeek.Wednesday] = "Not served",
                [DayOfWeek.Thursday] = "Not served",
                [DayOfWeek.Friday] = "Not served",
                [DayOfWeek.Saturday] = "10:00 AM - 1:00 PM",
                [DayOfWeek.Sunday] = "10:00 AM - 1:00 PM"
            });

        var result = MenuHoursPresentation.Create(windows);

        Assert.Equal("10:00 AM - 1:00 PM", result.TodaySummary);
        Assert.Collection(
            result.Rows,
            row =>
            {
                Assert.Equal("Monday-Friday", row.Label);
                Assert.Equal("Not served", row.Summary);
            },
            row =>
            {
                Assert.Equal("Saturday & Sunday", row.Label);
                Assert.Equal("10:00 AM - 1:00 PM", row.Summary);
            });
    }

    private static IReadOnlyList<MenuServiceWindowView> CreateWindows(
        DayOfWeek today,
        IReadOnlyDictionary<DayOfWeek, string> summaries)
    {
        var orderedDays = new[]
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday,
            DayOfWeek.Sunday
        };

        return orderedDays
            .Select(day => new MenuServiceWindowView(
                day,
                day.ToString(),
                !string.Equals(summaries[day], "Not served", StringComparison.Ordinal),
                summaries[day],
                day == today,
                null,
                null,
                summaries[day].Contains("next day", StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }
}
