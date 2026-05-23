using Anchor.Domain.Menu;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Anchor.Infrastructure.Data.Menu;

public sealed class MenuQueryRepository(ApplicationDbContext dbContext) : IMenuQueryRepository
{
    public async Task<IReadOnlyList<MenuServiceWindowRecord>> GetPublicServiceWindowsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.MenuServiceWindows
            .AsNoTracking()
            .OrderBy(window => window.Tab)
            .ThenBy(window => window.DayOfWeek)
            .Select(window => new MenuServiceWindowRecord(
                window.Tab,
                window.DayOfWeek,
                window.IsAvailable,
                window.OpensAt,
                window.ClosesAt,
                window.ClosesNextDay))
            .ToListAsync(cancellationToken);

    public async Task<PublicMenuSnapshot> GetPublicMenuSnapshotAsync(
        MenuTab tab,
        DateOnly today,
        DateOnly comingSoonCutoff,
        CancellationToken cancellationToken = default)
    {
        var family = tab == MenuTab.Drinks ? MenuFamily.Drink : MenuFamily.Food;
        var sections = await dbContext.MenuSections
            .AsNoTracking()
            .Where(section => section.Family == family)
            .Where(section => section.IsVisibleToGuests && !section.IsArchived)
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Name)
            .Select(SectionProjection)
            .ToListAsync(cancellationToken);

        var items = await dbContext.MenuItems
            .AsNoTracking()
            .Where(item => item.IsVisibleToGuests && !item.IsArchived)
            .Where(item => item.SectionAssignments.Any(assignment => assignment.Section.Family == family))
            .Select(ItemProjection)
            .ToListAsync(cancellationToken);

        var sectionsById = sections.ToDictionary(section => section.SectionId);
        var filteredItems = items
            .Where(item => IsItemVisibleOnTab(item, sectionsById, tab, today, comingSoonCutoff))
            .ToArray();

        var visibleSectionIds = filteredItems
            .SelectMany(item => item.SectionAssignments)
            .Where(assignment => sectionsById.TryGetValue(assignment.SectionId, out var section) && section.MenuTabs.Contains(tab))
            .Select(assignment => assignment.SectionId)
            .Distinct()
            .ToHashSet();

        foreach (var sectionId in visibleSectionIds.ToArray())
        {
            var currentSectionId = sectionId;
            var visitedSectionIds = new HashSet<Guid>();

            while (sectionsById.TryGetValue(currentSectionId, out var currentSection)
                && currentSection.ParentSectionId is { } parentSectionId
                && visitedSectionIds.Add(parentSectionId)
                && sectionsById.TryGetValue(parentSectionId, out var parentSection)
                && parentSection.MenuTabs.Contains(tab))
            {
                visibleSectionIds.Add(parentSectionId);
                currentSectionId = parentSectionId;
            }
        }

        var windows = await dbContext.MenuServiceWindows
            .AsNoTracking()
            .Where(window => window.Tab == tab)
            .OrderBy(window => window.DayOfWeek)
            .Select(window => new MenuServiceWindowRecord(
                window.Tab,
                window.DayOfWeek,
                window.IsAvailable,
                window.OpensAt,
                window.ClosesAt,
                window.ClosesNextDay))
            .ToListAsync(cancellationToken);

