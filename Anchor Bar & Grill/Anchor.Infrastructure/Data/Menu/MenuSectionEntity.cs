using Anchor.Domain.Menu;

namespace Anchor.Infrastructure.Data.Menu;

public sealed class MenuSectionEntity
{
    public Guid MenuSectionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public MenuFamily Family { get; set; }

    public int SortOrder { get; set; }

    public bool IsVisibleToGuests { get; set; }

    public bool IsArchived { get; set; }

    public ICollection<MenuItemEntity> Items { get; set; } = [];

    public ICollection<RecurringSpecialEntity> RecurringSpecials { get; set; } = [];
}
