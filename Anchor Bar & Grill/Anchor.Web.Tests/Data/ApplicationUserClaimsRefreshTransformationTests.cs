using System.Security.Claims;
using Anchor.Domain.Identity;
using Anchor.Web.Data;
using Anchor.Web.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Anchor.Web.Tests.Data;

public sealed class ApplicationUserClaimsRefreshTransformationTests
{
    [Fact]
    public async Task TransformAsync_returns_unauthenticated_principal_without_changes()
    {
        await using var identityContext = await SqliteIdentityTestContext.CreateAsync();
        var factory = new ApplicationUserClaimsPrincipalFactory(
            identityContext.UserManager,
            identityContext.RoleManager,
            Options.Create(new IdentityOptions()));
        var transformation = new ApplicationUserClaimsRefreshTransformation(identityContext.UserManager, factory);
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var transformed = await transformation.TransformAsync(principal);

        Assert.Same(principal, transformed);
    }

    [Fact]
    public async Task TransformAsync_rebuilds_principal_from_current_database_roles_and_flags()
    {
        await using var identityContext = await SqliteIdentityTestContext.CreateAsync();
        var factory = new ApplicationUserClaimsPrincipalFactory(
            identityContext.UserManager,
            identityContext.RoleManager,
            Options.Create(new IdentityOptions()));
        var transformation = new ApplicationUserClaimsRefreshTransformation(identityContext.UserManager, factory);

        Assert.True((await identityContext.RoleManager.CreateAsync(new IdentityRole(ApplicationRoles.Admin))).Succeeded);
        Assert.True((await identityContext.RoleManager.CreateAsync(new IdentityRole(ApplicationRoles.MenuManager))).Succeeded);

        var user = new ApplicationUser
        {
            UserName = "refresh@anchor.test",
            Email = "refresh@anchor.test",
            FirstName = "Refresh",
            LastName = "Captain",
            MustChangePassword = true
        };

        Assert.True((await identityContext.UserManager.CreateAsync(user, "Password1!")).Succeeded);
        Assert.True((await identityContext.UserManager.AddToRoleAsync(user, ApplicationRoles.Admin)).Succeeded);

        var stalePrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
                    new Claim(ClaimTypes.Role, ApplicationRoles.MenuManager)
                ],
                IdentityConstants.ApplicationScheme));

        var transformed = await transformation.TransformAsync(stalePrincipal);

        Assert.True(transformed.Identity?.IsAuthenticated);
        Assert.True(transformed.IsInRole(ApplicationRoles.Admin));
        Assert.False(transformed.IsInRole(ApplicationRoles.MenuManager));
        Assert.Contains(
            transformed.Claims,
            claim => claim.Type == ClaimTypes.GivenName
                && string.Equals(claim.Value, "Refresh", StringComparison.Ordinal));
        Assert.Contains(
            transformed.Claims,
            claim => claim.Type == ClaimTypes.Surname
                && string.Equals(claim.Value, "Captain", StringComparison.Ordinal));
        Assert.Contains(
            transformed.Claims,
            claim => claim.Type == ApplicationClaimTypes.MustChangePassword
                && string.Equals(claim.Value, bool.TrueString, StringComparison.OrdinalIgnoreCase));
    }
}
