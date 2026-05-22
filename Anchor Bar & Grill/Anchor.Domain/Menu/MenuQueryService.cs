namespace Anchor.Domain.Menu;

public sealed class MenuQueryService(IMenuQueryRepository repository) : IMenuQueryService
{
    private static readonly Guid SpecialsSectionId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public async Task<MenuTab> GetSuggestedPublicTabAsync(DateOnly today, TimeOnly currentTime, CancellationToken cancellationToken = default) =>
        PublicMenuTabSelectionRules.GetSuggestedTab(
            await repository.GetPublicServiceWindowsAsync(cancellationToken),
            today,
            currentTime);

    public async Task<PublicMenuView> GetPublicMenuAsync(MenuTab requestedTab, DateOnly today, CancellationToken cancellationToken = default)
    {
        var comingSoonCutoff = today.AddDays(30);
        var snapshot = await repository.GetPublicMenuSnapshotAsync(requestedTab, today, comingSoonCutoff, cancellationToken);
        var tabsWithContent = await repository.GetTabsWithVisibleContentAsync(today, comingSoonCutoff, cancellationToken);

        var visibleSections = snapshot.Sections
            .Where(section => section.MenuTabs.Contains(requestedTab))
            .ToArray();
        var visibleSectionIds = visibleSections
            .Select(section => section.SectionId)
            .ToHashSet();

        var visibleItems = snapshot.Items
            .Where(item => item.SectionAssignments.Any(assignment => visibleSectionIds.Contains(assignment.SectionId)))
            .ToArray();

        var publicItems = visibleItems
            .Select(item => new PublicMenuItemView(
                item.ItemId,
                item.Name,
                item.Description,
                item.ImagePath,
                item.PriceVariants
                    .OrderBy(variant => variant.SortOrder)
                    .ThenBy(variant => variant.Label, StringComparer.OrdinalIgnoreCase)
                    .Select(variant => new MenuItemPriceVariantView(variant.PriceVariantId, variant.Label, variant.Amount, variant.SortOrder))
                    .ToArray(),
                MenuPresentationRules.GetPublicStatusLabels(item, today),
                MenuPresentationRules.FormatOfferDateSummary(item, today),
                item.Special is null
                    ? null
                    : new MenuItemSpecialPublicView(
                        item.Special.ScheduleKind,
                        MenuPresentationRules.GetSpecialBadgeLabel(item.Special),
                        MenuPresentationRules.FormatSpecialScheduleSummary(item.Special),
                        MenuPresentationRules.FormatSpecialTimeSummary(item.Special),
                        item.Special.Callout,
                        MenuPresentationRules.IsSpecialToday(item.Special, today))))
            .ToDictionary(item => item.ItemId);

        var itemsBySection = visibleItems
            .SelectMany(item => item.SectionAssignments
                .Where(assignment => visibleSectionIds.Contains(assignment.SectionId))
                .Select(assignment => new { assignment.SectionId, Item = item }))
            .GroupBy(entry => entry.SectionId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<PublicMenuItemView>)group
                    .OrderByDescending(entry => entry.Item.Special is not null)
                    .ThenBy(entry => entry.Item.SectionAssignments.Single(assignment => assignment.SectionId == group.Key).SortOrder)
                    .ThenBy(entry => entry.Item.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(entry => publicItems[entry.Item.ItemId])
                    .ToArray());

        var standardSections = visibleSections
            .Where(section => visibleSectionIds.Contains(section.SectionId))
            .OrderBy(section => section.SortOrder)
            .ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase)
            .Select((section, index) => new PublicMenuSectionView(
                section.SectionId,
                section.Name,
                section.Callout,
                GetAccentClass(index),
                itemsBySection.GetValueOrDefault(section.SectionId, Array.Empty<PublicMenuItemView>())))
            .ToArray();

