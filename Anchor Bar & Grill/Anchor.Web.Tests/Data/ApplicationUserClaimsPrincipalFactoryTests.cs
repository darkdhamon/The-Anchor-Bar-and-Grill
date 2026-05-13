using Anchor.Domain.Identity;
using Anchor.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Anchor.Web.Tests.Support;

namespace Anchor.Web.Tests.Data;

public sealed class ApplicationUserClaimsPrincipalFactoryTests
{
    [Fact]
    public async Task GenerateClaimsAsync_adds_must_change_password_claim_for_bootstrap_users()
    {
        await using var identityContext = await SqliteIdentityTestContext.CreateAsync();
        var factory = new ApplicationUserClaimsPrincipalFactory(
            identityContext.UserManager,
            identityContext.RoleManager,
            Options.Create(new IdentityOptions()));
        var user = new ApplicationUser
        {
            UserName = "bootstrap@anchor.test",
            Email = "bootstrap@anchor.test",
            MustChangePassword = true
        };
        Assert.True((await identityContext.UserManager.CreateAsync(user, "Password1!")).Succeeded);

        var principal = await factory.CreateAsync(user);

        Assert.Contains(principal.Claims, claim =>
            claim.Type == ApplicationClaimTypes.MustChangePassword &&
            string.Equals(claim.Value, bool.TrueString, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GenerateClaimsAsync_omits_must_change_password_claim_for_standard_users()
    {
        await using var identityContext = await SqliteIdentityTestContext.CreateAsync();
        var factory = new ApplicationUserClaimsPrincipalFactory(
            identityContext.UserManager,
            identityContext.RoleManager,
            Options.Create(new IdentityOptions()));
        var user = new ApplicationUser
        {
            UserName = "staff@anchor.test",
            Email = "staff@anchor.test",
            MustChangePassword = false
        };
        Assert.True((await identityContext.UserManager.CreateAsync(user, "Password1!")).Succeeded);

        var principal = await factory.CreateAsync(user);

        Assert.DoesNotContain(principal.Claims, claim => claim.Type == ApplicationClaimTypes.MustChangePassword);
    }
}
