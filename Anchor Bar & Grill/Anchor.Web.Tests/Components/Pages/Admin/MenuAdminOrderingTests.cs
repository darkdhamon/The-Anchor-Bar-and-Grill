using System.Security.Claims;
using AngleSharp.Dom;
using Anchor.Domain.Identity;
using Anchor.Domain.Menu;
using Anchor.Web.Components.Pages.Admin;
using Anchor.Web.Images;
using Anchor.Web.Tests.Support;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Web.Tests.Components.Pages.Admin;

public sealed class MenuAdminOrderingTests : BunitContext
{
    private static readonly Guid AppetizersSectionId = Guid.Parse("B09EAEC0-1200-4F87-91D6-E5A2F321B782");
    private static readonly Guid WingsSectionId = Guid.Parse("A819AA83-B549-4377-B16B-9C8D0B64CE63");
    private static readonly Guid BurgersSectionId = Guid.Parse("C90A2631-C454-4720-8B44-AB842DD4BE2C");
    private static readonly Guid LoadedNachosItemId = Guid.Parse("B50D2B2C-93F6-42FD-BE4E-B01FA7E0C6D0");
    private static readonly Guid FriedPicklesItemId = Guid.Parse("3F0A5C65-C4A7-4252-8BD4-082A5C54F78B");
    private static readonly Guid CheeseCurdsItemId = Guid.Parse("D5456306-0BC6-4511-BF89-8A95D144B3C2");
    private static readonly Guid BreakfastParentSectionId = Guid.Parse("73BE4D4F-A7CF-4A79-B784-48997D9AB0F8");
    private static readonly Guid OmeletsChildSectionId = Guid.Parse("E45745BE-2BF9-48B8-8736-4A19E97D5B54");
    private static readonly Guid BreakfastToastItemId = Guid.Parse("9F6D3F61-1E16-45A1-88F5-0DF734A1BA76");
    private readonly TestAuthenticationStateProvider authStateProvider;

