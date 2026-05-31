using Anchor.Domain.Identity;
using Anchor.Web.Components.Account.Shared;
using Anchor.Domain.Identity.Users;
using Anchor.Domain.Events;
using Anchor.Domain.Menu;
using Anchor.Domain.Publicity;
using Anchor.Web.Components.Layout;
using Anchor.Web.Components.Pages;
using Anchor.Web.Components.Pages.Admin;
using Anchor.Web.Images;
using Anchor.Web.Tests.Support;
using Bunit;
using Bunit.JSInterop;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Anchor.Web.Tests;

public sealed class LayoutAndPageRenderTests : BunitContext
{
    private readonly TestAuthenticationStateProvider authStateProvider;
    private readonly FakeEventQueryService eventQueryService;
    private readonly FakeMenuQueryService menuQueryService;
    private readonly FakeHomepagePublicityService homepagePublicityService;
    private readonly FixedTimeProvider timeProvider;

    public LayoutAndPageRenderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
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
        eventQueryService = new FakeEventQueryService();
        menuQueryService = new FakeMenuQueryService();
        homepagePublicityService = new FakeHomepagePublicityService();
        timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 5, 21, 12, 0, 0, TimeSpan.FromHours(-5)));
        Services.AddSingleton<TimeProvider>(timeProvider);
        Services.AddSingleton<IEventQueryService>(eventQueryService);
        Services.AddSingleton<IMenuQueryService>(menuQueryService);
        Services.AddSingleton<IHomepagePublicityService>(homepagePublicityService);
        Services.AddSingleton<IMenuManagementService>(new FakeMenuManagementService());
        Services.AddSingleton<IMenuItemImageStorage>(new TestMenuItemImageStorage());
    }

    [Fact]
    public void MainLayout_RendersStaticNavigationHooks_ForGuests()
    {
        authStateProvider.SetUser(new ClaimsPrincipal(new ClaimsIdentity()));

        var cut = Render(builder =>
        {
            builder.OpenComponent<CascadingAuthenticationState>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.AddAttribute(1, "Body", (RenderFragment)(bodyBuilder => bodyBuilder.AddMarkupContent(0, "<section>Preview body</section>")));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        Assert.Contains("theme-light", cut.Markup);
        Assert.Contains("Browse", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Staff Access", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Staff Log In", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Log In", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Single(cut.FindAll(".brand-lockup__logo-frame"));
        Assert.Equal("The Anchor Bar & Grill", cut.Find(".brand-lockup__logo").GetAttribute("alt"));
        Assert.DoesNotContain("Comfort food, community nights, and a friendly place to gather.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">The Anchor Bar &amp; Grill<", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">Register<", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Event Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("site-header__nav-stack is-open", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("data-anchor-theme-toggle=\"true\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aria-expanded=\"false\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-enhance-nav=\"false\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        var guestMenuToggle = Assert.Single(cut.FindAll("[data-anchor-menu-toggle='true']"));
        Assert.Equal("site-header-nav", guestMenuToggle.GetAttribute("data-anchor-menu-target"));
        Assert.Equal("drawer", guestMenuToggle.GetAttribute("data-anchor-menu-kind"));
        Assert.Empty(cut.FindAll("#site-account-menu"));
        Assert.Contains("Preview body", cut.Markup);
    }

    [Fact]
    public void MainLayout_UsesPersistedThemeCookieForInitialRender()
    {
        authStateProvider.SetUser(new ClaimsPrincipal(new ClaimsIdentity()));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = "anchor-theme=dark";
        Services.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

        var cut = Render(builder =>
        {
            builder.OpenComponent<CascadingAuthenticationState>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.AddAttribute(1, "Body", (RenderFragment)(bodyBuilder => bodyBuilder.AddMarkupContent(0, "<section>Preview body</section>")));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        Assert.Contains("theme-dark", cut.Markup);
        Assert.Contains("checked", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MainLayout_ShowsRoleScopedStaffLinksForAdminUsers()
    {
        authStateProvider.SetUser(CreateUserWithName("admin@anchor.test", "Harbor", "Captain", ApplicationRoles.Admin));

        var cut = Render(builder =>
        {
            builder.OpenComponent<CascadingAuthenticationState>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.AddAttribute(1, "Body", (RenderFragment)(bodyBuilder => bodyBuilder.AddMarkupContent(0, "<section>Preview body</section>")));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        Assert.Contains("Staff tools", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Account Tools", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Account", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Help", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Event Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Menu Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Publicity Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Contact Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("User Management", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Security", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IT / System", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Manage Account", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hi, Harbor Captain", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Log Out", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Single(cut.FindAll(".brand-lockup__logo-frame"));
        Assert.Equal("The Anchor Bar & Grill", cut.Find(".brand-lockup__logo").GetAttribute("alt"));
        Assert.DoesNotContain("Comfort food, community nights, and a friendly place to gather.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">The Anchor Bar &amp; Grill<", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, cut.FindAll("[data-anchor-menu-toggle='true']").Count);
        Assert.Single(cut.FindAll("#site-account-menu"));
        var accountMenuToggle = cut.Find("[data-anchor-menu-target='site-account-menu']");
        Assert.Equal("menu", accountMenuToggle.GetAttribute("aria-haspopup"));
        Assert.DoesNotContain(">Register<", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MainLayout_ShowsTechnicalToolsOnlyForItUsers()
    {
        authStateProvider.SetUser(CreateUser("it@anchor.test", ApplicationRoles.It));

        var cut = Render(builder =>
        {
            builder.OpenComponent<CascadingAuthenticationState>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.AddAttribute(1, "Body", (RenderFragment)(bodyBuilder => bodyBuilder.AddMarkupContent(0, "<section>Preview body</section>")));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        Assert.DoesNotContain("Help", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IT / System", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("User Management", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Security", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Publicity Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Contact Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hi, It", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MainLayout_ShowsOnlyEventToolsForEventManagers()
    {
        authStateProvider.SetUser(CreateUser("events@anchor.test", ApplicationRoles.EventManager));

        var cut = Render(builder =>
        {
            builder.OpenComponent<CascadingAuthenticationState>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.AddAttribute(1, "Body", (RenderFragment)(bodyBuilder => bodyBuilder.AddMarkupContent(0, "<section>Preview body</section>")));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        Assert.DoesNotContain("Help", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Event Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Menu Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("User Management", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Security", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IT / System", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Publicity Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Contact Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MainLayout_ShowsOnlyMenuToolsForMenuManagers()
    {
        authStateProvider.SetUser(CreateUser("menu@anchor.test", ApplicationRoles.MenuManager));

        var cut = Render(builder =>
        {
            builder.OpenComponent<CascadingAuthenticationState>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<MainLayout>(0);
                childBuilder.AddAttribute(1, "Body", (RenderFragment)(bodyBuilder => bodyBuilder.AddMarkupContent(0, "<section>Preview body</section>")));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        Assert.DoesNotContain("Help", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Menu Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Event Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("User Management", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Security", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IT / System", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Publicity Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Contact Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HomePage_RendersThreeColumnHomepageWithServiceBackedSidebars()
    {
        homepagePublicityService.PublishedContent = new HomepagePublicityContent(
            string.Empty,
            "Fresh copy from the publicity editor.",
            "Published homepage messaging should flow through to the guest-facing welcome block." + Environment.NewLine + Environment.NewLine + "A second paragraph should render as supporting body copy.");

        var cut = Render<Home>();

        Assert.NotNull(cut.Find(".home-shell"));
        Assert.NotNull(cut.Find(".home-main"));
        Assert.NotNull(cut.Find(".home-rail--specials"));
        Assert.NotNull(cut.Find(".home-rail--events"));
        Assert.NotNull(cut.Find(".home-main .home-carousel"));
        Assert.Contains("Fresh copy from the publicity editor.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Published homepage messaging should flow through to the guest-facing welcome block.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("A second paragraph should render as supporting body copy.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(cut.FindAll(".home-main .page-hero__eyebrow"));
        Assert.Equal("Published homepage messaging should flow through to the guest-facing welcome block.", cut.Find(".home-main .page-hero__lead").TextContent.Trim());
        Assert.Single(cut.FindAll(".home-main .page-hero__copy"));
        var homeHeroMarkup = cut.Find(".home-main .page-hero").InnerHtml;
        Assert.True(
            homeHeroMarkup.IndexOf("data-anchor-carousel=\"true\"", StringComparison.OrdinalIgnoreCase) <
            homeHeroMarkup.IndexOf("A second paragraph should render as supporting body copy.", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("Browse the Menu", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Plan Your Visit", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-anchor-carousel=\"true\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(5, cut.FindAll("[data-anchor-carousel-slide]").Count);
        Assert.Equal(5, cut.FindAll("[data-anchor-carousel-to]").Count);
        Assert.Contains("data-anchor-carousel-prev", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-anchor-carousel-next", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/images/home-carousel/live-music-stage.jpg", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Patio nights with a crowd", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Open-air bar seating area with bright stools and a long stone counter.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Monday Night Burgers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Dockside Acoustic Night", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("What The Homepage Should Do", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exterior Photo Placement", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Visit Snapshot", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("For Staff", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-enhance-nav=\"false\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HomePage_Uses_Injected_TimeProvider_For_Current_Special_Status_Labeling()
    {
        timeProvider.SetLocalNow(new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.FromHours(-5)));

        var cut = Render<Home>();

        Assert.Contains("Sunday Pork Chop Dinner", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Now available", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HomePage_FallsBack_To_Mockup_Sidebars_When_Live_Data_Is_Unavailable()
    {
        Services.AddSingleton<IMenuQueryService>(new EmptyDescriptionMenuQueryService());
        Services.AddSingleton<IEventQueryService>(new FakeEventQueryService { UpcomingEvents = [] });

        var cut = Render<Home>();

        Assert.Contains("Tuesday Taco Basket", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Thursday Trivia", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Summer Kickoff Patio Party", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Dockside Acoustic Night", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicFacingPages_ExcludeInteractiveRouting()
    {
        var pageTypes = new[]
        {
            typeof(Home),
            typeof(Menu),
            typeof(Events),
            typeof(About),
            typeof(Contact),
            typeof(Help)
        };

        foreach (var pageType in pageTypes)
        {
            Assert.Contains(
                pageType.GetCustomAttributes(inherit: true),
                attribute => attribute is ExcludeFromInteractiveRoutingAttribute);
        }
    }

    [Fact]
    public void MenuPage_UsesSuggestedDefaultTabAndShowsServiceBackedSections()
    {
        menuQueryService.SuggestedTab = MenuTab.Lunch;

        var cut = Render<Menu>();
        var sidebar = cut.Find(".menu-sidebar").TextContent;
        var sidebarHours = cut.Find(".menu-sidebar .menu-hours-card").TextContent;
        var accordions = cut.FindAll(".menu-accordion__section");

        Assert.NotNull(cut.Find(".menu-page"));
        Assert.Contains("Browse by service", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Appetizers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Burgers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Lunch hours", sidebarHours, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Tuesday-Saturday", sidebarHours, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Not served", sidebarHours, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Lunch hours", sidebar, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(cut.FindAll(".menu-sidebar .hours-row--today"));
        Assert.DoesNotContain("Lunch brings together", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Shareables for the table.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, accordions.Count);
        Assert.True(accordions[0].HasAttribute("open"));
        Assert.False(accordions[1].HasAttribute("open"));
        Assert.NotEmpty(cut.FindAll(".menu-item__image"));
        Assert.NotEmpty(cut.FindAll(".menu-item--text-only"));
        Assert.Contains("Coming Soon", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Seasonal", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Limited Time", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MenuPage_UsesNextSuggestedServiceWhenNoTabIsRequested()
    {
        menuQueryService.SuggestedTab = MenuTab.Dinner;
        timeProvider.SetLocalNow(new DateTimeOffset(2026, 5, 18, 9, 0, 0, TimeSpan.FromHours(-5)));

        var cut = Render<Menu>();
        var sidebarHours = cut.Find(".menu-sidebar .menu-hours-card").TextContent;
        var accordions = cut.FindAll(".menu-accordion__section");

        Assert.Contains("Dinner hours", sidebarHours, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Monday Night Burgers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Appetizers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Specials", accordions[0].QuerySelector("h2")?.TextContent.Trim());
    }

    [Fact]
    public void MenuPage_UsesQueryStringTabSelectionAndShowsEmptyDrinksState()
    {
        menuQueryService.SuggestedTab = MenuTab.Dinner;
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/menu?tab=drinks");

        var cut = Render<Menu>();

        Assert.Contains("Drink hours are live", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Drinks hours", cut.Find(".menu-sidebar .menu-hours-card").TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Drinks", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Appetizers", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MenuPage_Skips_Blank_Item_Descriptions()
    {
        Services.AddSingleton<IMenuQueryService>(new EmptyDescriptionMenuQueryService());

        var cut = Render<Menu>();

        Assert.Contains("Pepsi", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(cut.FindAll(".menu-item__content p"));
    }

    [Fact]
    public void MenuPage_Indents_Child_Section_Items()
    {
        Services.AddSingleton<IMenuQueryService>(new NestedChildSectionMenuQueryService());

        var cut = Render<Menu>();

        Assert.Contains("Omelets", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Single(cut.FindAll(".menu-subsection__items .menu-item--nested"));
        Assert.Contains("menu-subsection__items", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EventsPage_RendersUpcomingCalendarContent()
    {
        var cut = Render<Events>();

        Assert.Contains("Events mockup", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Thursday Trivia", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Friday Live Music", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Third Friday Steak Night", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Summer Kickoff Patio Party", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Community Bingo Fundraiser", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(cut.FindAll(".event-card__image"));
        Assert.Contains("Show every currently scheduled event in one public-facing list.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("every other Friday", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AboutPage_RendersStoryAndGuestExperienceSections()
    {
        var cut = Render<About>();

        Assert.Contains("About mockup", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Story Direction", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Guest Experience", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContactPage_RendersHoursAndInquiryMockup()
    {
        var cut = Render<Contact>();

        Assert.Contains("Contact mockup", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hours Preview", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Social Media", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Facebook", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Instagram", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(cut.FindAll(".social-profile__link"));
        Assert.Contains("Send Mockup Inquiry", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EventsAdminPage_RendersHelpAndEditDeleteActions()
    {
        var cut = Render<EventsAdmin>();

        Assert.Contains("Event editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("How this page should work", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Event image (optional)", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Recurring event?", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Recurrence pattern", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Repeat cadence", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Week of month", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Recurs until (optional)", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("every other Friday", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Third Friday Steak Night", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Choose an existing badge or type a new one to create it on the fly", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, cut.FindAll("input[type='date']").Count);
        Assert.Single(cut.FindAll("input[type='time']"));
        Assert.Contains("Delete", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MenuAdminPage_RendersMenuManagementPreview()
    {
        var cut = Render<MenuAdmin>();

        Assert.NotNull(cut.Find(".menu-admin-page"));
        Assert.NotNull(cut.Find(".menu-editor-nav"));
        Assert.Contains("Menu editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Food browser", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Meal filter", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Add section", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("How to use this page", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Archive is the safe way to hide something from guests while keeping it ready for reuse", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Appetizers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Delete section", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Breakfast", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Drinks", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicityAdminHomePage_RendersDraftAndPublishWorkflow()
    {
        var cut = Render<PublicityAdminHome>();

        Assert.Contains("Publicity editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Homepage intro copy", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Save draft", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Save &amp; publish", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Published homepage preview", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContactAdminPage_RendersContactWorkflowPreview()
    {
        var cut = Render<ContactAdmin>();

        Assert.Contains("Contact editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Contact details form", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Hours preview", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Social profiles", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Profile editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Choose an existing platform or type a new one", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Add another profile", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HelpPage_ExplainsAdminCreatedAccounts()
    {
        var cut = Render<Help>();

        Assert.Contains("Browse by subject", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Browse by role type", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Admins create staff accounts, then assign roles.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("local sign-in and optional passkeys only", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Publicity Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Contact Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EventManager", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("self-register", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RegisterPage_ShowsAdminManagedAccountCreationNotice()
    {
        var cut = Render<Anchor.Web.Components.Account.Pages.Register>();

        Assert.Contains("Account creation is handled by an admin.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Public self-registration is disabled.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Go to login", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"/Account/Login\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-enhance-nav=\"false\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("type=\"submit\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ManageNavMenu_DoesNotRenderExternalLoginsEntry()
    {
        var cut = Render<ManageNavMenu>();

        Assert.Contains("Profile", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Email", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Password", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("External logins", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IdentityComponents_DoNotIncludeExternalProviderScaffolding()
    {
        var repositoryRoot = GetRepositoryRoot();
        var loginPageFile = Path.Combine(repositoryRoot, "Anchor Bar & Grill", "Anchor.Web", "Components", "Account", "Pages", "Login.razor");
        var routeBuilderFile = Path.Combine(repositoryRoot, "Anchor Bar & Grill", "Anchor.Web", "Components", "Account", "IdentityComponentsEndpointRouteBuilderExtensions.cs");
        var externalLoginPageFile = Path.Combine(repositoryRoot, "Anchor Bar & Grill", "Anchor.Web", "Components", "Account", "Pages", "ExternalLogin.razor");
        var externalLoginsManagePageFile = Path.Combine(repositoryRoot, "Anchor Bar & Grill", "Anchor.Web", "Components", "Account", "Pages", "Manage", "ExternalLogins.razor");
        var externalLoginPickerFile = Path.Combine(repositoryRoot, "Anchor Bar & Grill", "Anchor.Web", "Components", "Account", "Shared", "ExternalLoginPicker.razor");

        var loginPageMarkup = File.ReadAllText(loginPageFile);
        var routeBuilderSource = File.ReadAllText(routeBuilderFile);

        Assert.DoesNotContain("Use another service to log in.", loginPageMarkup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ExternalLoginPicker", loginPageMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("/PerformExternalLogin", routeBuilderSource, StringComparison.Ordinal);
        Assert.DoesNotContain("/LinkExternalLogin", routeBuilderSource, StringComparison.Ordinal);
        Assert.False(File.Exists(externalLoginPageFile));
        Assert.False(File.Exists(externalLoginsManagePageFile));
        Assert.False(File.Exists(externalLoginPickerFile));
    }

    [Fact]
    public void ThemeJavaScript_ContainsHomepageCarouselHooks()
    {
        var repositoryRoot = GetRepositoryRoot();
        var themeScriptFile = Path.Combine(repositoryRoot, "Anchor Bar & Grill", "Anchor.Web", "wwwroot", "theme.js");
        var themeScript = File.ReadAllText(themeScriptFile);

        Assert.Contains("data-anchor-carousel", themeScript, StringComparison.Ordinal);
        Assert.Contains("data-anchor-carousel-prev", themeScript, StringComparison.Ordinal);
        Assert.Contains("data-anchor-carousel-next", themeScript, StringComparison.Ordinal);
        Assert.Contains("data-anchor-carousel-interval", themeScript, StringComparison.Ordinal);
        Assert.Contains("touchstart", themeScript, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RazorComponents_DoNotUseRootlessInternalHrefOrActionValues()
    {
        var componentsRoot = Path.Combine(GetRepositoryRoot(), "Anchor Bar & Grill", "Anchor.Web", "Components");
        var invalidNavigationAttributes = new List<string>();
        var pattern = new Regex("(?:href|action)=\"(?!/|https?://|#|mailto:|@|\\.)([^\"]+)\"", RegexOptions.Compiled);

        foreach (var file in Directory.GetFiles(componentsRoot, "*.razor", SearchOption.AllDirectories))
        {
            if (string.Equals(Path.GetFileName(file), "App.razor", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            var matches = pattern.Matches(content);

            foreach (Match match in matches)
            {
                invalidNavigationAttributes.Add($"{Path.GetFileName(file)}: {match.Value}");
            }
        }

        Assert.Empty(invalidNavigationAttributes);
    }

    [Fact]
    public void UserManagementPage_DisablesRemovingAdminFromSignedInAdmin()
    {
        authStateProvider.SetUser(CreateUser("user-1", ApplicationRoles.Admin));
        Services.AddSingleton<IIdentityAdministrationService>(new FakeIdentityAdministrationService
        {
            Users =
            [
                new ManagedUserSummary("user-1", "admin@anchor.test", "Admin", "User", "507-555-1000", true, false, false, false, [ApplicationRoles.Admin]),
                new ManagedUserSummary("user-2", "backup-admin@anchor.test", "Backup", "Admin", null, true, true, false, false, [ApplicationRoles.Admin])
            ]
        });

        var cut = Render(builder =>
        {
            builder.OpenComponent<CascadingAuthenticationState>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<Users>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        var keepAdminButton = cut.FindAll("button")
            .Single(button => string.Equals(button.TextContent.Trim(), "Keep Admin", StringComparison.Ordinal));

        Assert.True(keepAdminButton.HasAttribute("disabled"));
        Assert.Contains("own account", keepAdminButton.GetAttribute("title"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Remove Admin", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Profile details", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Phone on file: 507-555-1000", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Account confirmed", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Email unverified", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Email verified", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Reset temporary password", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Save new temporary password", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Save profile details", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SystemToolsPage_RendersTechnicalPlaceholderGuidance()
    {
        var cut = Render<SystemTools>();

        Assert.Contains("IT / System", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Reserved technical surface", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Technical dashboards", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    private static ClaimsPrincipal CreateUser(string userName, params string[] roles) =>
        CreateUserWithName(userName, firstName: null, lastName: null, roles);

    private static ClaimsPrincipal CreateUserWithName(
        string userName,
        string? firstName,
        string? lastName,
        params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.NameIdentifier, userName)
        };

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            claims.Add(new Claim(ClaimTypes.GivenName, firstName));
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            claims.Add(new Claim(ClaimTypes.Surname, lastName));
        }

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "README.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test output directory.");
    }

    private sealed class FakeMenuQueryService : IMenuQueryService
    {
        private static readonly Guid AppetizersSectionId = Guid.Parse("24F84594-8F35-480E-B0F3-8E605E436511");
        private static readonly Guid BurgersSectionId = Guid.Parse("1164E8D0-64EE-4CFD-BDE8-B00BC01F72F4");
        private static readonly Guid DrinksSectionId = Guid.Parse("50293894-B6D4-4E6B-B242-C225E0D0B650");

        public MenuTab SuggestedTab { get; set; } = MenuTab.Lunch;

        public Task<MenuTab> GetSuggestedPublicTabAsync(DateOnly today, TimeOnly currentTime, CancellationToken cancellationToken = default) =>
            Task.FromResult(SuggestedTab);

        public Task<IReadOnlyList<PublicHomeSpecialView>> GetHomeSpecialsAsync(DateOnly today, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<PublicHomeSpecialView> specials =
            [
                new(
                    Guid.Parse("4F2657C8-DDB4-4A69-A493-0CE49E61977D"),
                    "Monday",
                    "Monday Night Burgers",
                    "A dependable burger-night draw with fries and easy weeknight pricing.",
                    "After 5:00 PM",
                    "$11 basket special",
                    "Burgers - Menu item: Classic Hamburger",
                    today.DayOfWeek == DayOfWeek.Monday ? "Now available" : null,
                    today.DayOfWeek == DayOfWeek.Monday),
                new(
                    Guid.Parse("B0A14AF0-708E-4678-95CE-2767DE55E0A4"),
                    "Sunday",
                    "Sunday Pork Chop Dinner",
                    "A hearty end-of-week dinner special that should read as a repeatable tradition.",
                    "After 3:00 PM",
                    "$17 dinner plate",
                    "Dinner Specials",
                    today.DayOfWeek == DayOfWeek.Sunday ? "Now available" : null,
                    today.DayOfWeek == DayOfWeek.Sunday)
            ];

            return Task.FromResult(specials);
        }

        public Task<MenuManagementView> GetMenuManagementViewAsync(DateOnly today, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<MenuServiceWindowView> breakfastDays = CreateHours(
                today,
                new Dictionary<DayOfWeek, (bool IsAvailable, TimeOnly? OpensAt, TimeOnly? ClosesAt, bool ClosesNextDay)>
                {
                    [DayOfWeek.Saturday] = (true, new TimeOnly(10, 0), new TimeOnly(13, 0), false),
                    [DayOfWeek.Sunday] = (true, new TimeOnly(10, 0), new TimeOnly(13, 0), false)
                });

            IReadOnlyList<MenuServiceWindowView> lunchDays = CreateHours(
                today,
                new Dictionary<DayOfWeek, (bool IsAvailable, TimeOnly? OpensAt, TimeOnly? ClosesAt, bool ClosesNextDay)>
                {
                    [DayOfWeek.Tuesday] = (true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
                    [DayOfWeek.Wednesday] = (true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
                    [DayOfWeek.Thursday] = (true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
                    [DayOfWeek.Friday] = (true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
                    [DayOfWeek.Saturday] = (true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
                    [DayOfWeek.Sunday] = (true, new TimeOnly(11, 0), new TimeOnly(15, 0), false)
                });

            IReadOnlyList<MenuServiceWindowView> dinnerDays = CreateHours(
                today,
                new Dictionary<DayOfWeek, (bool IsAvailable, TimeOnly? OpensAt, TimeOnly? ClosesAt, bool ClosesNextDay)>
                {
                    [DayOfWeek.Monday] = (true, new TimeOnly(17, 0), new TimeOnly(20, 0), false),
                    [DayOfWeek.Tuesday] = (true, new TimeOnly(16, 0), new TimeOnly(21, 0), false),
                    [DayOfWeek.Wednesday] = (true, new TimeOnly(16, 0), new TimeOnly(21, 0), false),
                    [DayOfWeek.Thursday] = (true, new TimeOnly(16, 0), new TimeOnly(21, 0), false),
                    [DayOfWeek.Friday] = (true, new TimeOnly(16, 0), new TimeOnly(22, 0), false),
                    [DayOfWeek.Saturday] = (true, new TimeOnly(16, 0), new TimeOnly(22, 0), false),
                    [DayOfWeek.Sunday] = (true, new TimeOnly(15, 0), new TimeOnly(20, 0), false)
                });

            IReadOnlyList<MenuServiceWindowView> drinkDays = CreateHours(
                today,
                new Dictionary<DayOfWeek, (bool IsAvailable, TimeOnly? OpensAt, TimeOnly? ClosesAt, bool ClosesNextDay)>
                {
                    [DayOfWeek.Monday] = (true, new TimeOnly(16, 0), new TimeOnly(21, 0), false),
                    [DayOfWeek.Tuesday] = (true, new TimeOnly(11, 0), new TimeOnly(22, 0), false),
                    [DayOfWeek.Wednesday] = (true, new TimeOnly(11, 0), new TimeOnly(22, 0), false),
                    [DayOfWeek.Thursday] = (true, new TimeOnly(11, 0), new TimeOnly(22, 0), false),
                    [DayOfWeek.Friday] = (true, new TimeOnly(11, 0), new TimeOnly(0, 0), true),
                    [DayOfWeek.Saturday] = (true, new TimeOnly(10, 0), new TimeOnly(0, 0), true),
                    [DayOfWeek.Sunday] = (true, new TimeOnly(10, 0), new TimeOnly(21, 0), false)
                });

            IReadOnlyList<MenuSectionAdminView> sections =
            [
                MenuAdminViewFactory.Section(AppetizersSectionId, "Appetizers", MenuFamily.Food, [MenuTab.Lunch, MenuTab.Dinner], 1),
                MenuAdminViewFactory.Section(BurgersSectionId, "Burgers", MenuFamily.Food, [MenuTab.Lunch, MenuTab.Dinner], 2),
                MenuAdminViewFactory.Section(DrinksSectionId, "Cocktails", MenuFamily.Drink, [MenuTab.Drinks], 1)
            ];

            IReadOnlyList<MenuItemAdminView> items =
            [
                MenuAdminViewFactory.Item(
                    Guid.Parse("1CC159E4-C492-474B-B4B0-15274F61B23F"),
                    MenuFamily.Food,
                    "Cheese Curds",
                    "Crisp white cheddar curds with your choice of dipping sauce.",
                    1,
                    [MenuAdminViewFactory.Assignment(AppetizersSectionId, "Appetizers", 1)],
                    [MenuTab.Lunch, MenuTab.Dinner],
                    [new MenuItemPriceVariantView("Regular", 9m, 1)],
                    imagePath: "images/menu/appetizers.svg"),
                MenuAdminViewFactory.Item(
                    Guid.Parse("3C0AF95B-8976-4C46-84FB-C66E2B8B3575"),
                    MenuFamily.Food,
                    "Seasonal Soup",
                    "Cup or bowl, updated as the kitchen rotates specials.",
                    2,
                    [MenuAdminViewFactory.Assignment(AppetizersSectionId, "Appetizers", 2)],
                    [MenuTab.Lunch, MenuTab.Dinner],
                    [new MenuItemPriceVariantView("Cup", 4m, 1), new MenuItemPriceVariantView("Bowl", 6m, 2)]),
                MenuAdminViewFactory.Item(
                    Guid.Parse("816F24F6-14F3-4648-9F8A-520C17600952"),
                    MenuFamily.Food,
                    "Classic Hamburger",
                    "Fresh hand-pattied burger; add cheese if desired.",
                    1,
                    [MenuAdminViewFactory.Assignment(BurgersSectionId, "Burgers", 1)],
                    [MenuTab.Lunch, MenuTab.Dinner],
                    [new MenuItemPriceVariantView("Regular", 11m, 1)],
                    imagePath: "images/menu/burgers.svg"),
                MenuAdminViewFactory.Item(
                    Guid.Parse("8F457E31-B23E-4CB0-A32B-5754C50B19F4"),
                    MenuFamily.Food,
                    "Monday Night Burgers",
                    "A dependable burger-night draw with fries and easy weeknight pricing.",
                    1,
                    [MenuAdminViewFactory.Assignment(BurgersSectionId, "Burgers", 1)],
                    [MenuTab.Dinner],
                    [new MenuItemPriceVariantView("Regular", 11m, 1)],
                    ["Special", "Today"],
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
                        ["Today"],
                        today.DayOfWeek == DayOfWeek.Monday),
                    imagePath: "images/menu/burgers.svg")
            ];

            return Task.FromResult(
                new MenuManagementView(
                    [
                        new(MenuTab.Breakfast, "Breakfast", breakfastDays),
                        new(MenuTab.Lunch, "Lunch", lunchDays),
                        new(MenuTab.Dinner, "Dinner", dinnerDays),
                        new(MenuTab.Drinks, "Drinks", drinkDays)
                    ],
                    sections,
                    items));
        }

        public Task<PublicMenuView> GetPublicMenuAsync(MenuTab requestedTab, DateOnly today, CancellationToken cancellationToken = default)
        {
            var tabs = new[]
            {
                CreateTabLink(MenuTab.Breakfast, requestedTab, false),
                CreateTabLink(MenuTab.Lunch, requestedTab, true),
                CreateTabLink(MenuTab.Dinner, requestedTab, true),
                CreateTabLink(MenuTab.Drinks, requestedTab, false)
            };

            return requestedTab switch
            {
                MenuTab.Dinner => Task.FromResult(CreateDinnerMenu(today, tabs)),
                MenuTab.Drinks => Task.FromResult(CreateEmptyMenu(MenuTab.Drinks, tabs, today)),
                MenuTab.Breakfast => Task.FromResult(CreateEmptyMenu(MenuTab.Breakfast, tabs, today)),
                _ => Task.FromResult(CreateLunchMenu(today, tabs))
            };
        }

        private static PublicMenuView CreateLunchMenu(DateOnly today, IReadOnlyList<MenuTabLinkView> tabs)
        {
            IReadOnlyList<MenuServiceWindowView> hours = CreateHours(
                today,
                new Dictionary<DayOfWeek, (bool IsAvailable, TimeOnly? OpensAt, TimeOnly? ClosesAt, bool ClosesNextDay)>
                {
                    [DayOfWeek.Tuesday] = (true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
                    [DayOfWeek.Wednesday] = (true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
                    [DayOfWeek.Thursday] = (true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
                    [DayOfWeek.Friday] = (true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
                    [DayOfWeek.Saturday] = (true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
                    [DayOfWeek.Sunday] = (true, new TimeOnly(11, 0), new TimeOnly(15, 0), false)
                });

            IReadOnlyList<PublicMenuSectionView> sections =
            [
                new(
                    AppetizersSectionId,
                    "Appetizers",
                    "Shareables for the table.",
                    "accent-blue",
                    (IReadOnlyList<PublicMenuItemView>)
                    [
                        new(
                            Guid.Parse("A35ED7CC-E947-4BEC-8B0A-8C2B8B73BFAB"),
                            "Cheese Curds",
                            "Crisp white cheddar curds with your choice of dipping sauce.",
                            "images/menu/appetizers.svg",
                            [new MenuItemPriceVariantView("Regular", 9m, 1)],
                            [],
                            null,
                            null),
                        new(
                            Guid.Parse("B9B1D225-227D-4F91-95D0-11FC0D3C5F84"),
                            "Mini Tacos",
                            "Served with salsa and sour cream.",
                            null,
                            [new MenuItemPriceVariantView("Regular", 9m, 1)],
                            ["Coming Soon"],
                            "Expected on May 31",
                            null),
                        new(
                            Guid.Parse("4BC371B2-DB2D-4872-8D68-393129E38556"),
                            "Quesadillas",
                            "Loaded with cheese and served with salsa and sour cream.",
                            null,
                            [new MenuItemPriceVariantView("Regular", 11m, 1)],
                            ["Seasonal"],
                            "Available through Jul 10",
                            null),
                        new(
                            Guid.Parse("C68985EB-A612-4A74-8FBE-E1EDB20E4A0B"),
                            "Fish Tacos",
                            "Finished with Boom Boom sauce for a bold bar-food favorite.",
                            "images/menu/appetizers.svg",
                            [new MenuItemPriceVariantView("Regular", 10m, 1)],
                            ["Limited Time"],
                            "Available through Jun 3",
                            null)
                    ]),
                new(
                    BurgersSectionId,
                    "Burgers",
                    null,
                    "accent-magenta",
                    (IReadOnlyList<PublicMenuItemView>)
                    [
                        new(
                            Guid.Parse("7D14308C-C286-4B2D-B58C-A8F74F8DBA0A"),
                            "Classic Hamburger",
                            "Fresh hand-pattied burger; add cheese if desired.",
                            "images/menu/burgers.svg",
                            [new MenuItemPriceVariantView("Regular", 11m, 1)],
                            [],
                            null,
                            null),
                        new(
                            Guid.Parse("F4D0A309-1D35-47F6-B7A8-FB652C2B7141"),
                            "Bacon Cheeseburger",
                            "A familiar favorite with bacon and melty cheese.",
                            null,
                            [new MenuItemPriceVariantView("Regular", 13m, 1)],
                            [],
                            null,
                            null)
                    ])
            ];

            return new PublicMenuView(MenuTab.Lunch, tabs, hours, sections);
        }

        private static PublicMenuView CreateDinnerMenu(DateOnly today, IReadOnlyList<MenuTabLinkView> tabs)
        {
            IReadOnlyList<MenuServiceWindowView> hours = CreateHours(
                today,
                new Dictionary<DayOfWeek, (bool IsAvailable, TimeOnly? OpensAt, TimeOnly? ClosesAt, bool ClosesNextDay)>
                {
                    [DayOfWeek.Monday] = (true, new TimeOnly(17, 0), new TimeOnly(20, 0), false),
                    [DayOfWeek.Tuesday] = (true, new TimeOnly(16, 0), new TimeOnly(21, 0), false),
                    [DayOfWeek.Wednesday] = (true, new TimeOnly(16, 0), new TimeOnly(21, 0), false),
                    [DayOfWeek.Thursday] = (true, new TimeOnly(16, 0), new TimeOnly(21, 0), false),
                    [DayOfWeek.Friday] = (true, new TimeOnly(16, 0), new TimeOnly(22, 0), false),
                    [DayOfWeek.Saturday] = (true, new TimeOnly(16, 0), new TimeOnly(22, 0), false),
                    [DayOfWeek.Sunday] = (true, new TimeOnly(15, 0), new TimeOnly(20, 0), false)
                });

            IReadOnlyList<PublicMenuSectionView> sections =
            [
                new(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "Specials",
                    null,
                    "accent-gold",
                    (IReadOnlyList<PublicMenuItemView>)
                    [
                        new(
                            Guid.Parse("DDFFDAE0-E918-43C2-B682-AE3FD5EC50EF"),
                            "Monday Night Burgers",
                            "A dependable burger-night draw with fries and easy weeknight pricing.",
                            "images/menu/burgers.svg",
                            [new MenuItemPriceVariantView("Regular", 11m, 1)],
                            ["Special", "Today"],
                            null,
                            new MenuItemSpecialPublicView(
                                MenuItemSpecialScheduleKind.WeeklyRecurring,
                                "Monday",
                                "Every Monday",
                                "After 5:00 PM",
                                "$11 basket special",
                                today.DayOfWeek == DayOfWeek.Monday))
                    ]),
                new(
                    BurgersSectionId,
                    "Burgers",
                    null,
                    "accent-magenta",
                    (IReadOnlyList<PublicMenuItemView>)
                    [
                        new(
                            Guid.Parse("DDFFDAE0-E918-43C2-B682-AE3FD5EC50EF"),
                            "Monday Night Burgers",
                            "A dependable burger-night draw with fries and easy weeknight pricing.",
                            "images/menu/burgers.svg",
                            [new MenuItemPriceVariantView("Regular", 11m, 1)],
                            ["Special", "Today"],
                            null,
                            new MenuItemSpecialPublicView(
                                MenuItemSpecialScheduleKind.WeeklyRecurring,
                                "Monday",
                                "Every Monday",
                                "After 5:00 PM",
                                "$11 basket special",
                                today.DayOfWeek == DayOfWeek.Monday)),
                        new(
                            Guid.Parse("7D14308C-C286-4B2D-B58C-A8F74F8DBA0A"),
                            "Classic Hamburger",
                            "Fresh hand-pattied burger; add cheese if desired.",
                            "images/menu/burgers.svg",
                            [new MenuItemPriceVariantView("Regular", 11m, 1)],
                            [],
                            null,
                            null)
                    ])
            ];

            return new PublicMenuView(MenuTab.Dinner, tabs, hours, sections);
        }

        private static PublicMenuView CreateEmptyMenu(MenuTab tab, IReadOnlyList<MenuTabLinkView> tabs, DateOnly today)
        {
            IReadOnlyList<MenuServiceWindowView> hours = tab == MenuTab.Drinks
                ? CreateHours(
                    today,
                    new Dictionary<DayOfWeek, (bool IsAvailable, TimeOnly? OpensAt, TimeOnly? ClosesAt, bool ClosesNextDay)>
                    {
                        [DayOfWeek.Monday] = (true, new TimeOnly(16, 0), new TimeOnly(21, 0), false),
                        [DayOfWeek.Tuesday] = (true, new TimeOnly(11, 0), new TimeOnly(22, 0), false),
                        [DayOfWeek.Wednesday] = (true, new TimeOnly(11, 0), new TimeOnly(22, 0), false),
                        [DayOfWeek.Thursday] = (true, new TimeOnly(11, 0), new TimeOnly(22, 0), false),
                        [DayOfWeek.Friday] = (true, new TimeOnly(11, 0), new TimeOnly(0, 0), true),
                        [DayOfWeek.Saturday] = (true, new TimeOnly(10, 0), new TimeOnly(0, 0), true),
                        [DayOfWeek.Sunday] = (true, new TimeOnly(10, 0), new TimeOnly(21, 0), false)
                    })
                : CreateHours(
                    today,
                    new Dictionary<DayOfWeek, (bool IsAvailable, TimeOnly? OpensAt, TimeOnly? ClosesAt, bool ClosesNextDay)>
                    {
                        [DayOfWeek.Saturday] = (true, new TimeOnly(10, 0), new TimeOnly(13, 0), false),
                        [DayOfWeek.Sunday] = (true, new TimeOnly(10, 0), new TimeOnly(13, 0), false)
                    });

            return new PublicMenuView(tab, tabs, hours, []);
        }

        private static MenuTabLinkView CreateTabLink(MenuTab tab, MenuTab requestedTab, bool hasVisibleContent) =>
            new(tab, GetTabLabel(tab), GetQueryValue(tab), requestedTab == tab, hasVisibleContent);

        private static IReadOnlyList<MenuServiceWindowView> CreateHours(
            DateOnly today,
            IReadOnlyDictionary<DayOfWeek, (bool IsAvailable, TimeOnly? OpensAt, TimeOnly? ClosesAt, bool ClosesNextDay)> configuredDays)
        {
            return new[]
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday,
                DayOfWeek.Sunday
            }
            .Select(day =>
            {
                var config = configuredDays.TryGetValue(day, out var value)
                    ? value
                    : (false, null, null, false);

                var summary = !config.IsAvailable || config.OpensAt is null || config.ClosesAt is null
                    ? "Not served"
                    : $"{config.OpensAt.Value:h\\:mm} {(config.OpensAt.Value.Hour >= 12 ? "PM" : "AM")} - {config.ClosesAt.Value:h\\:mm} {(config.ClosesAt.Value.Hour >= 12 || config.ClosesAt.Value == TimeOnly.MinValue ? "PM" : "AM")}{(config.ClosesNextDay ? " next day" : string.Empty)}";

                return new MenuServiceWindowView(
                    day,
                    day.ToString(),
                    config.IsAvailable,
                    summary,
                    today.DayOfWeek == day,
                    config.OpensAt,
                    config.ClosesAt,
                    config.ClosesNextDay);
            })
            .ToArray();
        }

        private static string GetTabLabel(MenuTab tab) =>
            tab switch
            {
                MenuTab.Breakfast => "Breakfast",
                MenuTab.Lunch => "Lunch",
                MenuTab.Dinner => "Dinner",
                MenuTab.Drinks => "Drinks",
                _ => tab.ToString()
            };

        private static string GetQueryValue(MenuTab tab) =>
            tab switch
            {
                MenuTab.Breakfast => "breakfast",
                MenuTab.Lunch => "lunch",
                MenuTab.Dinner => "dinner",
                MenuTab.Drinks => "drinks",
                _ => "lunch"
            };
    }

    private sealed class FakeEventQueryService : IEventQueryService
    {
        public IReadOnlyList<EventOccurrenceRecord> UpcomingEvents { get; set; } =
        [
            new(
                Guid.Parse("0BE8C2B1-FE05-4518-8699-2870A9E85010"),
                "Dockside Acoustic Night",
                "An unplugged evening set that keeps the room lively without overpowering dinner service.",
                "A stripped-back live set for guests who want music and conversation at the same time.",
                "Live Music",
                null,
                new DateOnly(2026, 5, 23),
                new TimeOnly(19, 30),
                null,
                false,
                10,
                false,
                "One-time event on May 23, 2026 at 7:30 PM"),
            new(
                Guid.Parse("6D934C7A-F7D6-4758-9BF1-2679B7258C3A"),
                "Sunday Community Bingo",
                "A family-friendly Sunday event that pairs well with a relaxed lunch stop.",
                "Recurring bingo with easy daytime timing and a casual community feel.",
                "Community Night",
                null,
                new DateOnly(2026, 5, 24),
                new TimeOnly(11, 0),
                null,
                false,
                20,
                true,
                "Recurring every Sunday at 11:00 AM - next on May 24, 2026")
        ];

        public Task<IReadOnlyList<EventOccurrenceRecord>> GetUpcomingEventsAsync(
            DateTime localNow,
            int daysAhead = 30,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(UpcomingEvents);
    }

    private sealed class FakeMenuManagementService : IMenuManagementService
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

        public Task<MenuOperationResult> ReorderSectionContentAsync(
            IReadOnlyList<SaveMenuSortOrderRequest> sectionRequests,
            IReadOnlyList<SaveMenuSortOrderRequest> itemRequests,
            Guid parentSectionId,
            CancellationToken cancellationToken = default) =>
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

    private sealed class FakeHomepagePublicityService : IHomepagePublicityService
    {
        public HomepagePublicityContent? DraftContent { get; set; }

        public HomepagePublicityContent? PublishedContent { get; set; }

        public Task<HomepagePublicityAdminView> GetHomepageAdminViewAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new HomepagePublicityAdminView(DraftContent, null, PublishedContent, null));

        public Task<HomepagePublicityContent?> GetPublishedHomepageAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PublishedContent);

        public Task<HomepagePublicityOperationResult> SaveDraftAsync(
            SaveHomepagePublicityRequest request,
            CancellationToken cancellationToken = default)
        {
            DraftContent = new HomepagePublicityContent(request.Eyebrow, request.Headline, request.Summary);
            return Task.FromResult(HomepagePublicityOperationResult.Success());
        }

        public Task<HomepagePublicityOperationResult> PublishAsync(
            SaveHomepagePublicityRequest request,
            CancellationToken cancellationToken = default)
        {
            DraftContent = new HomepagePublicityContent(request.Eyebrow, request.Headline, request.Summary);
            PublishedContent = DraftContent;
            return Task.FromResult(HomepagePublicityOperationResult.Success());
        }
    }

    private sealed class EmptyDescriptionMenuQueryService : IMenuQueryService
    {
        public Task<MenuTab> GetSuggestedPublicTabAsync(DateOnly today, TimeOnly currentTime, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuTab.Lunch);

        public Task<PublicMenuView> GetPublicMenuAsync(MenuTab requestedTab, DateOnly today, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<MenuServiceWindowView> hours =
            [
                new(DayOfWeek.Monday, "Monday", true, "11:00 AM - 5:00 PM", true, new TimeOnly(11, 0), new TimeOnly(17, 0), false)
            ];

            IReadOnlyList<MenuTabLinkView> tabs =
            [
                new(MenuTab.Lunch, "Lunch", "lunch", true, true)
            ];

            IReadOnlyList<PublicMenuSectionView> sections =
            [
                new(
                    Guid.Parse("31CF1B24-8435-4D22-A7C1-C9039F21C37D"),
                    "Soft Drinks",
                    null,
                    "accent-blue",
                    (IReadOnlyList<PublicMenuItemView>)
                    [
                        new(
                            Guid.Parse("634A9095-9BBA-46CD-A409-15717A90A11E"),
                            "Pepsi",
                            string.Empty,
                            null,
                            [new MenuItemPriceVariantView("Regular", 3m, 1)],
                            [],
                            null,
                            null)
                    ])
            ];

            return Task.FromResult(new PublicMenuView(MenuTab.Lunch, tabs, hours, sections));
        }

        public Task<IReadOnlyList<PublicHomeSpecialView>> GetHomeSpecialsAsync(DateOnly today, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublicHomeSpecialView>>([]);

        public Task<MenuManagementView> GetMenuManagementViewAsync(DateOnly today, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class NestedChildSectionMenuQueryService : IMenuQueryService
    {
        public Task<MenuTab> GetSuggestedPublicTabAsync(DateOnly today, TimeOnly currentTime, CancellationToken cancellationToken = default) =>
            Task.FromResult(MenuTab.Breakfast);

        public Task<PublicMenuView> GetPublicMenuAsync(MenuTab requestedTab, DateOnly today, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<MenuServiceWindowView> hours =
            [
                new(DayOfWeek.Saturday, "Saturday", true, "9:00 AM - 12:00 PM", today.DayOfWeek == DayOfWeek.Saturday, new TimeOnly(9, 0), new TimeOnly(12, 0), false),
                new(DayOfWeek.Sunday, "Sunday", true, "9:00 AM - 12:00 PM", today.DayOfWeek == DayOfWeek.Sunday, new TimeOnly(9, 0), new TimeOnly(12, 0), false)
            ];

            IReadOnlyList<MenuTabLinkView> tabs =
            [
                new(MenuTab.Breakfast, "Breakfast", "breakfast", true, true),
                new(MenuTab.Lunch, "Lunch", "lunch", false, true),
                new(MenuTab.Dinner, "Dinner", "dinner", false, true),
                new(MenuTab.Drinks, "Drinks", "drinks", false, false)
            ];

            var omeletItem = new PublicMenuItemView(
                Guid.Parse("5EF4D676-2D94-430D-87BD-1D86073AF823"),
                "Ham & Cheese",
                "Ham and American cheese.",
                null,
                [new MenuItemPriceVariantView("Regular", 13m, 1)],
                [],
                null,
                null);

            IReadOnlyList<PublicMenuSectionView> sections =
            [
                new(
                    Guid.Parse("E8A8A54F-D40D-4A4F-80D0-093311B9C2F2"),
                    "Breakfast",
                    "Includes choice of Bloody Mary or Screwdriver",
                    "accent-gold",
                    [
                        new PublicMenuSectionEntryView(
                            10,
                            null,
                            new PublicMenuChildSectionView(
                                Guid.Parse("8792097D-2FC1-4F5B-9915-1AF5BE4E4E56"),
                                "Omelets",
                                "Choice of breakfast potato or hashbrowns.",
                                [omeletItem]))
                    ])
            ];

            return Task.FromResult(new PublicMenuView(MenuTab.Breakfast, tabs, hours, sections));
        }

        public Task<IReadOnlyList<PublicHomeSpecialView>> GetHomeSpecialsAsync(DateOnly today, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublicHomeSpecialView>>([]);

        public Task<MenuManagementView> GetMenuManagementViewAsync(DateOnly today, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FixedTimeProvider(DateTimeOffset localNow) : TimeProvider
    {
        private readonly TimeZoneInfo localTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "Test/Local",
            localNow.Offset,
            "Test/Local",
            "Test/Local");

        private DateTimeOffset currentLocalNow = localNow;

        public override TimeZoneInfo LocalTimeZone => localTimeZone;

        public override DateTimeOffset GetUtcNow() => currentLocalNow.ToUniversalTime();

        public void SetLocalNow(DateTimeOffset localNow) => currentLocalNow = localNow;
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
        public async Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            IEnumerable<IAuthorizationRequirement> requirements)
        {
            return await Task.FromResult(Evaluate(user, requirements));
        }

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

    private sealed class FakeIdentityAdministrationService : IIdentityAdministrationService
    {
        public IReadOnlyList<ManagedUserSummary> Users { get; init; } = [];

        public Task<IReadOnlyList<ManagedUserSummary>> GetUsersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Users);

        public Task<BootstrapSecurityOverview> GetSecurityOverviewAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new BootstrapSecurityOverview(1, 1, 1));

        public Task<IdentityOperationResult> CreateUserAsync(CreateManagedUserRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityOperationResult.Success());

        public Task<IdentityOperationResult> UpdateUserProfileAsync(UpdateManagedUserProfileRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityOperationResult.Success());

        public Task<IdentityOperationResult> ResetUserPasswordAsync(ResetManagedUserPasswordRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityOperationResult.Success());

        public Task<IdentityOperationResult> AddRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityOperationResult.Success());

        public Task<IdentityOperationResult> RemoveRoleAsync(string userId, string roleName, string actingUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityOperationResult.Success());

        public Task<IdentityOperationResult> SetAccountConfirmedAsync(string userId, bool accountConfirmed, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityOperationResult.Success());
    }
}
