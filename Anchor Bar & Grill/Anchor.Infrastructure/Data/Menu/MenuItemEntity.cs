namespace Anchor.Infrastructure.Data.Menu;

public sealed class MenuItemEntity
{
    public Guid MenuItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public int SortOrder { get; set; }

    public bool IsVisibleToGuests { get; set; }

    public bool IsArchived { get; set; }

    public DateOnly? OfferStartsOn { get; set; }

    public DateOnly? OfferEndsOn { get; set; }

    public bool IsSeasonal { get; set; }

    public int? SeasonStartMonth { get; set; }

    public int? SeasonStartDay { get; set; }

    public int? SeasonEndMonth { get; set; }

    public int? SeasonEndDay { get; set; }

    public ICollection<MenuItemPriceVariantEntity> PriceVariants { get; set; } = [];

    public bool UsesSectionVisibility { get; set; } = true;

    public ICollection<MenuItemSectionAssignmentEntity> SectionAssignments { get; set; } = [];

    public ICollection<MenuItemTabEntity> MenuTabs { get; set; } = [];

    public MenuItemSpecialEntity? Special { get; set; }
}
