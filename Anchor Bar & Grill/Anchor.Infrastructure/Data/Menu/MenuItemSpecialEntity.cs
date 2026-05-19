using Anchor.Domain.Menu;

namespace Anchor.Infrastructure.Data.Menu;

public sealed class MenuItemSpecialEntity
{
    public Guid MenuItemId { get; set; }

    public MenuItemEntity Item { get; set; } = null!;

    public MenuItemSpecialScheduleKind ScheduleKind { get; set; }

    public DayOfWeek? DayOfWeek { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public TimeOnly? StartsAt { get; set; }

    public TimeOnly? EndsAt { get; set; }

    public bool ClosesNextDay { get; set; }

    public string? Callout { get; set; }
}
