using Anchor.Domain.Menu;

namespace Anchor.Domain.Tests.Menu;

public sealed class MenuAvailabilityRulesTests
{
    [Theory]
    [InlineData(12, 20, 1, 5, 2026, 12, 24, true)]
    [InlineData(12, 20, 1, 5, 2027, 1, 3, true)]
    [InlineData(12, 20, 1, 5, 2026, 12, 1, false)]
    [InlineData(12, 20, 1, 5, 2027, 1, 20, false)]
    public void IsDateWithinRecurringSeason_supports_cross_year_windows(
        int startMonth,
        int startDay,
        int endMonth,
        int endDay,
        int year,
        int month,
        int day,
        bool expected)
    {
        var result = MenuAvailabilityRules.IsDateWithinRecurringSeason(
            new DateOnly(year, month, day),
            startMonth,
            startDay,
            endMonth,
            endDay);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildSeasonBoundary_clamps_february_twenty_ninth_in_non_leap_years()
    {
        var boundary = MenuAvailabilityRules.BuildSeasonBoundary(2027, 2, 29, isStartBoundary: false);

        Assert.Equal(new DateOnly(2027, 2, 28), boundary);
    }

    [Fact]
    public void BuildSeasonBoundary_preserves_february_twenty_ninth_in_leap_years()
    {
        var boundary = MenuAvailabilityRules.BuildSeasonBoundary(2028, 2, 29, isStartBoundary: false);

        Assert.Equal(new DateOnly(2028, 2, 29), boundary);
    }
}