        var specialsItems = visibleItems
            .Where(item => item.Special is not null)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => publicItems[item.ItemId])
            .ToArray();

        List<PublicMenuSectionView> sections = [];
        if (specialsItems.Length > 0)
        {
            sections.Add(new PublicMenuSectionView(
                SpecialsSectionId,
                "Specials",
                null,
                "accent-gold",
                specialsItems));
        }

        sections.AddRange(standardSections);

        var tabs = Enum.GetValues<MenuTab>()
            .Select(tab => new MenuTabLinkView(
                tab,
                MenuPresentationRules.GetTabLabel(tab),
                MenuPresentationRules.GetTabQueryValue(tab),
                tab == requestedTab,
                tabsWithContent.Contains(tab)))
            .ToArray();

        var serviceHours = BuildServiceHours(snapshot.ServiceWindows, today);

        return new PublicMenuView(snapshot.Tab, tabs, serviceHours, sections);
    }

    public async Task<IReadOnlyList<PublicHomeSpecialView>> GetHomeSpecialsAsync(DateOnly today, CancellationToken cancellationToken = default) =>
        (await repository.GetHomeSpecialItemsAsync(today, today.AddDays(30), cancellationToken))
        .Where(item => item.Special is not null)
        .OrderBy(item => item.Special!.ScheduleKind == MenuItemSpecialScheduleKind.WeeklyRecurring
            ? Array.IndexOf(MenuPresentationRules.DayOrder.ToArray(), item.Special.DayOfWeek ?? DayOfWeek.Monday)
            : 7)
        .ThenBy(item => item.Special!.StartDate)
        .ThenBy(item => item.SortOrder)
        .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
        .Select(item => new PublicHomeSpecialView(
            item.ItemId,
            MenuPresentationRules.GetSpecialBadgeLabel(item.Special!),
            item.Name,
            item.Description,
            MenuPresentationRules.FormatSpecialTimeSummary(item.Special!),
            item.Special!.Callout,
            MenuPresentationRules.GetPlacementSummary(item),
            MenuPresentationRules.IsSpecialToday(item.Special!, today)))
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
                section.Callout,
                section.Family,
                section.MenuTabs,
                section.SortOrder,
                section.IsVisibleToGuests,
                section.IsArchived,
                MenuPresentationRules.GetAdminStatusLabels(section)))
            .ToArray();

        var items = snapshot.Items
            .OrderBy(item => item.Family)
            .ThenBy(
                item => item.SectionAssignments
                    .Select(assignment => assignment.SectionName)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(item => item.Special is not null)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => new MenuItemAdminView(
                item.ItemId,
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
                item.SectionAssignments
                    .OrderBy(assignment => assignment.SectionName, StringComparer.OrdinalIgnoreCase)
                    .Select(assignment => new MenuItemSectionAssignmentView(
                        assignment.SectionId,
                        assignment.SectionName,
                        assignment.SortOrder))
                    .ToArray(),
                item.UsesSectionVisibility,
                item.MenuTabs.OrderBy(tab => tab).ToArray(),
                item.PriceVariants
                    .OrderBy(variant => variant.SortOrder)
                    .ThenBy(variant => variant.Label, StringComparer.OrdinalIgnoreCase)
                    .Select(variant => new MenuItemPriceVariantView(variant.PriceVariantId, variant.Label, variant.Amount, variant.SortOrder))
                    .ToArray(),
                MenuPresentationRules.GetAdminStatusLabels(item, today),
                MenuPresentationRules.FormatOfferDateSummary(item, today),
                item.Special is null
                    ? null
                    : new MenuItemSpecialAdminView(
                        item.Special.ScheduleKind,
                        item.Special.DayOfWeek,
                        item.Special.StartDate,
                        item.Special.EndDate,
                        item.Special.StartsAt,
                        item.Special.EndsAt,
                        item.Special.ClosesNextDay,
                        MenuPresentationRules.GetSpecialBadgeLabel(item.Special),
                        MenuPresentationRules.FormatSpecialScheduleSummary(item.Special),
                        MenuPresentationRules.FormatSpecialTimeSummary(item.Special),
                        item.Special.Callout,
                        MenuPresentationRules.GetAdminStatusLabels(item.Special, today),
                        MenuPresentationRules.IsSpecialToday(item.Special, today))))
            .ToArray();

        return new MenuManagementView(tabs, sections, items);
    }

    private static string GetAccentClass(int index) =>
        (index % 4) switch
        {
            0 => "accent-blue",
            1 => "accent-green",
            2 => "accent-gold",
            _ => "accent-magenta"
        };

    private static IReadOnlyList<MenuServiceWindowView> BuildServiceHours(IEnumerable<MenuServiceWindowRecord> windows, DateOnly today) =>
        windows
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
}
