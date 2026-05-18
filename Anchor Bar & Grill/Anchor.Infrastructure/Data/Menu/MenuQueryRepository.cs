using Anchor.Domain.Menu;
using Microsoft.EntityFrameworkCore;

namespace Anchor.Infrastructure.Data.Menu;

public sealed class MenuQueryRepository(ApplicationDbContext dbContext) : IMenuQueryRepository
{
    public async Task<PublicMenuSnapshot> GetPublicMenuSnapshotAsync(
        MenuTab tab,
        DateOnly today,
        DateOnly comingSoonCutoff,
        CancellationToken cancellationToken = default)
    {
        var family = tab == MenuTab.Drinks ? MenuFamily.Drink : MenuFamily.Food;

        var items = await dbContext.MenuItems
            .AsNoTracking()
            .Where(item => item.Section.Family == family)
            .Where(item => item.Section.IsVisibleToGuests && !item.Section.IsArchived)
            .Where(item => item.IsVisibleToGuests && !item.IsArchived)
            .Where(item => item.OfferEndsOn == null || item.OfferEndsOn >= today)
            .Where(item => item.OfferStartsOn == null || item.OfferStartsOn <= comingSoonCutoff)
            .Where(item => tab == MenuTab.Drinks
                ? item.Section.Family == MenuFamily.Drink
                : item.FoodTabs.Any(link => link.Tab == tab))
            .Select(item => new MenuItemRecord(
                item.MenuItemId,
                item.MenuSectionId,
                item.Section.Name,
                item.Section.Family,
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
                item.FoodTabs
                    .OrderBy(link => link.Tab)
                    .Select(link => link.Tab)
                    .ToList()))
            .ToListAsync(cancellationToken);

        var specials = await dbContext.RecurringSpecials
            .AsNoTracking()
            .Where(special => special.Tab == tab)
            .Where(special => (special.LinkedMenuItemId.HasValue
                ? special.LinkedMenuItem!.Section.Family
                : special.Section.Family) == family)
            .Where(special => special.LinkedMenuItemId == null || !special.LinkedMenuItem!.IsArchived)
            .Where(special => special.LinkedMenuItemId.HasValue
                ? special.LinkedMenuItem!.Section.IsVisibleToGuests && !special.LinkedMenuItem.Section.IsArchived
                : special.Section.IsVisibleToGuests && !special.Section.IsArchived)
            .Where(special => special.IsVisibleToGuests && !special.IsArchived)
            .Select(special => new MenuRecurringSpecialRecord(
                special.RecurringSpecialId,
                special.Tab,
                special.LinkedMenuItemId.HasValue ? special.LinkedMenuItem!.MenuSectionId : special.MenuSectionId,
                special.LinkedMenuItemId.HasValue ? special.LinkedMenuItem!.Section.Name : special.Section.Name,
                special.DayOfWeek,
                special.Title,
                special.Description,
                special.TimeNote,
                special.PriceNote,
                special.LinkedMenuItemId,
                special.LinkedMenuItem != null ? special.LinkedMenuItem.Name : null,
                special.SortOrder,
                special.IsVisibleToGuests,
                special.IsArchived))
            .ToListAsync(cancellationToken);

        var visibleSectionIds = items.Select(item => item.SectionId)
            .Concat(specials.Select(special => special.SectionId))
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
                section.Family,
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

        return new PublicMenuSnapshot(tab, sections, items, specials, windows);
    }

