using Anchor.Domain.Identity;
using Anchor.Web.Components.Account.Shared;
using Anchor.Domain.Identity.Users;
using Anchor.Web.Components.Layout;
using Anchor.Web.Components.Pages;
using Anchor.Web.Components.Pages.Admin;
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
        Assert.Contains("Staff Access", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Log In", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(">Register<", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Event Editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("site-header__nav-stack is-open", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("data-anchor-theme-toggle=\"true\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-anchor-menu-toggle=\"true\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aria-expanded=\"false\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-enhance-nav=\"false\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
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

        Assert.Contains("Staff Tools", cut.Markup, StringComparison.OrdinalIgnoreCase);
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
    public void HomePage_RendersGuestWelcomeAndPlaceholderCarousel()
    {
        var cut = Render<Home>();

        Assert.Contains("Welcome aboard The Anchor.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exterior Photo Placement", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Browse the Menu", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-anchor-carousel=\"true\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(5, cut.FindAll("[data-anchor-carousel-slide]").Count);
        Assert.Equal(5, cut.FindAll("[data-anchor-carousel-to]").Count);
        Assert.Contains("data-anchor-carousel-prev", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-anchor-carousel-next", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/images/home-carousel/live-music-stage.jpg", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Patio nights with a crowd", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Open-air bar seating area with bright stools and a long stone counter.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Monday Night Burgers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sunday Pork Chop Dinner", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Thursday Trivia", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Summer Kickoff Patio Party", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-enhance-nav=\"false\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
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
    public void MenuPage_RendersMenuSectionsFromMockupData()
    {
        var cut = Render<Menu>();

        Assert.Contains("Menu mockup", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Appetizers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Burgers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Monday Night Burgers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sunday Pork Chop Dinner", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(cut.FindAll(".menu-item__image"));
        Assert.NotEmpty(cut.FindAll(".menu-item--text-only"));
        Assert.Contains("Coming Soon", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Seasonal", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Limited Time Special", cut.Markup, StringComparison.OrdinalIgnoreCase);
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

        Assert.Contains("Menu editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Item editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Section preview", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Menu image (optional)", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Optional image", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Choose an existing section or type a new section name to create it", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Offer start date", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, cut.FindAll("input[type='date']").Count);
        Assert.Contains("Recurring specials", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Day of week", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Monday Night Burgers", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Seasonal item?", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("without an end date is not treated as seasonal or limited time", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AboutAdminPage_RendersContentBlockEditor()
    {
        var cut = Render<AboutAdmin>();

        Assert.Contains("Publicity editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Publicity content editor", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Content Blocks", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Current publicity sections", cut.Markup, StringComparison.OrdinalIgnoreCase);
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
