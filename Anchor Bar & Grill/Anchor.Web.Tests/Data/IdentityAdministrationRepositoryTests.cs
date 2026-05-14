using Anchor.Domain.Identity;
using Anchor.Web.Data;
using Anchor.Web.Tests.Support;

namespace Anchor.Web.Tests.Data;

public sealed class IdentityAdministrationRepositoryTests
{
    [Fact]
    public async Task GetUsersAsync_returns_user_state_and_role_assignments()
    {
        await using var identityContext = await SqliteIdentityTestContext.CreateAsync();
        var repository = new IdentityAdministrationRepository(identityContext.DbContext, identityContext.UserManager);

        var adminRole = new Microsoft.AspNetCore.Identity.IdentityRole(ApplicationRoles.Admin);
        var menuRole = new Microsoft.AspNetCore.Identity.IdentityRole(ApplicationRoles.MenuManager);
        Assert.True((await identityContext.RoleManager.CreateAsync(adminRole)).Succeeded);
        Assert.True((await identityContext.RoleManager.CreateAsync(menuRole)).Succeeded);

        var alphaUser = new ApplicationUser
        {
            UserName = "alpha@anchor.test",
            Email = "alpha@anchor.test",
            FirstName = "Alpha",
            LastName = "Captain",
            PhoneNumber = "507-555-0001",
            EmailConfirmed = true,
            IsBootstrapAccount = true,
            MustChangePassword = true
        };
        var betaUser = new ApplicationUser
        {
            UserName = "beta@anchor.test",
            Email = "beta@anchor.test",
            EmailConfirmed = false
        };

        Assert.True((await identityContext.UserManager.CreateAsync(alphaUser, "Password1!")).Succeeded);
        Assert.True((await identityContext.UserManager.CreateAsync(betaUser, "Password1!")).Succeeded);
        Assert.True((await identityContext.UserManager.AddToRoleAsync(alphaUser, ApplicationRoles.Admin)).Succeeded);
        Assert.True((await identityContext.UserManager.AddToRoleAsync(alphaUser, ApplicationRoles.MenuManager)).Succeeded);

        var users = await repository.GetUsersAsync();

        Assert.Collection(users,
            first =>
            {
                Assert.Equal("alpha@anchor.test", first.Email);
                Assert.Equal("Alpha", first.FirstName);
                Assert.Equal("Captain", first.LastName);
                Assert.Equal("507-555-0001", first.PhoneNumber);
                Assert.True(first.EmailConfirmed);
                Assert.True(first.IsBootstrapAccount);
                Assert.True(first.MustChangePassword);
                Assert.Equal([ApplicationRoles.Admin, ApplicationRoles.MenuManager], first.Roles);
            },
            second =>
            {
                Assert.Equal("beta@anchor.test", second.Email);
                Assert.Null(second.FirstName);
                Assert.Null(second.LastName);
                Assert.Null(second.PhoneNumber);
                Assert.False(second.EmailConfirmed);
                Assert.Empty(second.Roles);
            });
    }

    [Fact]
    public async Task AddRoleAsync_and_RemoveRoleAsync_are_idempotent()
    {
        await using var identityContext = await SqliteIdentityTestContext.CreateAsync();
        var repository = new IdentityAdministrationRepository(identityContext.DbContext, identityContext.UserManager);

        Assert.True((await identityContext.RoleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(ApplicationRoles.EventManager))).Succeeded);
        var user = new ApplicationUser { UserName = "staff@anchor.test", Email = "staff@anchor.test" };
        Assert.True((await identityContext.UserManager.CreateAsync(user, "Password1!")).Succeeded);

        var addFirst = await repository.AddRoleAsync(user.Id, ApplicationRoles.EventManager);
        var addSecond = await repository.AddRoleAsync(user.Id, ApplicationRoles.EventManager);
        var removeFirst = await repository.RemoveRoleAsync(user.Id, ApplicationRoles.EventManager);
        var removeSecond = await repository.RemoveRoleAsync(user.Id, ApplicationRoles.EventManager);

        Assert.True(addFirst.Succeeded);
        Assert.True(addSecond.Succeeded);
        Assert.True(removeFirst.Succeeded);
        Assert.True(removeSecond.Succeeded);
        Assert.False(await identityContext.UserManager.IsInRoleAsync(user, ApplicationRoles.EventManager));
    }

    [Fact]
    public async Task SetEmailConfirmedAsync_updates_confirmation_state()
    {
        await using var identityContext = await SqliteIdentityTestContext.CreateAsync();
        var repository = new IdentityAdministrationRepository(identityContext.DbContext, identityContext.UserManager);

        var user = new ApplicationUser
        {
            UserName = "pending@anchor.test",
            Email = "pending@anchor.test",
            EmailConfirmed = false
        };
        Assert.True((await identityContext.UserManager.CreateAsync(user, "Password1!")).Succeeded);

        var result = await repository.SetEmailConfirmedAsync(user.Id, true);
        var refreshedUser = await identityContext.UserManager.FindByIdAsync(user.Id);

        Assert.True(result.Succeeded);
        Assert.NotNull(refreshedUser);
        Assert.True(refreshedUser.EmailConfirmed);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_updates_name_and_phone_details()
    {
        await using var identityContext = await SqliteIdentityTestContext.CreateAsync();
        var repository = new IdentityAdministrationRepository(identityContext.DbContext, identityContext.UserManager);

        var user = new ApplicationUser
        {
            UserName = "profile@anchor.test",
            Email = "profile@anchor.test",
            PhoneNumber = "507-555-0000",
            PhoneNumberConfirmed = true
        };
        Assert.True((await identityContext.UserManager.CreateAsync(user, "Password1!")).Succeeded);

        var result = await repository.UpdateUserProfileAsync(
            new Anchor.Domain.Identity.Users.UpdateManagedUserProfileRequest(
                user.Id,
                "Harbor",
                "Manager",
                "507-555-1212"));

        var refreshedUser = await identityContext.UserManager.FindByIdAsync(user.Id);

        Assert.True(result.Succeeded);
        Assert.NotNull(refreshedUser);
        Assert.Equal("Harbor", refreshedUser.FirstName);
        Assert.Equal("Manager", refreshedUser.LastName);
        Assert.Equal("507-555-1212", refreshedUser.PhoneNumber);
        Assert.False(refreshedUser.PhoneNumberConfirmed);
    }

    [Fact]
    public async Task CreateUserAsync_creates_confirmed_user_that_must_rotate_password()
    {
        await using var identityContext = await SqliteIdentityTestContext.CreateAsync();
        var repository = new IdentityAdministrationRepository(identityContext.DbContext, identityContext.UserManager);

        var result = await repository.CreateUserAsync(
            new Anchor.Domain.Identity.Users.CreateManagedUserRequest(
                "newstaff@anchor.test",
                "Password1!",
                true));

        var createdUser = await identityContext.UserManager.FindByEmailAsync("newstaff@anchor.test");

        Assert.True(result.Succeeded);
        Assert.NotNull(createdUser);
        Assert.True(createdUser.EmailConfirmed);
        Assert.True(createdUser.MustChangePassword);
        Assert.False(createdUser.IsBootstrapAccount);
    }
}
