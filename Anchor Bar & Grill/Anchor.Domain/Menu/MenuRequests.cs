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

public sealed record SaveMenuItemSpecialRequest(
    MenuItemSpecialScheduleKind ScheduleKind,
    DayOfWeek? DayOfWeek,
    DateOnly StartDate,
    DateOnly? EndDate,
    TimeOnly? StartsAt,
    TimeOnly? EndsAt,
    bool ClosesNextDay,
    string? Callout);

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
    IReadOnlyList<MenuTab> FoodTabs,
    SaveMenuItemSpecialRequest? Special);

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
