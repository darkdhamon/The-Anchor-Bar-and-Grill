using Anchor.Web.Time;
using Anchor.Web.Tests.Support;

namespace Anchor.Web.Tests.Time;

public sealed class RestaurantTimeProviderTests
{
    [Fact]
    public void GetLocalNow_Uses_Chicago_Time_When_Configured_With_Iana_Id()
    {
        var utcClock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 7, 6, 3, 30, 0, TimeSpan.Zero));
        var provider = RestaurantTimeProvider.Create(utcClock, RestaurantTimeInfo.CanonicalTimeZoneId);

        var localNow = provider.GetLocalNow();

        Assert.Equal(new DateTimeOffset(2026, 7, 5, 22, 30, 0, TimeSpan.FromHours(-5)), localNow);
    }

    [Fact]
    public void Create_Throws_When_TimeZone_Cannot_Be_Resolved()
    {
        var utcClock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 7, 6, 3, 30, 0, TimeSpan.Zero));

        var error = Assert.Throws<InvalidOperationException>(() => RestaurantTimeProvider.Create(utcClock, "Mars/OlympusMons"));

        Assert.Contains("Mars/OlympusMons", error.Message, StringComparison.Ordinal);
    }

    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private readonly DateTimeOffset utcNow = utcNow.ToUniversalTime();

        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
