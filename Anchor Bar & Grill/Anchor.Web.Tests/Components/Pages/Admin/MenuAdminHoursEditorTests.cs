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
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Web.Tests.Components.Pages.Admin;

public sealed class MenuAdminHoursEditorTests : BunitContext
{
    private static readonly DayOfWeek[] OrderedDays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    ];

    private readonly TestAuthenticationStateProvider authStateProvider;
    private readonly MutableMenuHoursStore hoursStore;

    public MenuAdminHoursEditorTests()
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

        hoursStore = new MutableMenuHoursStore();
        Services.AddSingleton<IMenuQueryService>(new MutableMenuHoursQueryService(hoursStore));
        Services.AddSingleton<IMenuManagementService>(new MutableMenuHoursManagementService(hoursStore));
    }

    [Fact]
    public void HoursTabSwitch_uses_the_selected_tab_values()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin();

        Assert.Equal("11:00 AM", GetOpenTimeValue(cut, DayOfWeek.Tuesday));

        ClickHoursTab(cut, "Dinner");
        cut.WaitForAssertion(() => Assert.Equal("4:00 PM", GetOpenTimeValue(cut, DayOfWeek.Tuesday)));

        ClickHoursTab(cut, "Lunch");
        cut.WaitForAssertion(() => Assert.Equal("11:00 AM", GetOpenTimeValue(cut, DayOfWeek.Tuesday)));
    }

    [Fact]
    public void Saved_hours_remain_after_switching_to_another_tab_and_back()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin();
        GetTimeInputs(cut, DayOfWeek.Tuesday)[0].Input("1230");
        GetTimeInputs(cut, DayOfWeek.Tuesday)[1].Input("330p");

        ClickSaveHours(cut);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Lunch service hours updated.", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(new TimeOnly(12, 30), hoursStore.Get(MenuTab.Lunch, DayOfWeek.Tuesday).OpensAt);
            Assert.Equal(new TimeOnly(15, 30), hoursStore.Get(MenuTab.Lunch, DayOfWeek.Tuesday).ClosesAt);
        });

        ClickHoursTab(cut, "Dinner");
        cut.WaitForAssertion(() => Assert.Equal("4:00 PM", GetOpenTimeValue(cut, DayOfWeek.Tuesday)));

        ClickHoursTab(cut, "Lunch");
        cut.WaitForAssertion(() =>
        {
            Assert.Equal("12:30 PM", GetOpenTimeValue(cut, DayOfWeek.Tuesday));
            Assert.Equal("3:30 PM", GetCloseTimeValue(cut, DayOfWeek.Tuesday));
        });
    }

    [Fact]
    public void Typing_time_and_immediately_saving_persists_the_latest_values()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin();
        GetTimeInputs(cut, DayOfWeek.Tuesday)[0].Input("1030");
        GetTimeInputs(cut, DayOfWeek.Tuesday)[1].Input("330p");

        ClickSaveHours(cut);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Lunch service hours updated.", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(new TimeOnly(10, 30), hoursStore.Get(MenuTab.Lunch, DayOfWeek.Tuesday).OpensAt);
            Assert.Equal(new TimeOnly(15, 30), hoursStore.Get(MenuTab.Lunch, DayOfWeek.Tuesday).ClosesAt);
        });
    }

    [Fact]
    public void Hours_input_blur_normalizes_shorthand_to_twelve_hour_display()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin();
        var openInput = GetTimeInputs(cut, DayOfWeek.Tuesday)[0];

        openInput.Input("1300");
        openInput.TriggerEvent("onblur", new FocusEventArgs());

        cut.WaitForAssertion(() => Assert.Equal("1:00 PM", GetOpenTimeValue(cut, DayOfWeek.Tuesday)));
    }

    [Fact]
    public void Save_hours_is_disabled_until_available_rows_have_valid_times()
    {
        authStateProvider.SetUser(CreateUser("menu.manager@anchor.test", ApplicationRoles.MenuManager));

        var cut = RenderMenuAdmin();
        var openInput = GetTimeInputs(cut, DayOfWeek.Tuesday)[0];

        openInput.Focus();
        openInput.Input(string.Empty);

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(string.Empty, GetOpenTimeValue(cut, DayOfWeek.Tuesday));
            Assert.True(GetSaveHoursButton(cut).HasAttribute("disabled"));
            Assert.Contains("Complete required times", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });

        openInput.Input("1230");

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("1230", GetOpenTimeValue(cut, DayOfWeek.Tuesday));
            Assert.False(GetSaveHoursButton(cut).HasAttribute("disabled"));
        });
    }

    private IRenderedComponent<ContainerFragment> RenderMenuAdmin()
    {
        var cut = Render(builder =>
        {
            builder.OpenComponent<CascadingAuthenticationState>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<MenuAdmin>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        cut.FindAll(".menu-editor-tabs__button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Hours", StringComparison.Ordinal))
            .Click();

        return cut;
    }

    private static void ClickHoursTab(IRenderedComponent<ContainerFragment> cut, string label)
    {
        cut.FindAll(".menu-hours-tab-row button")
            .Single(button => string.Equals(button.TextContent.Trim(), label, StringComparison.Ordinal))
            .Click();
    }

    private static void ClickSaveHours(IRenderedComponent<ContainerFragment> cut)
    {
        GetSaveHoursButton(cut).Click();
    }

    private static AngleSharp.Dom.IElement GetSaveHoursButton(IRenderedComponent<ContainerFragment> cut) =>
        cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Save Lunch hours", StringComparison.Ordinal));

    private static string? GetOpenTimeValue(IRenderedComponent<ContainerFragment> cut, DayOfWeek dayOfWeek) =>
        GetTimeInputs(cut, dayOfWeek)[0].GetAttribute("value");

    private static string? GetCloseTimeValue(IRenderedComponent<ContainerFragment> cut, DayOfWeek dayOfWeek) =>
        GetTimeInputs(cut, dayOfWeek)[1].GetAttribute("value");

    private static IReadOnlyList<AngleSharp.Dom.IElement> GetTimeInputs(IRenderedComponent<ContainerFragment> cut, DayOfWeek dayOfWeek)
    {
        var dayIndex = Array.IndexOf(OrderedDays, dayOfWeek);
        var row = cut.FindAll(".menu-hours-editor__row")[dayIndex];
        return row.QuerySelectorAll("input[type='text']");
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

    private sealed class MutableMenuHoursQueryService(MutableMenuHoursStore store) : IMenuQueryService
    {
        private static readonly Guid FoodSectionId = Guid.Parse("A10F7FBA-0D4F-45E1-9E9A-53CF736F867D");

        public Task<PublicMenuView> GetPublicMenuAsync(MenuTab requestedTab, DateOnly today, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PublicHomeSpecialView>> GetHomeSpecialsAsync(DateOnly today, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublicHomeSpecialView>>([]);

        public Task<MenuManagementView> GetMenuManagementViewAsync(DateOnly today, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<MenuSectionAdminView> sections =
            [
                new(FoodSectionId, "Appetizers", MenuFamily.Food, 1, true, false, [])
            ];

            IReadOnlyList<MenuTabHoursAdminView> tabs = Enum.GetValues<MenuTab>()
                .Select(tab => new MenuTabHoursAdminView(
                    tab,
                    tab.ToString(),
                    OrderedDays
                        .Select(day =>
                        {
                            var window = store.Get(tab, day);
                            return new MenuServiceWindowView(
                                day,
                                day.ToString(),
                                window.IsAvailable,
                                FormatWindow(window),
                                today.DayOfWeek == day,
                                window.OpensAt,
                                window.ClosesAt,
                                window.ClosesNextDay);
                        })
                        .ToArray()))
                .ToArray();

            return Task.FromResult(new MenuManagementView(tabs, sections, []));
        }

        private static string FormatWindow(MutableMenuHoursStore.WindowState window)
        {
            if (!window.IsAvailable || window.OpensAt is null || window.ClosesAt is null)
            {
                return "Not served";
            }

            var suffix = window.ClosesNextDay ? " next day" : string.Empty;
            return $"{window.OpensAt:HH\\:mm} - {window.ClosesAt:HH\\:mm}{suffix}";
        }
    }

    private sealed class MutableMenuHoursManagementService(MutableMenuHoursStore store) : IMenuManagementService
    {
        public Task<MenuOperationResult> SaveSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(request.SectionId ?? Guid.NewGuid()));

        public Task<MenuOperationResult> SaveItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuOperationResult.Success(request.ItemId ?? Guid.NewGuid()));

        public Task<MenuOperationResult> SaveServiceWindowsAsync(SaveMenuServiceWindowRequest request, CancellationToken cancellationToken = default)
        {
            foreach (var day in request.Days)
            {
                store.Set(
                    request.Tab,
                    day.DayOfWeek,
                    new MutableMenuHoursStore.WindowState(
                        day.IsAvailable,
                        day.OpensAt,
                        day.ClosesAt,
                        day.ClosesNextDay));
            }

            return Task.FromResult(MenuOperationResult.Success());
        }

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

    private sealed class MutableMenuHoursStore
    {
        private readonly Dictionary<(MenuTab Tab, DayOfWeek Day), WindowState> windows = new()
        {
            [(MenuTab.Lunch, DayOfWeek.Tuesday)] = new(true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
            [(MenuTab.Dinner, DayOfWeek.Tuesday)] = new(true, new TimeOnly(16, 0), new TimeOnly(21, 0), false)
        };

        public WindowState Get(MenuTab tab, DayOfWeek dayOfWeek) =>
            windows.TryGetValue((tab, dayOfWeek), out var window)
                ? window
                : new WindowState(false, null, null, false);

        public void Set(MenuTab tab, DayOfWeek dayOfWeek, WindowState state) =>
            windows[(tab, dayOfWeek)] = state;

        public sealed record WindowState(bool IsAvailable, TimeOnly? OpensAt, TimeOnly? ClosesAt, bool ClosesNextDay);
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
