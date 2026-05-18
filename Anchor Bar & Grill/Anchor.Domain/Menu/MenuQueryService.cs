namespace Anchor.Domain.Menu;

public sealed class MenuQueryService(IMenuQueryRepository repository) : IMenuQueryService
{
    public async Task<PublicMenuView> GetPublicMenuAsync(MenuTab requestedTab, DateOnly today, CancellationToken cancellationToken = default)
    {
        var comingSoonCutoff = today.AddDays(30);
        var snapshot = await repository.GetPublicMenuSnapshotAsync(requestedTab, today, comingSoonCutoff, cancellationToken);
        var tabsWithContent = await repository.GetTabsWithVisibleContentAsync(today, comingSoonCutoff, cancellationToken);

        var specialsBySection = snapshot.Specials
            .GroupBy(special => special.SectionId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<PublicRecurringSpecialView>)group
                    .OrderBy(special => special.SortOrder)
                    .ThenBy(special => special.Title, StringComparer.OrdinalIgnoreCase)
                    .Select(special => new PublicRecurringSpecialView(
                        special.SpecialId,
                        MenuPresentationRules.GetDayLabel(special.DayOfWeek),
                        special.Title,
                        special.Description,
                        special.TimeNote,
                        special.PriceNote,
                        MenuPresentationRules.GetPlacementSummary(special),
                        special.DayOfWeek == today.DayOfWeek))
                    .ToArray());

