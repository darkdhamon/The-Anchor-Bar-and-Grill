using System.Security.Claims;
using Anchor.Domain.Identity;
using Anchor.Domain.Publicity;
using Anchor.Web.Components.Pages.Admin;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Web.Tests.Components.Pages.Admin;

public sealed class PublicityAdminTests : BunitContext
{
    private readonly TestAuthenticationStateProvider authStateProvider;
    private readonly FakeHomepagePublicityService publicityService;

    public PublicityAdminTests()
    {
        Services.AddLogging();
        Services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(ApplicationPolicies.AdminAccess, policy => policy.RequireRole(ApplicationRoles.Admin));
        });
        Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
        authStateProvider = new TestAuthenticationStateProvider();
        Services.AddSingleton<AuthenticationStateProvider>(authStateProvider);
        Services.AddCascadingAuthenticationState();
        publicityService = new FakeHomepagePublicityService();
        Services.AddSingleton<IHomepagePublicityService>(publicityService);

        authStateProvider.SetUser(new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "admin@anchor.test"),
            new Claim(ClaimTypes.Role, ApplicationRoles.Admin)
        ], "TestAuth")));
    }

    [Fact]
    public void PublicityAdminHome_SaveDraft_updates_the_editor_state()
    {
        var cut = Render<PublicityAdminHome>();

        cut.Find("[id='HomepagePublicity.Eyebrow']").Change("Weekend Welcome");
        cut.Find("[id='HomepagePublicity.Headline']").Change("Fresh weekend copy");
        cut.Find("[id='HomepagePublicity.Summary']").Change("Guests should see the updated homepage summary after publish.");
        cut.FindAll("button").Single(button => button.TextContent.Contains("Save draft", StringComparison.OrdinalIgnoreCase)).Click();

        Assert.Equal("Fresh weekend copy", publicityService.DraftContent?.Headline);
        Assert.Contains("Homepage draft saved", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicityAdminHome_UsesCanonicalRouteAndMarksHomepageSectionActive()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/admin/publicity");

        var cut = Render<PublicityAdminHome>();
        var links = cut.FindAll(".publicity-shell__link");

        Assert.Equal("/admin/publicity", links[0].GetAttribute("href"));
        Assert.Contains("active", links[0].ClassList);
        Assert.DoesNotContain("active", links[1].ClassList);
        Assert.Contains("No draft saved yet", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Nothing published yet", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PublicityAdminHome_RendersSavedTimesInUtc()
    {
        publicityService.DraftContent = new HomepagePublicityContent("Weekend", "Draft headline", "Draft summary");
        publicityService.PublishedContent = new HomepagePublicityContent("Published", "Published headline", "Published summary");
        publicityService.DraftUpdatedAtUtc = new DateTimeOffset(2026, 5, 25, 10, 30, 0, TimeSpan.FromHours(-5));
        publicityService.PublishedUpdatedAtUtc = new DateTimeOffset(2026, 5, 24, 21, 0, 0, TimeSpan.FromHours(2));

        var cut = Render<PublicityAdminHome>();

        Assert.Contains("May 25, 2026 at 3:30 PM UTC", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("May 24, 2026 at 7:00 PM UTC", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicityAdminAbout_renders_placeholder_navigation()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/admin/publicity/about");

        var cut = Render<PublicityAdminAbout>();
        var links = cut.FindAll(".publicity-shell__link");

        Assert.Contains("About page placeholder", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Homepage intro", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Coming soon", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("/admin/publicity", links[0].GetAttribute("href"));
        Assert.DoesNotContain("active", links[0].ClassList);
        Assert.Contains("active", links[1].ClassList);
    }

    private sealed class FakeHomepagePublicityService : IHomepagePublicityService
    {
        public HomepagePublicityContent? DraftContent { get; set; }

        public HomepagePublicityContent? PublishedContent { get; set; }

        public DateTimeOffset? DraftUpdatedAtUtc { get; set; }

        public DateTimeOffset? PublishedUpdatedAtUtc { get; set; }

        public Task<HomepagePublicityAdminView> GetHomepageAdminViewAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new HomepagePublicityAdminView(DraftContent, DraftUpdatedAtUtc, PublishedContent, PublishedUpdatedAtUtc));

        public Task<HomepagePublicityContent?> GetPublishedHomepageAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PublishedContent);

        public Task<HomepagePublicityOperationResult> SaveDraftAsync(SaveHomepagePublicityRequest request, CancellationToken cancellationToken = default)
        {
            DraftContent = new HomepagePublicityContent(request.Eyebrow, request.Headline, request.Summary);
            DraftUpdatedAtUtc = DateTimeOffset.UtcNow;
            return Task.FromResult(HomepagePublicityOperationResult.Success());
        }

        public Task<HomepagePublicityOperationResult> PublishAsync(SaveHomepagePublicityRequest request, CancellationToken cancellationToken = default)
        {
            PublishedContent = new HomepagePublicityContent(request.Eyebrow, request.Headline, request.Summary);
            DraftContent = PublishedContent;
            DraftUpdatedAtUtc = DateTimeOffset.UtcNow;
            PublishedUpdatedAtUtc = DraftUpdatedAtUtc;
            return Task.FromResult(HomepagePublicityOperationResult.Success());
        }
    }

    private sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
    {
        private AuthenticationState authenticationState = new(new ClaimsPrincipal(new ClaimsIdentity()));

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(authenticationState);

        public void SetUser(ClaimsPrincipal user)
        {
            authenticationState = new AuthenticationState(user);
            NotifyAuthenticationStateChanged(Task.FromResult(authenticationState));
        }
    }

    private sealed class TestAuthorizationService(IAuthorizationPolicyProvider policyProvider) : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements) =>
            Task.FromResult(Evaluate(user, requirements));

        public async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
        {
            var policy = await policyProvider.GetPolicyAsync(policyName);
            return policy is null ? AuthorizationResult.Failed() : Evaluate(user, policy.Requirements);
        }

        private static AuthorizationResult Evaluate(ClaimsPrincipal user, IEnumerable<IAuthorizationRequirement> requirements)
        {
            foreach (var requirement in requirements)
            {
                switch (requirement)
                {
                    case DenyAnonymousAuthorizationRequirement when user.Identity?.IsAuthenticated != true:
                        return AuthorizationResult.Failed();
                    case RolesAuthorizationRequirement rolesRequirement when !rolesRequirement.AllowedRoles.Any(user.IsInRole):
                        return AuthorizationResult.Failed();
                }
            }

            return AuthorizationResult.Success();
        }
    }
}
