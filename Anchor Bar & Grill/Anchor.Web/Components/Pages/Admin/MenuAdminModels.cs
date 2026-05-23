using Anchor.Domain.Menu;
using Anchor.Web.Components.Pages;

namespace Anchor.Web.Components.Pages.Admin;

internal enum MenuEditorTab
{
    Food,
    Drinks,
    Hours
}

internal enum MenuArchiveFilter
{
    Active,
    Both,
    Archived
}

internal enum MenuContentFilter
{
    All,
    Standard,
    Specials
}

internal enum MenuAdminDetailKind
{
    None,
    Section,
    Item
}

internal sealed record MenuAdminBrowserItemViewModel(
    MenuItemAdminView Item,
    Guid SectionId,
    bool IsContextMuted);

internal sealed record MenuAdminBrowserSectionViewModel(
    MenuSectionAdminView Section,
    bool IsContextMuted,
    IReadOnlyList<MenuAdminBrowserItemViewModel> Items);

internal sealed record MenuAdminHoursSummaryViewModel(
    MenuTab Tab,
    string Label,
    MenuHoursCardView Card);

internal sealed record MenuAdminDuplicateItemPromptViewModel(
    Guid ItemId,
    string Name,
    MenuFamily Family,
    bool IsArchived,
    bool IsSpecial);

internal sealed class MenuSectionFormModel
{
    public Guid? SectionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Callout { get; set; }

    public MenuFamily Family { get; set; } = MenuFamily.Food;

    public Guid? ParentSectionId { get; set; }

    public bool ShowBreakfast { get; set; }

    public bool ShowLunch { get; set; } = true;

    public bool ShowDinner { get; set; } = true;

    public bool ShowDrinks { get; set; }

    public int SortOrder { get; set; } = 1;

    public bool IsVisibleToGuests { get; set; } = true;

    public bool IsArchived { get; set; }
}

internal sealed class MenuItemPriceVariantFormModel
{
    public Guid? PriceVariantId { get; set; }

    public string Label { get; set; } = "Regular";

    public string AmountText { get; set; } = "0.00";

    public int SortOrder { get; set; } = 1;
}

internal sealed class MenuItemFormModel
{
    public Guid? ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public int SortOrder { get; set; } = 1;

    public bool IsVisibleToGuests { get; set; } = true;

    public bool IsArchived { get; set; }

    public string? OfferStartsOnText { get; set; }

    public string? OfferEndsOnText { get; set; }

    public bool IsSeasonal { get; set; }

    public bool UseRecurringSeasonWindow { get; set; }

    public int? SeasonStartMonth { get; set; }

    public int? SeasonStartDay { get; set; }

    public int? SeasonEndMonth { get; set; }

    public int? SeasonEndDay { get; set; }

    public bool UseSectionVisibility { get; set; } = true;

    public HashSet<Guid> SelectedSectionIds { get; } = [];

    public Dictionary<Guid, int> SectionSortOrders { get; } = [];

    public Guid? ActiveSectionId { get; set; }

    public bool ShowBreakfast { get; set; }

    public bool ShowLunch { get; set; } = true;

    public bool ShowDinner { get; set; } = true;

    public bool IsSpecial { get; set; }

    public MenuItemSpecialScheduleKind SpecialScheduleKind { get; set; } = MenuItemSpecialScheduleKind.WeeklyRecurring;

    public HashSet<DayOfWeek> SelectedSpecialDays { get; } = [];

    public string? SpecialStartDateText { get; set; }

    public string? SpecialEndDateText { get; set; }

    public string? SpecialStartsAtText { get; set; }

    public string? SpecialEndsAtText { get; set; }

    public bool SpecialClosesNextDay { get; set; }

    public string? SpecialCallout { get; set; }

    public List<MenuItemPriceVariantFormModel> PriceVariants { get; } =
    [
        new()
    ];
}

internal sealed class MenuServiceWindowDayFormModel
{
    public DayOfWeek DayOfWeek { get; set; }

    public bool IsAvailable { get; set; }

    public string? OpensAtText { get; set; }

    public string? ClosesAtText { get; set; }

    public bool ClosesNextDay { get; set; }
}

internal sealed class MenuServiceWindowFormModel
{
    public MenuTab Tab { get; set; } = MenuTab.Lunch;

    public List<MenuServiceWindowDayFormModel> Days { get; } = [];
}
