using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Domain.Tests.Identity;

public sealed class AuthorizationBuilderExtensionsTests
{
    [Theory]
    [InlineData(ApplicationPolicies.AdminAccess, ApplicationRoles.Admin, true)]
    [InlineData(ApplicationPolicies.EventManagement, ApplicationRoles.Admin, true)]
    [InlineData(ApplicationPolicies.MenuManagement, ApplicationRoles.Admin, true)]
    [InlineData(ApplicationPolicies.ITAccess, ApplicationRoles.Admin, false)]
    [InlineData(ApplicationPolicies.EventManagement, ApplicationRoles.EventManager, true)]
    [InlineData(ApplicationPolicies.MenuManagement, ApplicationRoles.MenuManager, true)]
    [InlineData(ApplicationPolicies.ITAccess, ApplicationRoles.It, true)]
    [InlineData(ApplicationPolicies.AdminAccess, ApplicationRoles.EventManager, false)]
    [InlineData(ApplicationPolicies.MenuManagement, ApplicationRoles.EventManager, false)]
    [InlineData(ApplicationPolicies.EventManagement, ApplicationRoles.MenuManager, false)]
    [InlineData(ApplicationPolicies.AdminAccess, ApplicationRoles.It, false)]
    [InlineData(ApplicationPolicies.ITAccess, ApplicationRoles.EventManager, false)]
    public async Task AddAnchorAuthorizationPolicies_applies_expected_role_access(
        string policyName,
        string roleName,
        bool shouldSucceed)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationBuilder()
            .AddAnchorAuthorizationPolicies();

        await using var serviceProvider = services.BuildServiceProvider();
        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        var principal = CreatePrincipal(roleName);

        var result = await authorizationService.AuthorizeAsync(principal, resource: null, policyName);

        Assert.Equal(shouldSucceed, result.Succeeded);
    }

    private static ClaimsPrincipal CreatePrincipal(string roleName) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-1"),
            new Claim(ClaimTypes.Role, roleName)
        ], "TestAuth"));
}
