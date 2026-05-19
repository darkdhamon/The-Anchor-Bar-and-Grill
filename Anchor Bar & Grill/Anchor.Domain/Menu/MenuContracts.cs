namespace Anchor.Domain.Menu;

public sealed record MenuItemPriceVariantRecord(Guid PriceVariantId, string Label, decimal Amount, int SortOrder);

public sealed record MenuItemSpecialRecord(
    Guid MenuItemId,
    MenuItemSpecialScheduleKind ScheduleKind,
    DayOfWeek? DayOfWeek,
    DateOnly StartDate,
    DateOnly? EndDate,
    TimeOnly? StartsAt,
    TimeOnly? EndsAt,
    bool ClosesNextDay,
    string? Callout);

public sealed record MenuSectionRecord(
    Guid SectionId,
    string Name,
    MenuFamily Family,
    int SortOrder,
    bool IsVisibleToGuests,
    bool IsArchived);

public sealed record MenuItemRecord(
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
    IReadOnlyList<MenuItemPriceVariantRecord> PriceVariants,
    IReadOnlyList<MenuTab> FoodTabs,
    MenuItemSpecialRecord? Special);

public sealed record MenuServiceWindowRecord(
    MenuTab Tab,
    DayOfWeek DayOfWeek,
    bool IsAvailable,
    TimeOnly? OpensAt,
    TimeOnly? ClosesAt,
    bool ClosesNextDay);

public sealed record PublicMenuSnapshot(
    MenuTab Tab,
    IReadOnlyList<MenuSectionRecord> Sections,
    IReadOnlyList<MenuItemRecord> Items,
    IReadOnlyList<MenuServiceWindowRecord> ServiceWindows);

public sealed record MenuManagementSnapshot(
    IReadOnlyList<MenuSectionRecord> Sections,
    IReadOnlyList<MenuItemRecord> Items,
    IReadOnlyList<MenuServiceWindowRecord> ServiceWindows);

public sealed record MenuSectionReferenceRecord(Guid SectionId, MenuFamily Family, bool IsArchived);

public sealed record MenuItemReferenceRecord(
    Guid ItemId,
    Guid SectionId,
    MenuFamily Family,
    string Name,
    string Description,
    bool IsArchived,
    IReadOnlyList<MenuTab> FoodTabs,
    bool HasSpecial);