    public async Task<IReadOnlyList<MenuRecurringSpecialRecord>> GetHomeRecurringSpecialsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.RecurringSpecials
            .AsNoTracking()
            .Where(special => special.IsVisibleToGuests && !special.IsArchived)
            .Where(special => special.LinkedMenuItemId == null || !special.LinkedMenuItem!.IsArchived)
            .Where(special => special.LinkedMenuItemId.HasValue
                ? special.LinkedMenuItem!.Section.IsVisibleToGuests && !special.LinkedMenuItem.Section.IsArchived
                : special.Section.IsVisibleToGuests && !special.Section.IsArchived)
            .OrderBy(special => special.DayOfWeek)
            .ThenBy(special => special.SortOrder)
            .Select(special => new MenuRecurringSpecialRecord(
                special.RecurringSpecialId,
                special.Tab,
                special.LinkedMenuItemId.HasValue ? special.LinkedMenuItem!.MenuSectionId : special.MenuSectionId,
                special.LinkedMenuItemId.HasValue ? special.LinkedMenuItem!.Section.Name : special.Section.Name,
                special.DayOfWeek,
                special.Title,
                special.Description,
                special.TimeNote,
                special.PriceNote,
                special.LinkedMenuItemId,
                special.LinkedMenuItem != null ? special.LinkedMenuItem.Name : null,
                special.SortOrder,
                special.IsVisibleToGuests,
                special.IsArchived))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<MenuTab>> GetTabsWithVisibleContentAsync(DateOnly today, DateOnly comingSoonCutoff, CancellationToken cancellationToken = default)
    {
        List<MenuTab> tabs = [];
        foreach (var tab in Enum.GetValues<MenuTab>())
        {
            var family = tab == MenuTab.Drinks ? MenuFamily.Drink : MenuFamily.Food;

            var hasItems = await dbContext.MenuItems
                .AsNoTracking()
                .Where(item => item.Section.Family == family)
                .Where(item => item.Section.IsVisibleToGuests && !item.Section.IsArchived)
                .Where(item => item.IsVisibleToGuests && !item.IsArchived)
                .Where(item => item.OfferEndsOn == null || item.OfferEndsOn >= today)
                .Where(item => item.OfferStartsOn == null || item.OfferStartsOn <= comingSoonCutoff)
                .AnyAsync(item => tab == MenuTab.Drinks
                    ? item.Section.Family == MenuFamily.Drink
                    : item.FoodTabs.Any(link => link.Tab == tab), cancellationToken);

            var hasSpecials = await dbContext.RecurringSpecials
                .AsNoTracking()
                .Where(special => special.Tab == tab)
                .Where(special => special.LinkedMenuItemId == null || !special.LinkedMenuItem!.IsArchived)
                .Where(special => special.LinkedMenuItemId.HasValue
                    ? special.LinkedMenuItem!.Section.IsVisibleToGuests && !special.LinkedMenuItem.Section.IsArchived
                    : special.Section.IsVisibleToGuests && !special.Section.IsArchived)
                .AnyAsync(special => special.IsVisibleToGuests && !special.IsArchived, cancellationToken);

            if (hasItems || hasSpecials)
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
                section.Family,
                section.SortOrder,
                section.IsVisibleToGuests,
                section.IsArchived))
            .ToListAsync(cancellationToken);

        var items = await dbContext.MenuItems
            .AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name)
            .Select(item => new MenuItemRecord(
                item.MenuItemId,
                item.MenuSectionId,
                item.Section.Name,
                item.Section.Family,
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
                item.FoodTabs
                    .OrderBy(link => link.Tab)
                    .Select(link => link.Tab)
                    .ToList()))
            .ToListAsync(cancellationToken);

        var specials = await dbContext.RecurringSpecials
            .AsNoTracking()
            .OrderBy(special => special.DayOfWeek)
            .ThenBy(special => special.SortOrder)
            .Select(special => new MenuRecurringSpecialRecord(
                special.RecurringSpecialId,
                special.Tab,
                special.LinkedMenuItemId.HasValue ? special.LinkedMenuItem!.MenuSectionId : special.MenuSectionId,
                special.LinkedMenuItemId.HasValue ? special.LinkedMenuItem!.Section.Name : special.Section.Name,
                special.DayOfWeek,
                special.Title,
                special.Description,
                special.TimeNote,
                special.PriceNote,
                special.LinkedMenuItemId,
                special.LinkedMenuItem != null ? special.LinkedMenuItem.Name : null,
                special.SortOrder,
                special.IsVisibleToGuests,
                special.IsArchived))
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

        return new MenuManagementSnapshot(sections, items, specials, windows);
    }
}
