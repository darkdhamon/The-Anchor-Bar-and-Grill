namespace Anchor.Web.Configuration;

public sealed class RestaurantTimeOptions
{
    public const string SectionName = "RestaurantTime";

    public string TimeZoneId { get; set; } = "America/Chicago";
}