        return new PublicMenuSnapshot(
            tab,
            sections.Where(section => visibleSectionIds.Contains(section.SectionId)).ToArray(),
            filteredItems,
            windows);
    }

    public async Task<IReadOnlyList<MenuItemRecord>> GetHomeSpecialItemsAsync(
        DateOnly today,
        DateOnly comingSoonCutoff,
        CancellationToken cancellationToken = default)
    {
        var sections = await dbContext.MenuSections
            .AsNoTracking()
            .Where(section => section.IsVisibleToGuests && !section.IsArchived)
            .Select(SectionProjection)
            .ToListAsync(cancellationToken);
        var sectionsById = sections.ToDictionary(section => section.SectionId);

        var items = await dbContext.MenuItems
            .AsNoTracking()
            .Where(item => item.Special != null)
            .Where(item => item.IsVisibleToGuests && !item.IsArchived)
            .Where(item => item.SectionAssignments.Any(assignment => assignment.Section.IsVisibleToGuests && !assignment.Section.IsArchived))
            .Select(ItemProjection)
            .ToListAsync(cancellationToken);

        return items
            .Where(item => IsSpecialVisibleForHome(item, sectionsById, today, comingSoonCutoff))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<MenuTab>> GetTabsWithVisibleContentAsync(
        DateOnly today,
        DateOnly comingSoonCutoff,
        CancellationToken cancellationToken = default)
    {
        var sections = await dbContext.MenuSections
            .AsNoTracking()
            .Where(section => section.IsVisibleToGuests && !section.IsArchived)
            .Select(SectionProjection)
            .ToListAsync(cancellationToken);
        var sectionsById = sections.ToDictionary(section => section.SectionId);

        var items = await dbContext.MenuItems
            .AsNoTracking()
            .Where(item => item.IsVisibleToGuests && !item.IsArchived)
            .Where(item => item.SectionAssignments.Any(assignment => assignment.Section.IsVisibleToGuests && !assignment.Section.IsArchived))
            .Select(ItemProjection)
            .ToListAsync(cancellationToken);

        List<MenuTab> tabs = [];
        foreach (var tab in Enum.GetValues<MenuTab>())
        {
            if (items.Any(item => IsItemVisibleOnTab(item, sectionsById, tab, today, comingSoonCutoff)))
            {
                tabs.Add(tab);
            }
        }

        return tabs;
    }

    public async Task<MenuManagementSnapshot> GetMenuManagementSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var sections = await dbContext.MenuSections
            .AsNoTracking()
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Name)
            .Select(SectionProjection)
            .ToListAsync(cancellationToken);

        var items = await dbContext.MenuItems
            .AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .Select(ItemProjection)
            .ToListAsync(cancellationToken);

        var windows = await dbContext.MenuServiceWindows
            .AsNoTracking()
            .OrderBy(window => window.Tab)
            .ThenBy(window => window.DayOfWeek)
            .Select(window => new MenuServiceWindowRecord(
                window.Tab,
                window.DayOfWeek,
                window.IsAvailable,
                window.OpensAt,
                window.ClosesAt,
                window.ClosesNextDay))
            .ToListAsync(cancellationToken);

        return new MenuManagementSnapshot(sections, items, windows);
    }

    private static readonly Expression<Func<MenuSectionEntity, MenuSectionRecord>> SectionProjection = section =>
        new MenuSectionRecord(
            section.MenuSectionId,
            section.Name,
            section.Callout,
            section.Family,
            section.ParentSectionId,
            section.MenuTabs
                .OrderBy(link => link.Tab)
                .Select(link => link.Tab)
                .ToList(),
            section.SortOrder,
            section.IsVisibleToGuests,
            section.IsArchived);

    private static readonly Expression<Func<MenuItemEntity, MenuItemRecord>> ItemProjection = item =>
        new MenuItemRecord(
            item.MenuItemId,
            item.SectionAssignments
                .Select(assignment => assignment.Section.Family)
                .FirstOrDefault(),
            item.Name,
            item.Description,
            item.ImagePath,
            item.SortOrder,
            item.IsVisibleToGuests,
            item.IsArchived,
            item.OfferStartsOn,
            item.OfferEndsOn,
            item.IsSeasonal,
            item.SeasonStartMonth,
            item.SeasonStartDay,
            item.SeasonEndMonth,
            item.SeasonEndDay,
            item.PriceVariants
                .OrderBy(variant => variant.SortOrder)
                .Select(variant => new MenuItemPriceVariantRecord(
                    variant.MenuItemPriceVariantId,
                    variant.Label,
                    variant.Amount,
                    variant.SortOrder))
                .ToList(),
            item.SectionAssignments
                .OrderBy(assignment => assignment.SortOrder)
                .ThenBy(assignment => assignment.Section.SortOrder)
                .ThenBy(assignment => assignment.Section.Name)
                .Select(assignment => new MenuItemSectionAssignmentRecord(
                    assignment.MenuSectionId,
                    assignment.Section.Name,
                    assignment.SortOrder))
                .ToList(),
            item.UsesSectionVisibility,
            item.MenuTabs
                .OrderBy(link => link.Tab)
                .Select(link => link.Tab)
                .ToList(),
            item.Special == null
                ? null
                : new MenuItemSpecialRecord(
                    item.Special.MenuItemId,
                    item.Special.ScheduleKind,
                    item.Special.Days
                        .OrderBy(day => day.DayOfWeek)
                        .Select(day => day.DayOfWeek)
                        .ToList(),
                    item.Special.StartDate,
                    item.Special.EndDate,
                    item.Special.StartsAt,
                    item.Special.EndsAt,
                    item.Special.ClosesNextDay,
                    item.Special.Callout));

    private static bool IsItemVisibleOnTab(
        MenuItemRecord item,
        IReadOnlyDictionary<Guid, MenuSectionRecord> sectionsById,
        MenuTab tab,
        DateOnly today,
        DateOnly comingSoonCutoff)
    {
        var visibleAssignments = item.SectionAssignments
            .Where(assignment => sectionsById.TryGetValue(assignment.SectionId, out var section) && section.MenuTabs.Contains(tab))
            .ToArray();
        if (visibleAssignments.Length == 0)
        {
            return false;
        }

        if (!item.UsesSectionVisibility && !item.MenuTabs.Contains(tab))
        {
            return false;
        }

        if (item.Special is null)
        {
            return (item.OfferEndsOn is null || item.OfferEndsOn >= today)
                && (item.OfferStartsOn is null || item.OfferStartsOn <= comingSoonCutoff)
                && MenuAvailabilityRules.IsItemWithinRecurringSeason(item, today);
        }

        if (!IsItemLifetimeActiveToday(item, today) || !MenuAvailabilityRules.IsItemWithinRecurringSeason(item, today))
        {
            return false;
        }

        return item.Special.ScheduleKind == MenuItemSpecialScheduleKind.WeeklyRecurring
            ? item.Special.DaysOfWeek.Contains(today.DayOfWeek)
            : item.Special.StartDate is { } specialStart
                && specialStart <= comingSoonCutoff
                && (item.Special.EndDate ?? specialStart) >= today;
    }

    private static bool IsSpecialVisibleForHome(
        MenuItemRecord item,
        IReadOnlyDictionary<Guid, MenuSectionRecord> sectionsById,
        DateOnly today,
        DateOnly comingSoonCutoff)
    {
        if (item.Special is null)
        {
            return false;
        }

        var hasVisibleSection = item.SectionAssignments.Any(assignment => sectionsById.ContainsKey(assignment.SectionId));
        if (!hasVisibleSection)
        {
            return false;
        }

        if (!IsItemLifetimeActiveToday(item, today) || !MenuAvailabilityRules.IsItemWithinRecurringSeason(item, today))
        {
            return false;
        }

        return item.Special.ScheduleKind == MenuItemSpecialScheduleKind.WeeklyRecurring
            ? item.Special.DaysOfWeek.Contains(today.DayOfWeek)
            : item.Special.StartDate is { } specialStart
                && specialStart <= comingSoonCutoff
                && (item.Special.EndDate ?? specialStart) >= today;
    }

    private static bool IsItemLifetimeActiveToday(MenuItemRecord item, DateOnly today) =>
        (item.OfferStartsOn is null || item.OfferStartsOn <= today)
        && (item.OfferEndsOn is null || item.OfferEndsOn >= today);
}
