using System.Security.Claims;
using Anchor.Domain.Identity;
using Anchor.Domain.Menu;
using Anchor.Web.Components.Pages.Admin;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Web.Tests.Components.Pages.Admin;

public sealed class MenuAdminRedesignTests : BunitContext
{
    private readonly TestAuthenticationStateProvider authStateProvider;

    public MenuAdminRedesignTests()
    {
        var timeComboBoxModule = JSInterop.SetupModule("./Components/Shared/InputTimeComboBox.razor.js");
        timeComboBoxModule.SetupVoid("scrollRelevantOption", _ => true);

        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        Services.AddLogging();
        Services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(ApplicationPolicies.AdminAccess, policy => policy.RequireRole(ApplicationRoles.Admin));
            options.AddPolicy(ApplicationPolicies.EventManagement, policy => policy.RequireRole(ApplicationRoles.EventManager));
            options.AddPolicy(ApplicationPolicies.MenuManagement, policy => policy.RequireRole(ApplicationRoles.MenuManager));
            options.AddPolicy(ApplicationPolicies.ITAccess, policy => policy.RequireRole(ApplicationRoles.It));
        });
        Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();

        authStateProvider = new TestAuthenticationStateProvider();
        Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
        Services.AddCascadingAuthenticationState();

        Services.AddSingleton<IMenuQueryService>(new StaticMenuAdminQueryService());
        Services.AddSingleton<IMenuManagementService>(new StaticMenuAdminManagementService());
    }

    [Fact]
    public void Defaults_to_food_workspace_and_renders_top_level_tabs()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu");

        var selectedTab = cut.FindAll(".menu-editor-tabs__button.is-selected").Single();

        Assert.Equal("Food", selectedTab.TextContent.Trim());
        Assert.Contains("Food workspace", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Food browser", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hours", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Drinks", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Food_query_string_selects_requested_meal_filter()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu?tab=food&food=breakfast");

        var selectedChip = cut.FindAll(".menu-editor-filter-chip.is-selected")
            .Single(button => string.Equals(button.TextContent.Trim(), "Breakfast", StringComparison.Ordinal));

        Assert.Equal("Breakfast", selectedChip.TextContent.Trim());
        Assert.Contains("Breakfast Burrito", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Late Night Burger", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Specials_content_filter_shows_special_items()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu");

        cut.FindAll(".menu-editor-filter-chip")
            .Single(button => string.Equals(button.TextContent.Trim(), "Specials", StringComparison.Ordinal))
            .Click();

        Assert.Contains("Late Night Burger", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Secret Nachos", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Breakfast Burrito", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Both_filter_shows_archived_and_hidden_rows_with_different_states()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu");

        cut.FindAll(".menu-editor-segmented__button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Both", StringComparison.Ordinal))
            .Click();

        var archivedRow = cut.FindAll(".menu-editor-tree__row")
            .Single(row => row.TextContent.Contains("Retired Nachos", StringComparison.OrdinalIgnoreCase));

        var hiddenRow = cut.FindAll(".menu-editor-tree__row")
            .Single(row => row.TextContent.Contains("Secret Nachos", StringComparison.OrdinalIgnoreCase));

        Assert.Contains("is-archived", archivedRow.ClassName, StringComparison.Ordinal);
        Assert.Contains("is-hidden", hiddenRow.ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void Archived_filter_keeps_active_parent_section_visible_when_archived_child_matches()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu");

        cut.FindAll(".menu-editor-segmented__button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Archived", StringComparison.Ordinal))
            .Click();

        var section = cut.FindAll(".menu-editor-tree__section")
            .Single(row => row.TextContent.Contains("Appetizers", StringComparison.OrdinalIgnoreCase));

        Assert.Contains("is-context-muted", section.ClassName, StringComparison.Ordinal);
        Assert.Contains("Retired Nachos", section.TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Add_item_uses_selected_section_as_context()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu?tab=food&food=breakfast");

        cut.FindAll(".menu-editor-tree__select")
            .First(button => button.TextContent.Contains("Appetizers", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Add item", StringComparison.Ordinal))
            .Click();

        var sectionSelect = cut.Find("select");
        var mealCheckboxes = cut.FindAll(".menu-editor-checkbox-stack input[type='checkbox']");

        Assert.Equal(StaticMenuAdminQueryService.AppetizersSectionId.ToString(), sectionSelect.GetAttribute("value"));
        Assert.True(mealCheckboxes[0].HasAttribute("checked"));
        Assert.False(mealCheckboxes[1].HasAttribute("checked"));
        Assert.False(mealCheckboxes[2].HasAttribute("checked"));
    }

    [Fact]
    public void Item_save_failures_render_inside_the_detail_panel()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));
        Services.AddSingleton<IMenuManagementService>(new FailingMenuAdminManagementService());

        var cut = RenderMenuAdmin("/admin/menu?tab=drinks");

        cut.FindAll(".menu-editor-tree__select")
            .Single(button => button.TextContent.Contains("Cocktails", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Add item", StringComparison.Ordinal))
            .Click();

        cut.Find("input[placeholder='Classic hamburger, wing night, old fashioned...']").Input("Pepsi");
        cut.FindAll(".menu-editor-price-row input")[0].Input("Regular");
        cut.FindAll(".menu-editor-price-row input")[1].Input("3.00");

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Create item", StringComparison.Ordinal))
            .Click();

        var detailAlert = cut.Find(".menu-editor-detail .alert-danger");

        Assert.Contains("Description is optional", detailAlert.TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_item_uses_current_textbox_values_without_needing_blur()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));
        var captureService = new CapturingMenuAdminManagementService();
        Services.AddSingleton<IMenuManagementService>(captureService);

        var cut = RenderMenuAdmin("/admin/menu?tab=drinks");

        cut.FindAll(".menu-editor-tree__select")
            .Single(button => button.TextContent.Contains("Cocktails", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Add item", StringComparison.Ordinal))
            .Click();

        cut.Find("input[placeholder='Classic hamburger, wing night, old fashioned...']").Input("Pepsi");
        cut.FindAll(".menu-editor-price-row input")[0].Input("Regular");
        cut.FindAll(".menu-editor-price-row input")[1].Input("3.00");

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Create item", StringComparison.Ordinal))
            .Click();

        Assert.NotNull(captureService.LastSaveItemRequest);
        Assert.Equal("Pepsi", captureService.LastSaveItemRequest!.Name);
        Assert.Equal(string.Empty, captureService.LastSaveItemRequest.Description);
    }

    [Fact]
    public void Existing_item_accepts_change_style_description_updates()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));
        var captureService = new CapturingMenuAdminManagementService();
        Services.AddSingleton<IMenuManagementService>(captureService);

        var cut = RenderMenuAdmin("/admin/menu?tab=drinks");

        cut.FindAll(".menu-editor-tree__select")
            .Single(button => button.TextContent.Contains("Old Fashioned", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.Find("textarea").Change("Voice typed description");

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Save item", StringComparison.Ordinal))
            .Click();

        Assert.NotNull(captureService.LastSaveItemRequest);
        Assert.Equal("Voice typed description", captureService.LastSaveItemRequest!.Description);
    }

    [Fact]
    public void Thrown_item_save_errors_render_as_inline_status_messages()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));
        Services.AddSingleton<IMenuManagementService>(new ThrowingMenuAdminManagementService());

        var cut = RenderMenuAdmin("/admin/menu?tab=drinks");

        cut.FindAll(".menu-editor-tree__select")
            .Single(button => button.TextContent.Contains("Cocktails", StringComparison.OrdinalIgnoreCase))
            .Click();

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Add item", StringComparison.Ordinal))
            .Click();

        cut.Find("input[placeholder='Classic hamburger, wing night, old fashioned...']").Input("Pepsi");
        cut.FindAll(".menu-editor-price-row input")[0].Input("Regular");
        cut.FindAll(".menu-editor-price-row input")[1].Input("3.00");

        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Create item", StringComparison.Ordinal))
            .Click();

        var detailAlert = cut.Find(".menu-editor-detail .alert-danger");

        Assert.Contains("couldn't save the menu item", detailAlert.TextContent, StringComparison.OrdinalIgnoreCase);
    }

    private IRenderedComponent<ContainerFragment> RenderMenuAdmin(string uri)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(uri);

        return Render(builder =>
        {
            builder.OpenComponent<CascadingAuthenticationState>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<MenuAdmin>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });
    }

    private static ClaimsPrincipal CreateUser(string userName, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.NameIdentifier, userName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));
    }

    private sealed class StaticMenuAdminQueryService : IMenuQueryService
    {
        internal static readonly Guid AppetizersSectionId = Guid.Parse("D8F92296-4F3C-4B88-B2D4-D1775F54A1D1");
        private static readonly Guid BreakfastSectionId = Guid.Parse("8A88226D-F45A-4E15-9420-8E1828654A73");
        private static readonly Guid CocktailsSectionId = Guid.Parse("5E3C8768-2020-4C8A-A565-B2B981AAB1B1");
        private static readonly Guid ActiveSpecialItemId = Guid.Parse("A4CC9DA8-54AE-4FA9-85D1-2E666FCF4B18");
        private static readonly Guid HiddenFoodItemId = Guid.Parse("89CE687D-62E8-453F-8D08-12D74F85FCB9");
        private static readonly Guid ArchivedFoodItemId = Guid.Parse("44AA62BE-4B4D-46C7-A3D3-5088BF3B58DD");
        private static readonly Guid BreakfastItemId = Guid.Parse("797FEE70-BA14-46A5-AB88-DCDA3DAF7262");
        private static readonly Guid DrinkItemId = Guid.Parse("0A5B6B42-778E-49D3-8568-9AB1A785432D");
        private static readonly DateOnly Today = new(2026, 5, 18);

        public Task<MenuTab> GetSuggestedPublicTabAsync(DateOnly today, TimeOnly currentTime, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PublicMenuView> GetPublicMenuAsync(MenuTab requestedTab, DateOnly today, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PublicHomeSpecialView>> GetHomeSpecialsAsync(DateOnly today, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublicHomeSpecialView>>([]);

        public Task<MenuManagementView> GetMenuManagementViewAsync(DateOnly today, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<MenuTabHoursAdminView> tabs = Enum.GetValues<MenuTab>()
                .Select(tab => new MenuTabHoursAdminView(
                    tab,
                    tab.ToString(),
                    new[]
                    {
                        new MenuServiceWindowView(DayOfWeek.Monday, "Monday", true, "11:00 AM - 5:00 PM", today.DayOfWeek == DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(17, 0), false),
                        new MenuServiceWindowView(DayOfWeek.Tuesday, "Tuesday", true, "11:00 AM - 5:00 PM", today.DayOfWeek == DayOfWeek.Tuesday, new TimeOnly(11, 0), new TimeOnly(17, 0), false),
                        new MenuServiceWindowView(DayOfWeek.Wednesday, "Wednesday", true, "11:00 AM - 5:00 PM", today.DayOfWeek == DayOfWeek.Wednesday, new TimeOnly(11, 0), new TimeOnly(17, 0), false),
                        new MenuServiceWindowView(DayOfWeek.Thursday, "Thursday", true, "11:00 AM - 5:00 PM", today.DayOfWeek == DayOfWeek.Thursday, new TimeOnly(11, 0), new TimeOnly(17, 0), false),
                        new MenuServiceWindowView(DayOfWeek.Friday, "Friday", true, "11:00 AM - 5:00 PM", today.DayOfWeek == DayOfWeek.Friday, new TimeOnly(11, 0), new TimeOnly(17, 0), false),
                        new MenuServiceWindowView(DayOfWeek.Saturday, "Saturday", true, "11:00 AM - 5:00 PM", today.DayOfWeek == DayOfWeek.Saturday, new TimeOnly(11, 0), new TimeOnly(17, 0), false),
                        new MenuServiceWindowView(DayOfWeek.Sunday, "Sunday", true, "11:00 AM - 5:00 PM", today.DayOfWeek == DayOfWeek.Sunday, new TimeOnly(11, 0), new TimeOnly(17, 0), false)
                    }))
                .ToArray();

            IReadOnlyList<MenuSectionAdminView> sections =
            [
                new(AppetizersSectionId, "Appetizers", MenuFamily.Food, 1, true, false, []),
                new(BreakfastSectionId, "Breakfast Plates", MenuFamily.Food, 2, true, false, []),
                new(CocktailsSectionId, "Cocktails", MenuFamily.Drink, 1, true, false, [])
            ];

            IReadOnlyList<MenuItemAdminView> items =
            [
                new(
                    ActiveSpecialItemId,
                    AppetizersSectionId,
                    "Appetizers",
                    MenuFamily.Food,
                    "Late Night Burger",
                    "Lunch and dinner burger.",
                    null,
                    1,
                    true,
                    false,
                    null,
                    null,
                    false,
                    new[] { MenuTab.Lunch, MenuTab.Dinner },
                    new[] { new MenuItemPriceVariantView("Regular", 12m, 1) },
                    new[] { "Special", "Today" },
                    null,
                    new MenuItemSpecialAdminView(
                        MenuItemSpecialScheduleKind.WeeklyRecurring,
                        DayOfWeek.Monday,
                        new DateOnly(2026, 1, 1),
                        null,
                        new TimeOnly(17, 0),
                        null,
                        false,
                        "Monday",
                        "Every Monday starting Jan 1",
                        "After 5:00 PM",
                        "$11 basket special",
                        new[] { "Today" },
                        Today.DayOfWeek == DayOfWeek.Monday)),
                new(
                    HiddenFoodItemId,
                    AppetizersSectionId,
                    "Appetizers",
                    MenuFamily.Food,
                    "Secret Nachos",
                    "Hidden test item.",
                    null,
                    2,
                    false,
                    false,
                    null,
                    null,
                    false,
                    new[] { MenuTab.Lunch },
                    new[] { new MenuItemPriceVariantView("Regular", 10m, 1) },
                    new[] { "Hidden" },
                    null,
                    null),
                new(
                    ArchivedFoodItemId,
                    AppetizersSectionId,
                    "Appetizers",
                    MenuFamily.Food,
                    "Retired Nachos",
                    "Archived test item.",
                    null,
                    3,
                    true,
                    true,
                    null,
                    null,
                    false,
                    new[] { MenuTab.Lunch },
                    new[] { new MenuItemPriceVariantView("Regular", 9m, 1) },
                    new[] { "Archived" },
                    null,
                    null),
                new(
                    BreakfastItemId,
                    BreakfastSectionId,
                    "Breakfast Plates",
                    MenuFamily.Food,
                    "Breakfast Burrito",
                    "Breakfast-only item.",
                    null,
                    1,
                    true,
                    false,
                    null,
                    null,
                    false,
                    new[] { MenuTab.Breakfast },
                    new[] { new MenuItemPriceVariantView("Regular", 11m, 1) },
                    [],
                    null,
                    null),
                new(
                    DrinkItemId,
                    CocktailsSectionId,
                    "Cocktails",
                    MenuFamily.Drink,
                    "Old Fashioned",
                    "Drink item.",
                    null,
                    1,
                    true,
                    false,
                    null,
                    null,
                    false,
                    Array.Empty<MenuTab>(),
                    new[] { new MenuItemPriceVariantView("Regular", 12m, 1) },
                    [],
                    null,
                    null)
            ];

            return Task.FromResult(new MenuManagementView(tabs, sections, items));
        }
    }

    private sealed class StaticMenuAdminManagementService : IMenuManagementService
    {
        public Task<MenuOperationResult> SaveSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(request.SectionId ?? Guid.NewGuid()));

        public Task<MenuOperationResult> SaveItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(request.ItemId ?? Guid.NewGuid()));

        public Task<MenuOperationResult> SaveServiceWindowsAsync(SaveMenuServiceWindowRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderSectionsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderItemsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ArchiveSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));

        public Task<MenuOperationResult> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));
    }

    private sealed class FailingMenuAdminManagementService : IMenuManagementService
    {
        public Task<MenuOperationResult> SaveSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(request.SectionId ?? Guid.NewGuid()));

        public Task<MenuOperationResult> SaveItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Failure("Description is optional. This is the inline item-save test failure."));

        public Task<MenuOperationResult> SaveServiceWindowsAsync(SaveMenuServiceWindowRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderSectionsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderItemsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ArchiveSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));

        public Task<MenuOperationResult> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));
    }

    private sealed class CapturingMenuAdminManagementService : IMenuManagementService
    {
        public SaveMenuItemRequest? LastSaveItemRequest { get; private set; }

        public Task<MenuOperationResult> SaveSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(request.SectionId ?? Guid.NewGuid()));

        public Task<MenuOperationResult> SaveItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default)
        {
            LastSaveItemRequest = request;
            return Task.FromResult(MenuOperationResult.Success(request.ItemId ?? Guid.NewGuid()));
        }

        public Task<MenuOperationResult> SaveServiceWindowsAsync(SaveMenuServiceWindowRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderSectionsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderItemsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ArchiveSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));

        public Task<MenuOperationResult> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));
    }

    private sealed class ThrowingMenuAdminManagementService : IMenuManagementService
    {
        public Task<MenuOperationResult> SaveSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(request.SectionId ?? Guid.NewGuid()));

        public Task<MenuOperationResult> SaveItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Test exception");

        public Task<MenuOperationResult> SaveServiceWindowsAsync(SaveMenuServiceWindowRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderSectionsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderItemsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ArchiveSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));

        public Task<MenuOperationResult> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));
    }

    private sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
    {
        private AuthenticationState authenticationState = new(new ClaimsPrincipal(new ClaimsIdentity()));

        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(authenticationState);

        public void SetUser(ClaimsPrincipal user)
        {
            authenticationState = new AuthenticationState(user);
            NotifyAuthenticationStateChanged(Task.FromResult(authenticationState));
        }
    }

    private sealed class TestAuthorizationService(IAuthorizationPolicyProvider policyProvider) : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            IEnumerable<IAuthorizationRequirement> requirements) =>
            Task.FromResult(Evaluate(user, requirements));

        public async Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            string policyName)
        {
            var policy = await policyProvider.GetPolicyAsync(policyName);
            return policy is null
                ? AuthorizationResult.Failed()
                : Evaluate(user, policy.Requirements);
        }

        private static AuthorizationResult Evaluate(
            ClaimsPrincipal user,
            IEnumerable<IAuthorizationRequirement> requirements)
        {
            foreach (var requirement in requirements)
            {
                switch (requirement)
                {
                    case DenyAnonymousAuthorizationRequirement when user.Identity?.IsAuthenticated != true:
                        return AuthorizationResult.Failed();

                    case RolesAuthorizationRequirement rolesRequirement
                        when !rolesRequirement.AllowedRoles.Any(user.IsInRole):
                        return AuthorizationResult.Failed();
                }
            }

            return AuthorizationResult.Success();
        }
    }
}
