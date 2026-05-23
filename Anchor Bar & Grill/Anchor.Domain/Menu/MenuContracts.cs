namespace Anchor.Domain.Menu;

public sealed record MenuItemPriceVariantRecord(Guid PriceVariantId, string Label, decimal Amount, int SortOrder);

public sealed record MenuItemSectionAssignmentRecord(Guid SectionId, string SectionName, int SortOrder);

public sealed record MenuItemSpecialRecord(
    Guid MenuItemId,
    MenuItemSpecialScheduleKind ScheduleKind,
    IReadOnlyList<DayOfWeek> DaysOfWeek,
    DateOnly? StartDate,
    DateOnly? EndDate,
    TimeOnly? StartsAt,
    TimeOnly? EndsAt,
    bool ClosesNextDay,
    string? Callout)
{
    public MenuItemSpecialRecord(
        Guid menuItemId,
        MenuItemSpecialScheduleKind scheduleKind,
        DayOfWeek? dayOfWeek,
        DateOnly? startDate,
        DateOnly? endDate,
        TimeOnly? startsAt,
        TimeOnly? endsAt,
        bool closesNextDay,
        string? callout)
        : this(
            menuItemId,
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

public sealed record MenuSectionRecord(
    Guid SectionId,
    string Name,
    string? Callout,
    MenuFamily Family,
    Guid? ParentSectionId,
    IReadOnlyList<MenuTab> MenuTabs,
    int SortOrder,
    bool IsVisibleToGuests,
    bool IsArchived)
{
    public MenuSectionRecord(
        Guid sectionId,
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

public sealed record MenuItemRecord(
    Guid ItemId,
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
    int? SeasonStartMonth,
    int? SeasonStartDay,
    int? SeasonEndMonth,
    int? SeasonEndDay,
    IReadOnlyList<MenuItemPriceVariantRecord> PriceVariants,
    IReadOnlyList<MenuItemSectionAssignmentRecord> SectionAssignments,
    bool UsesSectionVisibility,
    IReadOnlyList<MenuTab> MenuTabs,
    MenuItemSpecialRecord? Special)
{
    public MenuItemRecord(
        Guid itemId,
        MenuFamily family,
        string name,
        string description,
        string? imagePath,
        int sortOrder,
        bool isVisibleToGuests,
        bool isArchived,
        DateOnly? offerStartsOn,
        DateOnly? offerEndsOn,
        bool isSeasonal,
        IReadOnlyList<MenuItemPriceVariantRecord> priceVariants,
        IReadOnlyList<MenuItemSectionAssignmentRecord> sectionAssignments,
        bool usesSectionVisibility,
        IReadOnlyList<MenuTab> menuTabs,
        MenuItemSpecialRecord? special)
        : this(
            itemId,
            family,
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

public sealed record MenuSectionReferenceRecord(
    Guid SectionId,
    MenuFamily Family,
    Guid? ParentSectionId,
    IReadOnlyList<MenuTab> MenuTabs,
    bool IsArchived)
{
    public MenuSectionReferenceRecord(
        Guid sectionId,
        MenuFamily family,
        IReadOnlyList<MenuTab> menuTabs,
        bool isArchived)
        : this(sectionId, family, null, menuTabs, isArchived)
    {
    }
}

public sealed record MenuItemReferenceRecord(
    Guid ItemId,
    MenuFamily Family,
    string Name,
    string Description,
    bool IsArchived,
    IReadOnlyList<MenuItemSectionAssignmentRecord> SectionAssignments,
    bool UsesSectionVisibility,
    IReadOnlyList<MenuTab> MenuTabs,
    bool HasSpecial);
