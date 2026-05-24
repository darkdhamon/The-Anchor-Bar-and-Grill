using System.Security.Claims;
using Anchor.Domain.Identity;
using Anchor.Web.Components.Account.Pages.Manage;
using Anchor.Web.Components.Account.Shared;
using Anchor.Web.Components.Pages;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Web.Tests.Account;

public sealed class RedirectComponentsTests : BunitContext
{
    private readonly TestAuthenticationStateProvider authenticationStateProvider = new();

    public RedirectComponentsTests()
    {
        Services.AddSingleton<AuthenticationStateProvider>(authenticationStateProvider);
        Services.AddCascadingAuthenticationState();
    }

    [Fact]
    public void RedirectToLogin_NavigatesToLoginWithEncodedReturnUrl()
    {
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("http://localhost/admin/users?filter=pending approval");

        Render<RedirectToLogin>();

        Assert.Equal(
            "http://localhost/Account/Login?returnUrl=http%3A%2F%2Flocalhost%2Fadmin%2Fusers%3Ffilter%3Dpending%20approval",
            navigationManager.Uri);
    }

    [Fact]
    public void ForcePasswordChangeRedirect_NavigatesAuthenticatedUsersWhoMustChangePassword()
    {
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        authenticationStateProvider.SetUser(CreateUser(mustChangePassword: true));

        Render<ForcePasswordChangeRedirect>(parameters => parameters
            .Add(component => component.RouteData, new RouteData(typeof(Home), new Dictionary<string, object?>())));

        Assert.Equal(
            "http://localhost/Account/Manage/ChangePassword?forced=true",
            navigationManager.Uri);
    }

    [Fact]
    public void ForcePasswordChangeRedirect_DoesNotNavigateAnonymousUsers()
    {
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        authenticationStateProvider.SetUser(new ClaimsPrincipal(new ClaimsIdentity()));

        Render<ForcePasswordChangeRedirect>(parameters => parameters
            .Add(component => component.RouteData, new RouteData(typeof(Home), new Dictionary<string, object?>())));

        Assert.Equal("http://localhost/", navigationManager.Uri);
    }

    [Fact]
    public void ForcePasswordChangeRedirect_DoesNotNavigateWhenAlreadyOnChangePasswordPage()
    {
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        authenticationStateProvider.SetUser(CreateUser(mustChangePassword: true));

        Render<ForcePasswordChangeRedirect>(parameters => parameters
            .Add(component => component.RouteData, new RouteData(typeof(ChangePassword), new Dictionary<string, object?>())));

        Assert.Equal("http://localhost/", navigationManager.Uri);
    }

    private static ClaimsPrincipal CreateUser(bool mustChangePassword)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-1"),
            new(ClaimTypes.Name, "captain@anchor.test")
        };

        if (mustChangePassword)
        {
            claims.Add(new Claim(ApplicationClaimTypes.MustChangePassword, bool.TrueString));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"));
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
}
