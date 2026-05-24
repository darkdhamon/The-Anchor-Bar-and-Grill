namespace Anchor.Infrastructure.Data.Menu;

public sealed class MenuItemSectionAssignmentEntity
{
    public Guid MenuItemId { get; set; }

    public MenuItemEntity Item { get; set; } = null!;

    public Guid MenuSectionId { get; set; }

    public MenuSectionEntity Section { get; set; } = null!;

    public int SortOrder { get; set; }
}
