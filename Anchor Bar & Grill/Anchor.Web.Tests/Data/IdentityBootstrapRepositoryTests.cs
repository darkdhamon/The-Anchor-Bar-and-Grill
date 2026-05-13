using Anchor.Domain.Identity;
using Anchor.Domain.Identity.Bootstrap;
using Anchor.Web.Data;
using Anchor.Web.Tests.Support;

namespace Anchor.Web.Tests.Data;

public sealed class IdentityBootstrapRepositoryTests
{
    [Fact]
    public async Task EnsureBootstrapUserAsync_creates_bootstrap_user_and_assigns_roles()
    {
        await using var identityContext = await SqliteIdentityTestContext.CreateAsync();
        var repository = new IdentityBootstrapRepository(identityContext.DbContext, identityContext.UserManager, identityContext.RoleManager);

        await repository.EnsureRoleExistsAsync(ApplicationRoles.Admin);
        await repository.EnsureRoleExistsAsync(ApplicationRoles.It);

        var result = await repository.EnsureBootstrapUserAsync(new BootstrapUserSeed(
            Email: "admin@anchor.test",
            Password: "ChangeMe123!",
            EmailConfirmed: true,
            MustChangePassword: true,
            IsBootstrapAccount: true,
            Roles: [ApplicationRoles.Admin, ApplicationRoles.It]));

        var user = await identityContext.UserManager.FindByEmailAsync("admin@anchor.test");

        Assert.True(result.Succeeded);
        Assert.NotNull(user);
        Assert.True(user.EmailConfirmed);
        Assert.True(user.MustChangePassword);
        Assert.True(user.IsBootstrapAccount);
        Assert.True(await identityContext.UserManager.IsInRoleAsync(user, ApplicationRoles.Admin));
        Assert.True(await identityContext.UserManager.IsInRoleAsync(user, ApplicationRoles.It));
    }

    [Fact]
    public async Task EnsureBootstrapUserAsync_does_not_restore_must_change_password_after_follow_up_run()
    {
        await using var identityContext = await SqliteIdentityTestContext.CreateAsync();
        var repository = new IdentityBootstrapRepository(identityContext.DbContext, identityContext.UserManager, identityContext.RoleManager);

        await repository.EnsureRoleExistsAsync(ApplicationRoles.Admin);
        await repository.EnsureRoleExistsAsync(ApplicationRoles.It);

        var seed = new BootstrapUserSeed(
            Email: "admin@anchor.test",
            Password: "ChangeMe123!",
            EmailConfirmed: true,
            MustChangePassword: true,
            IsBootstrapAccount: true,
            Roles: [ApplicationRoles.Admin, ApplicationRoles.It]);

        await repository.EnsureBootstrapUserAsync(seed);

        var user = await identityContext.UserManager.FindByEmailAsync(seed.Email);
        Assert.NotNull(user);
        user.MustChangePassword = false;
        var updateResult = await identityContext.UserManager.UpdateAsync(user);
        Assert.True(updateResult.Succeeded);

        await repository.EnsureBootstrapUserAsync(seed);

        var refreshedUser = await identityContext.UserManager.FindByEmailAsync(seed.Email);
        Assert.NotNull(refreshedUser);
        Assert.False(refreshedUser.MustChangePassword);
        Assert.True(await identityContext.UserManager.IsInRoleAsync(refreshedUser, ApplicationRoles.Admin));
        Assert.True(await identityContext.UserManager.IsInRoleAsync(refreshedUser, ApplicationRoles.It));
    }

    [Fact]
    public async Task GetRoleCoverageAsync_counts_admin_and_it_users()
    {
        await using var identityContext = await SqliteIdentityTestContext.CreateAsync();
        var repository = new IdentityBootstrapRepository(identityContext.DbContext, identityContext.UserManager, identityContext.RoleManager);

        await repository.EnsureRoleExistsAsync(ApplicationRoles.Admin);
        await repository.EnsureRoleExistsAsync(ApplicationRoles.It);

        var adminUser = new ApplicationUser { UserName = "admin@anchor.test", Email = "admin@anchor.test" };
        var itUser = new ApplicationUser { UserName = "it@anchor.test", Email = "it@anchor.test" };
        Assert.True((await identityContext.UserManager.CreateAsync(adminUser, "Password1!")).Succeeded);
        Assert.True((await identityContext.UserManager.CreateAsync(itUser, "Password1!")).Succeeded);
        Assert.True((await identityContext.UserManager.AddToRoleAsync(adminUser, ApplicationRoles.Admin)).Succeeded);
        Assert.True((await identityContext.UserManager.AddToRoleAsync(itUser, ApplicationRoles.It)).Succeeded);

        var coverage = await repository.GetRoleCoverageAsync();

        Assert.Equal(1, coverage.AdminUserCount);
        Assert.Equal(1, coverage.ItUserCount);
        Assert.True(coverage.HasMinimumCoverage);
    }
}
