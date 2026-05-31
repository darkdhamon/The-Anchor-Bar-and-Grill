using Anchor.Domain.Menu;
using System.ComponentModel.DataAnnotations.Schema;

namespace Anchor.Infrastructure.Data.Menu;

public sealed class MenuItemSpecialEntity
{
    public Guid MenuItemId { get; set; }

    public MenuItemEntity Item { get; set; } = null!;

    public MenuItemSpecialScheduleKind ScheduleKind { get; set; }

    public ICollection<MenuItemSpecialDayEntity> Days { get; set; } = [];

    [NotMapped]
    public DayOfWeek? DayOfWeek
    {
        get => Days.Count == 1 ? Days.First().DayOfWeek : null;
        set
        {
            Days.Clear();
            if (value is { } dayOfWeek)
            {
                Days.Add(new MenuItemSpecialDayEntity
                {
                    MenuItemId = MenuItemId,
                    DayOfWeek = dayOfWeek
                });
            }
        }
    }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public TimeOnly? StartsAt { get; set; }

    public TimeOnly? EndsAt { get; set; }

    public bool ClosesNextDay { get; set; }

    public string? Callout { get; set; }
}