        var itemsBySection = snapshot.Items
            .GroupBy(item => item.SectionId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<PublicMenuItemView>)group
                    .OrderBy(item => item.SortOrder)
                    .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(item => new PublicMenuItemView(
                        item.ItemId,
                        item.Name,
                        item.Description,
                        item.ImagePath,
                        item.PriceVariants
                            .OrderBy(variant => variant.SortOrder)
                            .ThenBy(variant => variant.Label, StringComparer.OrdinalIgnoreCase)
                            .Select(variant => new MenuItemPriceVariantView(variant.Label, variant.Amount, variant.SortOrder))
                            .ToArray(),
                        MenuPresentationRules.GetPublicStatusLabels(item, today),
                        MenuPresentationRules.FormatOfferDateSummary(item, today)))
                    .ToArray());

        var visibleSectionIds = new HashSet<Guid>(specialsBySection.Keys);
        visibleSectionIds.UnionWith(itemsBySection.Keys);

        var sections = snapshot.Sections
            .Where(section => visibleSectionIds.Contains(section.SectionId))
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase)
            .Select((section, index) => new PublicMenuSectionView(
                section.SectionId,
                section.Name,
                GetAccentClass(index),
                specialsBySection.GetValueOrDefault(section.SectionId, Array.Empty<PublicRecurringSpecialView>()),
                itemsBySection.GetValueOrDefault(section.SectionId, Array.Empty<PublicMenuItemView>())))
            .ToArray();

        var tabs = Enum.GetValues<MenuTab>()
            .Select(tab => new MenuTabLinkView(
                tab,
                MenuPresentationRules.GetTabLabel(tab),
                MenuPresentationRules.GetTabQueryValue(tab),
                tab == requestedTab,
                tabsWithContent.Contains(tab)))
            .ToArray();

        var serviceHours = snapshot.ServiceWindows
            .OrderBy(window => Array.IndexOf(MenuPresentationRules.DayOrder.ToArray(), window.DayOfWeek))
            .Select(window => new MenuServiceWindowView(
                window.DayOfWeek,
                MenuPresentationRules.GetDayLabel(window.DayOfWeek),
                window.IsAvailable,
                MenuPresentationRules.FormatServiceWindow(window),
                window.DayOfWeek == today.DayOfWeek,
                window.OpensAt,
                window.ClosesAt,
                window.ClosesNextDay))
            .ToArray();

        return new PublicMenuView(snapshot.Tab, tabs, serviceHours, sections);
    }

    public async Task<IReadOnlyList<PublicRecurringSpecialView>> GetHomeRecurringSpecialsAsync(DateOnly today, CancellationToken cancellationToken = default) =>
        (await repository.GetHomeRecurringSpecialsAsync(cancellationToken))
        .OrderBy(special => Array.IndexOf(MenuPresentationRules.DayOrder.ToArray(), special.DayOfWeek))
        .ThenBy(special => special.SortOrder)
        .Select(special => new PublicRecurringSpecialView(
            special.SpecialId,
            MenuPresentationRules.GetDayLabel(special.DayOfWeek),
            special.Title,
            special.Description,
            special.TimeNote,
            special.PriceNote,
            MenuPresentationRules.GetPlacementSummary(special),
            special.DayOfWeek == today.DayOfWeek))
        .ToArray();

    public async Task<MenuManagementView> GetMenuManagementViewAsync(DateOnly today, CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.GetMenuManagementSnapshotAsync(cancellationToken);

        var tabs = snapshot.ServiceWindows
            .GroupBy(window => window.Tab)
            .OrderBy(group => group.Key)
            .Select(group => new MenuTabHoursAdminView(
                group.Key,
                MenuPresentationRules.GetTabLabel(group.Key),
                group.OrderBy(window => Array.IndexOf(MenuPresentationRules.DayOrder.ToArray(), window.DayOfWeek))
                    .Select(window => new MenuServiceWindowView(
                        window.DayOfWeek,
                        MenuPresentationRules.GetDayLabel(window.DayOfWeek),
                        window.IsAvailable,
                        MenuPresentationRules.FormatServiceWindow(window),
                        window.DayOfWeek == today.DayOfWeek,
                        window.OpensAt,
                        window.ClosesAt,
                        window.ClosesNextDay))
                    .ToArray()))
            .ToArray();

        var sections = snapshot.Sections
            .OrderBy(section => section.Family)
            .ThenBy(section => section.SortOrder)
            .ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase)
            .Select(section => new MenuSectionAdminView(
                section.SectionId,
                section.Name,
                section.Family,
                section.SortOrder,
                section.IsVisibleToGuests,
                section.IsArchived,
                MenuPresentationRules.GetAdminStatusLabels(section)))
            .ToArray();

        var items = snapshot.Items
            .OrderBy(item => item.Family)
            .ThenBy(item => item.SectionName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => new MenuItemAdminView(
                item.ItemId,
                item.SectionId,
                item.SectionName,
                item.Family,
                item.Name,
                item.Description,
                item.ImagePath,
                item.SortOrder,
                item.IsVisibleToGuests,
                item.IsArchived,
                item.OfferStartsOn,
                item.OfferEndsOn,
                item.IsSeasonal,
                item.FoodTabs.OrderBy(tab => tab).ToArray(),
                item.PriceVariants
                    .OrderBy(variant => variant.SortOrder)
                    .ThenBy(variant => variant.Label, StringComparer.OrdinalIgnoreCase)
                    .Select(variant => new MenuItemPriceVariantView(variant.Label, variant.Amount, variant.SortOrder))
                    .ToArray(),
                MenuPresentationRules.GetAdminStatusLabels(item, today),
                MenuPresentationRules.FormatOfferDateSummary(item, today)))
            .ToArray();

        var specials = snapshot.Specials
            .OrderBy(special => special.Tab)
            .ThenBy(special => Array.IndexOf(MenuPresentationRules.DayOrder.ToArray(), special.DayOfWeek))
            .ThenBy(special => special.SortOrder)
            .ThenBy(special => special.Title, StringComparer.OrdinalIgnoreCase)
            .Select(special => new RecurringSpecialAdminView(
                special.SpecialId,
                special.Tab,
                special.SectionId,
                special.SectionName,
                special.DayOfWeek,
                MenuPresentationRules.GetDayLabel(special.DayOfWeek),
                special.Title,
                special.Description,
                special.TimeNote,
                special.PriceNote,
                special.LinkedMenuItemId,
                special.LinkedMenuItemName,
                special.SortOrder,
                special.IsVisibleToGuests,
                special.IsArchived,
                MenuPresentationRules.GetAdminStatusLabels(special, today),
                special.DayOfWeek == today.DayOfWeek))
            .ToArray();

        return new MenuManagementView(tabs, sections, items, specials);
    }

    private static string GetAccentClass(int index) =>
        (index % 4) switch
        {
            0 => "accent-blue",
            1 => "accent-green",
            2 => "accent-gold",
            _ => "accent-magenta"
        };
}
