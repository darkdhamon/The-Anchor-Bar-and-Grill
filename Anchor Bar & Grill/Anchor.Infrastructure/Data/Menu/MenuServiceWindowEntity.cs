using Anchor.Domain.Menu;

namespace Anchor.Infrastructure.Data.Menu;

public sealed class MenuServiceWindowEntity
{
    public MenuTab Tab { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public bool IsAvailable { get; set; }

    public TimeOnly? OpensAt { get; set; }

    public TimeOnly? ClosesAt { get; set; }

    public bool ClosesNextDay { get; set; }
}
