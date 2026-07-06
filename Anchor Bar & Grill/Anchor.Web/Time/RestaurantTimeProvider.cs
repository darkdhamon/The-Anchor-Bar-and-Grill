namespace Anchor.Web.Time;

public sealed class RestaurantTimeProvider(TimeProvider innerClock, TimeZoneInfo localTimeZone) : TimeProvider
{
    public override TimeZoneInfo LocalTimeZone => localTimeZone;

    public override DateTimeOffset GetUtcNow() => innerClock.GetUtcNow();

    public override long GetTimestamp() => innerClock.GetTimestamp();

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
        innerClock.CreateTimer(callback, state, dueTime, period);

    public static RestaurantTimeProvider Create(TimeProvider innerClock, string? configuredTimeZoneId) =>
        new(innerClock, ResolveTimeZone(configuredTimeZoneId));

    public static RestaurantTimeProvider CreateSystemClock(string? configuredTimeZoneId) =>
        Create(TimeProvider.System, configuredTimeZoneId);

    private static TimeZoneInfo ResolveTimeZone(string? configuredTimeZoneId)
    {
        var requestedId = string.IsNullOrWhiteSpace(configuredTimeZoneId)
            ? RestaurantTimeInfo.CanonicalTimeZoneId
            : configuredTimeZoneId.Trim();

        foreach (var timeZoneId in GetCandidateTimeZoneIds(requestedId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        throw new InvalidOperationException(
            $"The configured restaurant time zone '{requestedId}' could not be resolved on this host.");
    }

    private static IReadOnlyList<string> GetCandidateTimeZoneIds(string requestedId)
    {
        List<string> candidateIds = [requestedId];

        if (string.Equals(requestedId, RestaurantTimeInfo.CanonicalTimeZoneId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(requestedId, RestaurantTimeInfo.WindowsTimeZoneId, StringComparison.OrdinalIgnoreCase))
        {
            candidateIds.Add(RestaurantTimeInfo.CanonicalTimeZoneId);
            candidateIds.Add(RestaurantTimeInfo.WindowsTimeZoneId);
        }

        return candidateIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
