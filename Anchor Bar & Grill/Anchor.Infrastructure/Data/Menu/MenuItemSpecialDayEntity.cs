namespace Anchor.Infrastructure.Data.Menu;

public sealed class MenuItemSpecialDayEntity
{
    public Guid MenuItemId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public MenuItemSpecialEntity Special { get; set; } = null!;
}
