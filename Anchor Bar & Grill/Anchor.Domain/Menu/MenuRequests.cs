namespace Anchor.Domain.Menu;

public sealed record SaveMenuSectionRequest(
    Guid? SectionId,
    string Name,
    MenuFamily Family,
    int SortOrder,
    bool IsVisibleToGuests,
    bool IsArchived);

public sealed record SaveMenuItemPriceVariantRequest(
    Guid? PriceVariantId,
    string Label,
    decimal Amount,
    int SortOrder);

public sealed record SaveMenuItemRequest(
    Guid? ItemId,
    Guid SectionId,
    string Name,
    string Description,
    string? ImagePath,
    int SortOrder,
    bool IsVisibleToGuests,
    bool IsArchived,
    DateOnly? OfferStartsOn,
    DateOnly? OfferEndsOn,
    bool IsSeasonal,
    IReadOnlyList<SaveMenuItemPriceVariantRequest> PriceVariants,
    IReadOnlyList<MenuTab> FoodTabs);

public sealed record SaveRecurringSpecialRequest(
    Guid? SpecialId,
    MenuTab Tab,
    Guid SectionId,
    DayOfWeek DayOfWeek,
    string Title,
    string Description,
    string TimeNote,
    string? PriceNote,
    Guid? LinkedMenuItemId,
    int SortOrder,
    bool IsVisibleToGuests,
    bool IsArchived);

public sealed record SaveMenuServiceWindowDayRequest(
    DayOfWeek DayOfWeek,
    bool IsAvailable,
    TimeOnly? OpensAt,
    TimeOnly? ClosesAt,
    bool ClosesNextDay);

public sealed record SaveMenuServiceWindowRequest(
    MenuTab Tab,
    IReadOnlyList<SaveMenuServiceWindowDayRequest> Days);

public sealed record SaveMenuSortOrderRequest(
    Guid RecordId,
    int SortOrder);
