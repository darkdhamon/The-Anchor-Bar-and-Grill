using System.Globalization;

namespace Anchor.Domain.Menu;

public sealed record MenuTabLinkView(MenuTab Tab, string Label, string QueryValue, bool IsSelected, bool HasVisibleContent);

public sealed record MenuServiceWindowView(
    DayOfWeek DayOfWeek,
    string DayLabel,
    bool IsAvailable,
    string Summary,
    bool IsToday,
    TimeOnly? OpensAt,
    TimeOnly? ClosesAt,
    bool ClosesNextDay);

public sealed record MenuItemPriceVariantView(string Label, decimal Amount, int SortOrder)
{
    private static readonly CultureInfo UsCulture = CultureInfo.GetCultureInfo("en-US");

    public string PriceDisplay => Amount == decimal.Truncate(Amount)
        ? Amount.ToString("C0", UsCulture)
        : Amount.ToString("C2", UsCulture);
}

public sealed record PublicRecurringSpecialView(
    Guid SpecialId,
    string DayLabel,
    string Title,
    string Description,
    string TimeNote,
    string? PriceNote,
    string PlacementSummary,
    bool IsToday);

public sealed record PublicMenuItemView(
    Guid ItemId,
    string Name,
    string Description,
    string? ImagePath,
    IReadOnlyList<MenuItemPriceVariantView> PriceVariants,
    IReadOnlyList<string> StatusLabels,
    string? OfferDateSummary);

public sealed record PublicMenuSectionView(
    Guid SectionId,
    string Name,
    string AccentClass,
    IReadOnlyList<PublicRecurringSpecialView> Specials,
    IReadOnlyList<PublicMenuItemView> Items);

public sealed record PublicMenuView(
    MenuTab SelectedTab,
    IReadOnlyList<MenuTabLinkView> Tabs,
    IReadOnlyList<MenuServiceWindowView> ServiceHours,
    IReadOnlyList<PublicMenuSectionView> Sections);

public sealed record MenuSectionAdminView(
    Guid SectionId,
    string Name,
    MenuFamily Family,
    int SortOrder,
    bool IsVisibleToGuests,
    bool IsArchived,
    IReadOnlyList<string> StatusLabels);

public sealed record MenuItemAdminView(
    Guid ItemId,
    Guid SectionId,
    string SectionName,
    MenuFamily Family,
    string Name,
    string Description,
    string? ImagePath,
    int SortOrder,
    bool IsVisibleToGuests,
    bool IsArchived,
    DateOnly? OfferStartsOn,
    DateOnly? OfferEndsOn,
    bool IsSeasonal,
    IReadOnlyList<MenuTab> FoodTabs,
    IReadOnlyList<MenuItemPriceVariantView> PriceVariants,
    IReadOnlyList<string> StatusLabels,
    string? OfferDateSummary);

public sealed record RecurringSpecialAdminView(
    Guid SpecialId,
    MenuTab Tab,
    Guid SectionId,
    string SectionName,
    DayOfWeek DayOfWeek,
    string DayLabel,
    string Title,
    string Description,
    string TimeNote,
    string? PriceNote,
    Guid? LinkedMenuItemId,
    string? LinkedMenuItemName,
    int SortOrder,
    bool IsVisibleToGuests,
    bool IsArchived,
    IReadOnlyList<string> StatusLabels,
    bool IsToday);

public sealed record MenuTabHoursAdminView(
    MenuTab Tab,
    string Label,
    IReadOnlyList<MenuServiceWindowView> Days);

public sealed record MenuManagementView(
    IReadOnlyList<MenuTabHoursAdminView> Tabs,
    IReadOnlyList<MenuSectionAdminView> Sections,
    IReadOnlyList<MenuItemAdminView> Items,
    IReadOnlyList<RecurringSpecialAdminView> RecurringSpecials);
