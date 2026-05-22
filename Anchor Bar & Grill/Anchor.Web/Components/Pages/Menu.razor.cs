using Anchor.Domain.Menu;
using Microsoft.AspNetCore.Components;

namespace Anchor.Web.Components.Pages;

public partial class Menu
{
    private PublicMenuView? menuView;
    private MenuHoursCardView menuHoursCard = new("Not served", Array.Empty<MenuHoursDisplayRow>());

    [Inject]
    private IMenuQueryService MenuQueryService { get; set; } = null!;

    [Inject]
    private TimeProvider TimeProvider { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "tab")]
    private string? RequestedTab { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        var now = TimeProvider.GetLocalNow();
        var today = DateOnly.FromDateTime(now.DateTime);
        var currentTime = TimeOnly.FromDateTime(now.DateTime);
        var selectedTab = TryParseRequestedTab(RequestedTab, out var requestedTab)
            ? requestedTab
            : await MenuQueryService.GetSuggestedPublicTabAsync(today, currentTime);

        menuView = await MenuQueryService.GetPublicMenuAsync(selectedTab, today);
        menuHoursCard = MenuHoursPresentation.Create(menuView.ServiceHours);
    }

    private static bool TryParseRequestedTab(string? value, out MenuTab tab)
    {
        tab = MenuTab.Lunch;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "breakfast" => Assign(MenuTab.Breakfast, out tab),
            "lunch" => Assign(MenuTab.Lunch, out tab),
            "dinner" => Assign(MenuTab.Dinner, out tab),
            "drinks" => Assign(MenuTab.Drinks, out tab),
            _ => false
        };
    }

    private static string GetTabHref(string queryValue) => $"/menu?tab={Uri.EscapeDataString(queryValue)}";

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

    private static bool Assign(MenuTab value, out MenuTab target)
    {
        target = value;
        return true;
    }
}
