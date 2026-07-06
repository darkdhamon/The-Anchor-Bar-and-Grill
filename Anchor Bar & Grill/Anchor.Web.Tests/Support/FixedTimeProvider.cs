namespace Anchor.Web.Tests.Support;

internal sealed class FixedTimeProvider(DateTimeOffset localNow) : TimeProvider
{
    private readonly TimeZoneInfo localTimeZone = TimeZoneInfo.CreateCustomTimeZone(
        "Test/Local",
        localNow.Offset,
        "Test/Local",
        "Test/Local");

    private DateTimeOffset currentLocalNow = localNow;

    public override DateTimeOffset GetUtcNow() => currentLocalNow.ToUniversalTime();

    public override TimeZoneInfo LocalTimeZone => localTimeZone;

    public void SetLocalNow(DateTimeOffset localNow) => currentLocalNow = localNow;
}
