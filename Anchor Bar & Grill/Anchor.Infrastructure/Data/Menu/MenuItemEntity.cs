namespace Anchor.Infrastructure.Data.Menu;

public sealed class MenuItemEntity
{
    public Guid MenuItemId { get; set; }

    public Guid MenuSectionId { get; set; }

    public MenuSectionEntity Section { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public int SortOrder { get; set; }

    public bool IsVisibleToGuests { get; set; }

    public bool IsArchived { get; set; }

    public DateOnly? OfferStartsOn { get; set; }

    public DateOnly? OfferEndsOn { get; set; }

    public bool IsSeasonal { get; set; }

    public ICollection<MenuItemPriceVariantEntity> PriceVariants { get; set; } = [];

    public ICollection<MenuItemTabEntity> FoodTabs { get; set; } = [];

    public MenuItemSpecialEntity? Special { get; set; }
}