    public MenuAdminOrderingTests()
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
        Services.AddSingleton<IMenuItemImageStorage>(new TestMenuItemImageStorage());
    }

    [Fact]
    public void Move_section_down_button_reorders_sections()
    {
        var store = new MutableMenuTreeStore();
        ConfigureMenuServices(store);
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu?tab=food&food=all");

        cut.FindAll("button[title='Move section down']")[0].Click();

        cut.WaitForAssertion(() => Assert.Equal(
            ["Wings", "Appetizers", "Burgers"],
            GetSectionTitles(cut).Take(3).ToArray()));
    }

    [Fact]
    public void Move_item_down_button_reorders_items()
    {
        var store = new MutableMenuTreeStore();
        ConfigureMenuServices(store);
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu?tab=food&food=all");
        ExpandSection(cut, "Appetizers");
        var appetizersSection = GetSectionElement(cut, "Appetizers");

        appetizersSection.QuerySelectorAll("button[title='Move item down']")[0].Click();

        cut.WaitForAssertion(() => Assert.Equal(
            ["Fried Pickles", "Loaded Nachos", "Cheese Curds"],
            GetItemTitles(cut, "Appetizers")));
    }

    [Fact]
    public void Dragging_section_to_lower_slot_reorders_sections()
    {
        var store = new MutableMenuTreeStore();
        ConfigureMenuServices(store);
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu?tab=food&food=all");
        var source = cut.FindAll(".menu-editor-tree__row")
            .Single(row => row.TextContent.Contains("Appetizers", StringComparison.OrdinalIgnoreCase)
                && row.TextContent.Contains("Food section", StringComparison.OrdinalIgnoreCase));

        source.TriggerEvent("ondragstart", new DragEventArgs());
        var target = cut.FindAll(".menu-editor-tree__row")
            .Single(row => row.TextContent.Contains("Burgers", StringComparison.OrdinalIgnoreCase)
                && row.TextContent.Contains("Food section", StringComparison.OrdinalIgnoreCase));
        target.TriggerEvent("ondrop", new DragEventArgs());

        cut.WaitForAssertion(() => Assert.Equal(
            ["Wings", "Burgers", "Appetizers"],
            GetSectionTitles(cut).Take(3).ToArray()));
    }

    [Fact]
    public void Dragging_item_to_lower_slot_reorders_items()
    {
        var store = new MutableMenuTreeStore();
        ConfigureMenuServices(store);
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu?tab=food&food=all");
        ExpandSection(cut, "Appetizers");
        var source = cut.FindAll(".menu-editor-tree__row")
            .Single(row => row.TextContent.Contains("Loaded Nachos", StringComparison.OrdinalIgnoreCase));

        source.TriggerEvent("ondragstart", new DragEventArgs());
        var target = cut.FindAll(".menu-editor-tree__row")
            .Single(row => row.TextContent.Contains("Cheese Curds", StringComparison.OrdinalIgnoreCase));
        target.TriggerEvent("ondrop", new DragEventArgs());

        cut.WaitForAssertion(() => Assert.Equal(
            ["Fried Pickles", "Cheese Curds", "Loaded Nachos"],
            GetItemTitles(cut, "Appetizers")));
    }

    [Fact]
    public void Move_subsection_up_reorders_mixed_parent_content_stream()
    {
        var store = new MixedContentMenuTreeStore();
        ConfigureMixedContentMenuServices(store);
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin("/admin/menu?tab=food&food=breakfast");
        ExpandSection(cut, "Breakfast Plates");
        var breakfastSection = GetSectionElement(cut, "Breakfast Plates");

        breakfastSection.QuerySelectorAll("button[title='Move subsection up']")[0].Click();

        cut.WaitForAssertion(() => Assert.Equal(
            ["Omelets", "White Toast"],
            GetMixedContentTitles(cut, "Breakfast Plates")));
    }

    private void ConfigureMenuServices(MutableMenuTreeStore store)
    {
        Services.AddSingleton<IMenuQueryService>(new MutableMenuTreeQueryService(store));
        Services.AddSingleton<IMenuManagementService>(new MutableMenuTreeManagementService(store));
    }

    private void ConfigureMixedContentMenuServices(MixedContentMenuTreeStore store)
    {
        Services.AddSingleton<IMenuQueryService>(new MixedContentMenuTreeQueryService(store));
        Services.AddSingleton<IMenuManagementService>(new MixedContentMenuTreeManagementService(store));
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

    private static void ExpandSection(IRenderedComponent<ContainerFragment> cut, string title)
    {
        var section = GetSectionElement(cut, title);
        var toggle = section.QuerySelector(":scope > .menu-editor-tree__row");

        if (toggle is not null
            && string.Equals(toggle.GetAttribute("aria-expanded"), "false", StringComparison.Ordinal))
        {
            toggle.Click();
        }
    }

    private static IElement GetSectionElement(IRenderedComponent<ContainerFragment> cut, string title) =>
        cut.FindAll("article.menu-editor-tree__section")
            .Single(section => string.Equals(
                section.QuerySelector(":scope > .menu-editor-tree__row .menu-editor-tree__title")?.TextContent.Trim(),
                title,
                StringComparison.Ordinal));

    private static IReadOnlyList<string> GetSectionTitles(IRenderedComponent<ContainerFragment> cut) =>
        cut.FindAll("article.menu-editor-tree__section")
            .Select(section => section.QuerySelector(":scope > .menu-editor-tree__row .menu-editor-tree__title")?.TextContent.Trim() ?? string.Empty)
            .ToArray();

    private static IReadOnlyList<string> GetItemTitles(IRenderedComponent<ContainerFragment> cut, string sectionTitle)
    {
        var section = GetSectionElement(cut, sectionTitle);
        return section.QuerySelectorAll(".menu-editor-tree__group .menu-editor-tree__row:not(.menu-editor-tree__row--subsection) .menu-editor-tree__title")
            .Select(title => title.TextContent.Trim())
            .ToArray();
    }

    private static IReadOnlyList<string> GetMixedContentTitles(IRenderedComponent<ContainerFragment> cut, string sectionTitle)
    {
        var section = GetSectionElement(cut, sectionTitle);
        return section.QuerySelectorAll(".menu-editor-tree__group .menu-editor-tree__row .menu-editor-tree__title")
            .Select(title => title.TextContent.Trim())
            .ToArray();
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

    private sealed class MutableMenuTreeStore
    {
        private readonly List<MenuSectionAdminView> sections =
        [
            MenuAdminViewFactory.Section(AppetizersSectionId, "Appetizers", MenuFamily.Food, [MenuTab.Lunch], 1),
            MenuAdminViewFactory.Section(WingsSectionId, "Wings", MenuFamily.Food, [MenuTab.Lunch], 2),
            MenuAdminViewFactory.Section(BurgersSectionId, "Burgers", MenuFamily.Food, [MenuTab.Lunch], 3)
        ];

        private readonly List<MenuItemAdminView> items =
        [
            MenuAdminViewFactory.Item(LoadedNachosItemId, MenuFamily.Food, "Loaded Nachos", "Shareable opener.", 1, [MenuAdminViewFactory.Assignment(AppetizersSectionId, "Appetizers", 1)], [MenuTab.Lunch], [new MenuItemPriceVariantView("Regular", 10m, 1)]),
            MenuAdminViewFactory.Item(FriedPicklesItemId, MenuFamily.Food, "Fried Pickles", "Crisp starter.", 2, [MenuAdminViewFactory.Assignment(AppetizersSectionId, "Appetizers", 2)], [MenuTab.Lunch], [new MenuItemPriceVariantView("Regular", 9m, 1)]),
            MenuAdminViewFactory.Item(CheeseCurdsItemId, MenuFamily.Food, "Cheese Curds", "House favorite.", 3, [MenuAdminViewFactory.Assignment(AppetizersSectionId, "Appetizers", 3)], [MenuTab.Lunch], [new MenuItemPriceVariantView("Regular", 11m, 1)]),
            MenuAdminViewFactory.Item(Guid.Parse("F362955C-F9A6-4B22-87A1-0CB1B583F5A7"), MenuFamily.Food, "Traditional Wings", "Sauced and shareable.", 1, [MenuAdminViewFactory.Assignment(WingsSectionId, "Wings", 1)], [MenuTab.Lunch], [new MenuItemPriceVariantView("Regular", 14m, 1)]),
            MenuAdminViewFactory.Item(Guid.Parse("4EA66718-BF13-4727-95F9-83D7D5D3BAE9"), MenuFamily.Food, "Anchor Burger", "Griddled classic.", 1, [MenuAdminViewFactory.Assignment(BurgersSectionId, "Burgers", 1)], [MenuTab.Lunch], [new MenuItemPriceVariantView("Regular", 13m, 1)])
        ];

        public MenuManagementView BuildView() =>
            new(
                BuildHours(),
                sections.OrderBy(section => section.SortOrder).ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase).ToArray(),
                items.OrderByDescending(item => item.Special is not null).ThenBy(item => item.SortOrder).ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray());

        public void ReorderSections(IReadOnlyList<SaveMenuSortOrderRequest> requests)
        {
            foreach (var request in requests)
            {
                var index = sections.FindIndex(section => section.SectionId == request.RecordId);
                sections[index] = sections[index] with { SortOrder = request.SortOrder };
            }
        }

        public void ReorderItems(IReadOnlyList<SaveMenuSortOrderRequest> requests)
        {
            foreach (var request in requests)
            {
                var index = items.FindIndex(item => item.ItemId == request.RecordId);
                var item = items[index];
                items[index] = item with
                {
                    SectionAssignments = item.SectionAssignments
                        .Select(assignment => assignment.SectionId == request.ContextId
                            ? assignment with { SortOrder = request.SortOrder }
                            : assignment)
                        .ToArray()
                };
            }
        }

        private static IReadOnlyList<MenuTabHoursAdminView> BuildHours()
        {
            var days = Enum.GetValues<DayOfWeek>()
                .Select(day => new MenuServiceWindowView(day, day.ToString(), true, "11:00 AM - 5:00 PM", false, new TimeOnly(11, 0), new TimeOnly(17, 0), false))
                .ToArray();

            return
            [
                new MenuTabHoursAdminView(MenuTab.Breakfast, "Breakfast", days),
                new MenuTabHoursAdminView(MenuTab.Lunch, "Lunch", days),
                new MenuTabHoursAdminView(MenuTab.Dinner, "Dinner", days),
                new MenuTabHoursAdminView(MenuTab.Drinks, "Drinks", days)
            ];
        }
    }

    private sealed class MutableMenuTreeQueryService(MutableMenuTreeStore store) : IMenuQueryService
    {
        public Task<MenuTab> GetSuggestedPublicTabAsync(DateOnly today, TimeOnly currentTime, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PublicMenuView> GetPublicMenuAsync(MenuTab requestedTab, DateOnly today, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PublicHomeSpecialView>> GetHomeSpecialsAsync(DateOnly today, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublicHomeSpecialView>>([]);

        public Task<MenuManagementView> GetMenuManagementViewAsync(DateOnly today, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.BuildView());
    }

    private sealed class MixedContentMenuTreeStore
    {
        private readonly List<MenuSectionAdminView> sections =
        [
            MenuAdminViewFactory.Section(BreakfastParentSectionId, "Breakfast Plates", MenuFamily.Food, [MenuTab.Breakfast], 1),
            MenuAdminViewFactory.Section(OmeletsChildSectionId, "Omelets", MenuFamily.Food, [MenuTab.Breakfast], 2, parentSectionId: BreakfastParentSectionId, parentSectionName: "Breakfast Plates")
        ];

        private readonly List<MenuItemAdminView> items =
        [
            MenuAdminViewFactory.Item(BreakfastToastItemId, MenuFamily.Food, "White Toast", "Simple side.", 1, [MenuAdminViewFactory.Assignment(BreakfastParentSectionId, "Breakfast Plates", 1)], [MenuTab.Breakfast], [new MenuItemPriceVariantView("Regular", 2m, 1)]),
            MenuAdminViewFactory.Item(Guid.Parse("755EC9E6-38E7-4B4E-B52A-21D4B21EEC04"), MenuFamily.Food, "Denver Omelet", "Child section item.", 1, [MenuAdminViewFactory.Assignment(OmeletsChildSectionId, "Omelets", 1)], [MenuTab.Breakfast], [new MenuItemPriceVariantView("Regular", 12m, 1)])
        ];

        public MenuManagementView BuildView() =>
            new(
                BuildHours(),
                sections.OrderBy(section => section.SortOrder).ThenBy(section => section.Name, StringComparer.OrdinalIgnoreCase).ToArray(),
                items.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToArray());

        public void ReorderSections(IReadOnlyList<SaveMenuSortOrderRequest> requests)
        {
            foreach (var request in requests)
            {
                var index = sections.FindIndex(section => section.SectionId == request.RecordId);
                sections[index] = sections[index] with { SortOrder = request.SortOrder };
            }
        }

        public void ReorderItems(IReadOnlyList<SaveMenuSortOrderRequest> requests)
        {
            foreach (var request in requests)
            {
                var index = items.FindIndex(item => item.ItemId == request.RecordId);
                var item = items[index];
                items[index] = item with
                {
                    SectionAssignments = item.SectionAssignments
                        .Select(assignment => assignment.SectionId == request.ContextId
                            ? assignment with { SortOrder = request.SortOrder }
                            : assignment)
                        .ToArray()
                };
            }
        }

        private static IReadOnlyList<MenuTabHoursAdminView> BuildHours()
        {
            var days = Enum.GetValues<DayOfWeek>()
                .Select(day => new MenuServiceWindowView(day, day.ToString(), true, "11:00 AM - 5:00 PM", false, new TimeOnly(11, 0), new TimeOnly(17, 0), false))
                .ToArray();

            return
            [
                new MenuTabHoursAdminView(MenuTab.Breakfast, "Breakfast", days),
                new MenuTabHoursAdminView(MenuTab.Lunch, "Lunch", days),
                new MenuTabHoursAdminView(MenuTab.Dinner, "Dinner", days),
                new MenuTabHoursAdminView(MenuTab.Drinks, "Drinks", days)
            ];
        }
    }

    private sealed class MixedContentMenuTreeQueryService(MixedContentMenuTreeStore store) : IMenuQueryService
    {
        public Task<MenuTab> GetSuggestedPublicTabAsync(DateOnly today, TimeOnly currentTime, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PublicMenuView> GetPublicMenuAsync(MenuTab requestedTab, DateOnly today, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PublicHomeSpecialView>> GetHomeSpecialsAsync(DateOnly today, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublicHomeSpecialView>>([]);

        public Task<MenuManagementView> GetMenuManagementViewAsync(DateOnly today, CancellationToken cancellationToken = default) =>
            Task.FromResult(store.BuildView());
    }

    private sealed class MixedContentMenuTreeManagementService(MixedContentMenuTreeStore store) : IMenuManagementService
    {
        public Task<MenuOperationResult> SaveSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(request.SectionId ?? Guid.NewGuid()));

        public Task<MenuOperationResult> SaveItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(request.ItemId ?? Guid.NewGuid()));

        public Task<MenuOperationResult> SaveServiceWindowsAsync(SaveMenuServiceWindowRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderSectionsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default)
        {
            store.ReorderSections(requests);
            return Task.FromResult(MenuOperationResult.Success());
        }

        public Task<MenuOperationResult> ReorderItemsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default)
        {
            store.ReorderItems(requests);
            return Task.FromResult(MenuOperationResult.Success());
        }

        public Task<MenuOperationResult> ArchiveSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(sectionId));

        public Task<MenuOperationResult> ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));

        public Task<MenuOperationResult> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(itemId));
    }

    private sealed class MutableMenuTreeManagementService(MutableMenuTreeStore store) : IMenuManagementService
    {
        public Task<MenuOperationResult> SaveSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(request.SectionId ?? Guid.NewGuid()));

        public Task<MenuOperationResult> SaveItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(request.ItemId ?? Guid.NewGuid()));

        public Task<MenuOperationResult> SaveServiceWindowsAsync(SaveMenuServiceWindowRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success());

        public Task<MenuOperationResult> ReorderSectionsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default)
        {
            store.ReorderSections(requests);
            return Task.FromResult(MenuOperationResult.Success());
        }

        public Task<MenuOperationResult> ReorderItemsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default)
        {
            store.ReorderItems(requests);
            return Task.FromResult(MenuOperationResult.Success());
        }

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
