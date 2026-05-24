namespace Anchor.Infrastructure.Data.Menu;

public sealed class MenuItemPriceVariantEntity
{
    public Guid MenuItemPriceVariantId { get; set; }

    public Guid MenuItemId { get; set; }

    public MenuItemEntity Item { get; set; } = null!;

    public string Label { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int SortOrder { get; set; }
}
