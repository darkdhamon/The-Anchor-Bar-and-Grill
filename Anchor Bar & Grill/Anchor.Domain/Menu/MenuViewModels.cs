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

public sealed record MenuItemPriceVariantView(Guid? PriceVariantId, string Label, decimal Amount, int SortOrder)
{
    public MenuItemPriceVariantView(string Label, decimal Amount, int SortOrder)
        : this(null, Label, Amount, SortOrder)
    {
    }

    private static readonly CultureInfo UsCulture = CultureInfo.GetCultureInfo("en-US");

    public string PriceDisplay => Amount == decimal.Truncate(Amount)
        ? Amount.ToString("C0", UsCulture)
        : Amount.ToString("C2", UsCulture);
}

public sealed record MenuItemSpecialPublicView(
    MenuItemSpecialScheduleKind ScheduleKind,
    IReadOnlyList<DayOfWeek> DaysOfWeek,
    string BadgeLabel,
    string ScheduleSummary,
    string? TimeSummary,
    string? Callout,
    bool IsToday)
{
    public MenuItemSpecialPublicView(
        MenuItemSpecialScheduleKind scheduleKind,
        string badgeLabel,
        string scheduleSummary,
        string? timeSummary,
        string? callout,
        bool isToday)
        : this(scheduleKind, Array.Empty<DayOfWeek>(), badgeLabel, scheduleSummary, timeSummary, callout, isToday)
    {
    }

    public MenuItemSpecialPublicView(
        MenuItemSpecialScheduleKind scheduleKind,
        DayOfWeek? dayOfWeek,
        string badgeLabel,
        string scheduleSummary,
        string? timeSummary,
        string? callout,
        bool isToday)
        : this(scheduleKind, dayOfWeek is null ? Array.Empty<DayOfWeek>() : [dayOfWeek.Value], badgeLabel, scheduleSummary, timeSummary, callout, isToday)
    {
    }
}

public sealed record PublicHomeSpecialView(
    Guid ItemId,
    string BadgeLabel,
    string Title,
    string Description,
    string? TimeSummary,
    string? Callout,
    string PlacementSummary,
    string? AvailabilityLabel,
    bool IsAvailableNow);

public sealed record PublicMenuItemView(
    Guid ItemId,
    string Name,
    string Description,
    string? ImagePath,
    IReadOnlyList<MenuItemPriceVariantView> PriceVariants,
    IReadOnlyList<string> StatusLabels,
    string? OfferDateSummary,
    MenuItemSpecialPublicView? Special);

public sealed record PublicMenuChildSectionView(
    Guid SectionId,
    string Name,
    string? Callout,
    IReadOnlyList<PublicMenuItemView> Items);

public sealed record PublicMenuSectionEntryView(
    int SortOrder,
    PublicMenuItemView? Item,
    PublicMenuChildSectionView? ChildSection)
{
    public bool IsChildSection => ChildSection is not null;
}

public sealed record PublicMenuSectionView(
    Guid SectionId,
    string Name,
    string? Callout,
    IReadOnlyList<PublicMenuSectionEntryView> Entries)
{
    public PublicMenuSectionView(
        Guid sectionId,
        string name,
        string? callout,
        IReadOnlyList<PublicMenuItemView> items)
        : this(
            sectionId,
            name,
            callout,
            items.Select((item, index) => new PublicMenuSectionEntryView(index + 1, item, null)).ToArray())
    {
    }

    public IReadOnlyList<PublicMenuItemView> Items =>
        Entries
            .SelectMany(entry => entry.ChildSection?.Items ?? (entry.Item is null ? Array.Empty<PublicMenuItemView>() : [entry.Item]))
            .ToArray();
}

public sealed record PublicMenuView(
    MenuTab SelectedTab,
    IReadOnlyList<MenuTabLinkView> Tabs,
    IReadOnlyList<MenuServiceWindowView> ServiceHours,
    IReadOnlyList<PublicMenuSectionView> Sections);

