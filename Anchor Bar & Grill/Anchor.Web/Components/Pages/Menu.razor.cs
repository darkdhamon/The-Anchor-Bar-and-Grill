using Anchor.Domain.Menu;
using Microsoft.AspNetCore.Components;

namespace Anchor.Web.Components.Pages;

public partial class Menu
{
    private readonly DateOnly today = DateOnly.FromDateTime(DateTime.Today);
    private PublicMenuView? menuView;
    private IReadOnlyDictionary<MenuTab, MenuHoursCardView> tabHoursCards = new Dictionary<MenuTab, MenuHoursCardView>();

    [Inject]
    private IMenuQueryService MenuQueryService { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "tab")]
    private string? RequestedTab { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        menuView = await MenuQueryService.GetPublicMenuAsync(ParseTab(RequestedTab), today);
        tabHoursCards = menuView.Tabs.ToDictionary(
            tab => tab.Tab,
            tab => MenuHoursPresentation.Create(tab.ServiceHours));
    }

    private static MenuTab ParseTab(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "breakfast" => MenuTab.Breakfast,
            "dinner" => MenuTab.Dinner,
            "drinks" => MenuTab.Drinks,
            _ => MenuTab.Lunch
        };

    private static string GetTabHref(string queryValue) => $"/menu?tab={Uri.EscapeDataString(queryValue)}";

    private MenuHoursCardView GetTabHoursCard(MenuTab tab) =>
        tabHoursCards.TryGetValue(tab, out var card)
            ? card
            : new MenuHoursCardView("Not served", Array.Empty<MenuHoursDisplayRow>());

    private static string GetBadgeClass(string label) =>
        label switch
        {
            "Coming Soon" => "status-pill--coming-soon",
            "Seasonal" => "status-pill--seasonal",
            "Today" => "status-pill--today",
            _ => "status-pill--limited"
        };

    private static string GetSelectedTabLabel(MenuTab tab) =>
        tab switch
        {
            MenuTab.Breakfast => "Breakfast",
            MenuTab.Lunch => "Lunch",
            MenuTab.Dinner => "Dinner",
            MenuTab.Drinks => "Drinks",
            _ => "Lunch"
        };

    private static string GetEmptyStateTitle(MenuTab tab) =>
        tab switch
        {
            MenuTab.Breakfast => "Breakfast service is set up and ready for staff content.",
            MenuTab.Drinks => "Drink hours are live even though the beverage list is still being filled in.",
            MenuTab.Dinner => "Dinner content is coming soon.",
            _ => "This menu tab is coming soon."
        };

    private static string GetEmptyStateDescription(MenuTab tab) =>
        tab switch
        {
            MenuTab.Breakfast => "Guests can already see the breakfast service window for each day. Once staff add visible breakfast items, they will appear here automatically.",
            MenuTab.Drinks => "The drinks tab already carries its own weekly service hours, including late-night rows that can run past midnight. Staff can add beverages from the Menu Editor when they are ready.",
            MenuTab.Dinner => "Dinner hours are available, but there are no visible sections or specials assigned yet.",
            _ => "This tab has been created, but there is no visible guest content assigned to it yet."
        };

    private static string GetPriceSummary(PublicMenuItemView item) =>
        item.PriceVariants.Count == 1
            ? item.PriceVariants[0].PriceDisplay
            : string.Join(" / ", item.PriceVariants.Select(variant => variant.PriceDisplay));
}
