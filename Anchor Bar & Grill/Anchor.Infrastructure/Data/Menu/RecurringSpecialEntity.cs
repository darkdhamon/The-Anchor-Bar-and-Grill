using Anchor.Domain.Menu;

namespace Anchor.Infrastructure.Data.Menu;

public sealed class RecurringSpecialEntity
{
    public Guid RecurringSpecialId { get; set; }

    public MenuTab Tab { get; set; }

    public Guid MenuSectionId { get; set; }

    public MenuSectionEntity Section { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string TimeNote { get; set; } = string.Empty;

    public string? PriceNote { get; set; }

    public Guid? LinkedMenuItemId { get; set; }

    public MenuItemEntity? LinkedMenuItem { get; set; }

    public int SortOrder { get; set; }

    public bool IsVisibleToGuests { get; set; }

    public bool IsArchived { get; set; }
}
