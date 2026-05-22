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

        var items = await dbContext.MenuItems
            .AsNoTracking()
            .Where(item => item.IsVisibleToGuests && !item.IsArchived)
            .Where(item => item.SectionAssignments.Any(assignment =>
                assignment.Section.Family == family
                && assignment.Section.IsVisibleToGuests
                && !assignment.Section.IsArchived
                && assignment.Section.MenuTabs.Any(link => link.Tab == tab)))
            .Where(item => item.UsesSectionVisibility || item.MenuTabs.Any(link => link.Tab == tab))
            .Where(item => item.Special == null
                ? (item.OfferEndsOn == null || item.OfferEndsOn >= today)
                    && (item.OfferStartsOn == null || item.OfferStartsOn <= comingSoonCutoff)
                : item.Special.ScheduleKind == MenuItemSpecialScheduleKind.WeeklyRecurring
                    ? item.Special.StartDate <= today && (item.Special.EndDate == null || item.Special.EndDate >= today)
                    : item.Special.StartDate <= comingSoonCutoff && (item.Special.EndDate ?? item.Special.StartDate) >= today)
            .Select(ItemProjection)
            .ToListAsync(cancellationToken);

        var visibleSectionIds = items
            .SelectMany(item => item.SectionAssignments)
            .Select(assignment => assignment.SectionId)
            .Distinct()
            .ToArray();

        var sections = await dbContext.MenuSections
            .AsNoTracking()
            .Where(section => visibleSectionIds.Contains(section.MenuSectionId))
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Name)
            .Select(section => new MenuSectionRecord(
                section.MenuSectionId,
                section.Name,
                section.Callout,
                section.Family,
                section.MenuTabs
                    .OrderBy(link => link.Tab)
                    .Select(link => link.Tab)
                    .ToList(),
                section.SortOrder,
                section.IsVisibleToGuests,
                section.IsArchived))
            .ToListAsync(cancellationToken);

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

        return new PublicMenuSnapshot(tab, sections, items, windows);
    }

    public async Task<IReadOnlyList<MenuItemRecord>> GetHomeSpecialItemsAsync(
        DateOnly today,
        DateOnly comingSoonCutoff,
        CancellationToken cancellationToken = default) =>
        await dbContext.MenuItems
            .AsNoTracking()
            .Where(item => item.Special != null)
            .Where(item => item.IsVisibleToGuests && !item.IsArchived)
            .Where(item => item.SectionAssignments.Any(assignment =>
                assignment.Section.IsVisibleToGuests
                && !assignment.Section.IsArchived))
            .Where(item => item.Special!.ScheduleKind == MenuItemSpecialScheduleKind.WeeklyRecurring
                ? item.Special.StartDate <= today && (item.Special.EndDate == null || item.Special.EndDate >= today)
                : item.Special.StartDate <= comingSoonCutoff && (item.Special.EndDate ?? item.Special.StartDate) >= today)
            .Select(ItemProjection)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<MenuTab>> GetTabsWithVisibleContentAsync(DateOnly today, DateOnly comingSoonCutoff, CancellationToken cancellationToken = default)
    {
        List<MenuTab> tabs = [];
        foreach (var tab in Enum.GetValues<MenuTab>())
        {
            var family = tab == MenuTab.Drinks ? MenuFamily.Drink : MenuFamily.Food;

            var hasItems = await dbContext.MenuItems
                .AsNoTracking()
                .Where(item => item.IsVisibleToGuests && !item.IsArchived)
                .Where(item => item.SectionAssignments.Any(assignment =>
                    assignment.Section.Family == family
                    && assignment.Section.IsVisibleToGuests
                    && !assignment.Section.IsArchived
                    && assignment.Section.MenuTabs.Any(link => link.Tab == tab)))
                .Where(item => item.UsesSectionVisibility || item.MenuTabs.Any(link => link.Tab == tab))
                .AnyAsync(item => item.Special == null
                    ? (item.OfferEndsOn == null || item.OfferEndsOn >= today)
                        && (item.OfferStartsOn == null || item.OfferStartsOn <= comingSoonCutoff)
                    : item.Special.ScheduleKind == MenuItemSpecialScheduleKind.WeeklyRecurring
                        ? item.Special.StartDate <= today && (item.Special.EndDate == null || item.Special.EndDate >= today)
                        : item.Special.StartDate <= comingSoonCutoff && (item.Special.EndDate ?? item.Special.StartDate) >= today, cancellationToken);

            if (hasItems)
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
            .Select(section => new MenuSectionRecord(
                section.MenuSectionId,
                section.Name,
                section.Callout,
                section.Family,
                section.MenuTabs
                    .OrderBy(link => link.Tab)
                    .Select(link => link.Tab)
                    .ToList(),
                section.SortOrder,
                section.IsVisibleToGuests,
                section.IsArchived))
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
                    item.Special.DayOfWeek,
                    item.Special.StartDate,
                    item.Special.EndDate,
                    item.Special.StartsAt,
                    item.Special.EndsAt,
                    item.Special.ClosesNextDay,
                    item.Special.Callout));
}
