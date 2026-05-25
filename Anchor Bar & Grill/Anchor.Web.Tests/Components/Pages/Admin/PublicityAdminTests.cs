using System.Security.Claims;
using Anchor.Domain.Identity;
using Anchor.Domain.Publicity;
using Anchor.Web.Components.Pages.Admin;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
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
    public void PublicityAdminAbout_renders_placeholder_navigation()
    {
        var cut = Render<PublicityAdminAbout>();

        Assert.Contains("About page placeholder", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Homepage intro", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Coming soon", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeHomepagePublicityService : IHomepagePublicityService
    {
        public HomepagePublicityContent? DraftContent { get; private set; }

        public HomepagePublicityContent? PublishedContent { get; private set; }

        public Task<HomepagePublicityAdminView> GetHomepageAdminViewAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new HomepagePublicityAdminView(DraftContent, null, PublishedContent, null));

        public Task<HomepagePublicityContent?> GetPublishedHomepageAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(PublishedContent);

        public Task<HomepagePublicityOperationResult> SaveDraftAsync(SaveHomepagePublicityRequest request, CancellationToken cancellationToken = default)
        {
            DraftContent = new HomepagePublicityContent(request.Eyebrow, request.Headline, request.Summary);
            return Task.FromResult(HomepagePublicityOperationResult.Success());
        }

        public Task<HomepagePublicityOperationResult> PublishAsync(SaveHomepagePublicityRequest request, CancellationToken cancellationToken = default)
        {
            PublishedContent = new HomepagePublicityContent(request.Eyebrow, request.Headline, request.Summary);
            DraftContent = PublishedContent;
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
