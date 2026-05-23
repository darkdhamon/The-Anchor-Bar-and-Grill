namespace Anchor.Domain.Menu;

public sealed record SaveMenuSectionRequest(
    Guid? SectionId,
    string Name,
    string? Callout,
    MenuFamily Family,
    Guid? ParentSectionId,
    IReadOnlyList<MenuTab> MenuTabs,
    int SortOrder,
    bool IsVisibleToGuests,
    bool IsArchived)
{
    public SaveMenuSectionRequest(
        Guid? sectionId,
        string name,
        string? callout,
        MenuFamily family,
        IReadOnlyList<MenuTab> menuTabs,
        int sortOrder,
        bool isVisibleToGuests,
        bool isArchived)
        : this(sectionId, name, callout, family, null, menuTabs, sortOrder, isVisibleToGuests, isArchived)
    {
    }
}

public sealed record SaveMenuItemPriceVariantRequest(
    Guid? PriceVariantId,
    string Label,
    decimal Amount,
    int SortOrder);

public sealed record SaveMenuItemSectionAssignmentRequest(Guid SectionId, int SortOrder);

public sealed record SaveMenuItemSpecialRequest(
    MenuItemSpecialScheduleKind ScheduleKind,
    IReadOnlyList<DayOfWeek> DaysOfWeek,
    DateOnly? StartDate,
    DateOnly? EndDate,
    TimeOnly? StartsAt,
    TimeOnly? EndsAt,
    bool ClosesNextDay,
    string? Callout)
{
    public SaveMenuItemSpecialRequest(
        MenuItemSpecialScheduleKind scheduleKind,
        DayOfWeek? dayOfWeek,
        DateOnly? startDate,
        DateOnly? endDate,
        TimeOnly? startsAt,
        TimeOnly? endsAt,
        bool closesNextDay,
        string? callout)
        : this(
            scheduleKind,
            dayOfWeek is null ? Array.Empty<DayOfWeek>() : [dayOfWeek.Value],
            startDate,
            endDate,
            startsAt,
            endsAt,
            closesNextDay,
            callout)
    {
    }

    public DayOfWeek? DayOfWeek => DaysOfWeek.Count == 1 ? DaysOfWeek[0] : null;
}

public sealed record SaveMenuItemRequest(
    Guid? ItemId,
    string Name,
    string Description,
    string? ImagePath,
    int SortOrder,
    bool IsVisibleToGuests,
    bool IsArchived,
    DateOnly? OfferStartsOn,
    DateOnly? OfferEndsOn,
    bool IsSeasonal,
    int? SeasonStartMonth,
    int? SeasonStartDay,
    int? SeasonEndMonth,
    int? SeasonEndDay,
    IReadOnlyList<SaveMenuItemPriceVariantRequest> PriceVariants,
    IReadOnlyList<SaveMenuItemSectionAssignmentRequest> SectionAssignments,
    bool UsesSectionVisibility,
    IReadOnlyList<MenuTab> MenuTabs,
    SaveMenuItemSpecialRequest? Special)
{
    public SaveMenuItemRequest(
        Guid? itemId,
        string name,
        string description,
        string? imagePath,
        int sortOrder,
        bool isVisibleToGuests,
        bool isArchived,
        DateOnly? offerStartsOn,
        DateOnly? offerEndsOn,
        bool isSeasonal,
        IReadOnlyList<SaveMenuItemPriceVariantRequest> priceVariants,
        IReadOnlyList<SaveMenuItemSectionAssignmentRequest> sectionAssignments,
        bool usesSectionVisibility,
        IReadOnlyList<MenuTab> menuTabs,
        SaveMenuItemSpecialRequest? special)
        : this(
            itemId,
            name,
            description,
            imagePath,
            sortOrder,
            isVisibleToGuests,
            isArchived,
            offerStartsOn,
            offerEndsOn,
            isSeasonal,
            null,
            null,
            null,
            null,
            priceVariants,
            sectionAssignments,
            usesSectionVisibility,
            menuTabs,
            special)
    {
    }
}

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
    int SortOrder,
    Guid? ContextId = null);
