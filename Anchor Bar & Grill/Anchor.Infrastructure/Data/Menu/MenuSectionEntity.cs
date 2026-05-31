using Anchor.Domain.Menu;

namespace Anchor.Infrastructure.Data.Menu;

public sealed class MenuSectionEntity
{
    public Guid MenuSectionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string? Callout { get; set; }

    public MenuFamily Family { get; set; }

    public Guid? ParentSectionId { get; set; }

    public MenuSectionEntity? ParentSection { get; set; }

    public ICollection<MenuSectionEntity> ChildSections { get; set; } = [];

    public int SortOrder { get; set; }

    public bool IsVisibleToGuests { get; set; }

    public bool IsArchived { get; set; }

    public ICollection<MenuSectionTabEntity> MenuTabs { get; set; } = [];

    public ICollection<MenuItemSectionAssignmentEntity> ItemAssignments { get; set; } = [];
}