public sealed record MenuSectionAdminView(
    Guid SectionId,
    string Name,
    string? Callout,
    MenuFamily Family,
    Guid? ParentSectionId,
    string? ParentSectionName,
    IReadOnlyList<MenuTab> MenuTabs,
    int SortOrder,
    bool IsVisibleToGuests,
    bool IsArchived,
    IReadOnlyList<string> StatusLabels)
{
    public MenuSectionAdminView(
        Guid sectionId,
        string name,
        string? callout,
        MenuFamily family,
        IReadOnlyList<MenuTab> menuTabs,
        int sortOrder,
        bool isVisibleToGuests,
        bool isArchived,
        IReadOnlyList<string> statusLabels)
        : this(sectionId, name, callout, family, null, null, menuTabs, sortOrder, isVisibleToGuests, isArchived, statusLabels)
    {
    }
}

public sealed record MenuItemSectionAssignmentView(
    Guid SectionId,
    string SectionName,
    int SortOrder);

public sealed record MenuItemAdminView(
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
    IReadOnlyList<MenuItemSectionAssignmentView> SectionAssignments,
    bool UsesSectionVisibility,
    IReadOnlyList<MenuTab> MenuTabs,
    IReadOnlyList<MenuItemPriceVariantView> PriceVariants,
    IReadOnlyList<string> StatusLabels,
    string? OfferDateSummary,
    MenuItemSpecialAdminView? Special)
{
    public MenuItemAdminView(
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
        IReadOnlyList<MenuItemSectionAssignmentView> sectionAssignments,
        bool usesSectionVisibility,
        IReadOnlyList<MenuTab> menuTabs,
        IReadOnlyList<MenuItemPriceVariantView> priceVariants,
        IReadOnlyList<string> statusLabels,
        string? offerDateSummary,
        MenuItemSpecialAdminView? special)
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
            sectionAssignments,
            usesSectionVisibility,
            menuTabs,
            priceVariants,
            statusLabels,
            offerDateSummary,
            special)
    {
    }
}

public sealed record MenuItemSpecialAdminView(
    MenuItemSpecialScheduleKind ScheduleKind,
    IReadOnlyList<DayOfWeek> DaysOfWeek,
    DateOnly? StartDate,
    DateOnly? EndDate,
    TimeOnly? StartsAt,
    TimeOnly? EndsAt,
    bool ClosesNextDay,
    string BadgeLabel,
    string ScheduleSummary,
    string? TimeSummary,
    string? Callout,
    IReadOnlyList<string> StatusLabels,
    bool IsToday)
{
    public MenuItemSpecialAdminView(
        MenuItemSpecialScheduleKind scheduleKind,
        DayOfWeek? dayOfWeek,
        DateOnly? startDate,
        DateOnly? endDate,
        TimeOnly? startsAt,
        TimeOnly? endsAt,
        bool closesNextDay,
        string badgeLabel,
        string scheduleSummary,
        string? timeSummary,
        string? callout,
        IReadOnlyList<string> statusLabels,
        bool isToday)
        : this(
            scheduleKind,
            dayOfWeek is null ? Array.Empty<DayOfWeek>() : [dayOfWeek.Value],
            startDate,
            endDate,
            startsAt,
            endsAt,
            closesNextDay,
            badgeLabel,
            scheduleSummary,
            timeSummary,
            callout,
            statusLabels,
            isToday)
    {
    }

    public DayOfWeek? DayOfWeek => DaysOfWeek.Count == 1 ? DaysOfWeek[0] : null;
}

public sealed record MenuTabHoursAdminView(
    MenuTab Tab,
    string Label,
    IReadOnlyList<MenuServiceWindowView> Days);

public sealed record MenuManagementView(
    IReadOnlyList<MenuTabHoursAdminView> Tabs,
    IReadOnlyList<MenuSectionAdminView> Sections,
    IReadOnlyList<MenuItemAdminView> Items);
