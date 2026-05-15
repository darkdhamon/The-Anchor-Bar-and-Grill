using System.Security.Claims;
using Anchor.Domain.Identity.Users;
using Anchor.Web.Components.Account.Pages.Manage;
using Anchor.Web.Data;
using Anchor.Web.Tests.Support;
using Bunit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ManageIndex = Anchor.Web.Components.Account.Pages.Manage.Index;

namespace Anchor.Web.Tests.Account;

public sealed class ManageProfilePageTests : BunitContext
{
    [Fact]
    public async Task ManageProfile_renders_first_last_and_phone_fields()
    {
        await using var identityContext = await SqliteIdentityTestContext.CreateAsync();
        var user = await CreateUserAsync(identityContext, "profile@anchor.test", "Profile", "Owner", "507-555-0100");
        RegisterCommonServices(identityContext);

        var cut = Render(BuildManageProfileHost(user));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Update your profile details.", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("First name", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Last name", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Phone number", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Email</a> page", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("What this updates", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task ManageProfile_submit_updates_self_profile_through_shared_service()
    {
        await using var identityContext = await SqliteIdentityTestContext.CreateAsync();
        var user = await CreateUserAsync(identityContext, "profile@anchor.test", "Profile", "Owner", "507-555-0100");
        RegisterCommonServices(identityContext);
        var service = Services.GetRequiredService<IIdentityAdministrationService>() as FakeIdentityAdministrationService;
        var signInManager = Services.GetRequiredService<SignInManager<ApplicationUser>>() as FakeSignInManager;

        var cut = Render(BuildManageProfileHost(user));
        cut.WaitForAssertion(() => Assert.Contains("Save profile", cut.Markup, StringComparison.OrdinalIgnoreCase));

        cut.Find("input[id='Input.FirstName']").Change("Harbor");
        cut.Find("input[id='Input.LastName']").Change("Manager");
        cut.Find("input[id='Input.PhoneNumber']").Change("507-555-1212");
        cut.Find("form").Submit();

        Assert.NotNull(service);
        Assert.NotNull(signInManager);
        Assert.NotNull(service.LastUpdatedProfile);
        Assert.Equal(user.Id, service.LastUpdatedProfile.UserId);
        Assert.Equal("Harbor", service.LastUpdatedProfile.FirstName);
        Assert.Equal("Manager", service.LastUpdatedProfile.LastName);
        Assert.Equal("507-555-1212", service.LastUpdatedProfile.PhoneNumber);
        Assert.Equal(1, signInManager.RefreshCount);
    }

    [Fact]
    public async Task ManageProfile_shows_error_when_shared_service_rejects_changes()
    {
        await using var identityContext = await SqliteIdentityTestContext.CreateAsync();
        var user = await CreateUserAsync(identityContext, "profile@anchor.test", "Profile", "Owner", "507-555-0100");
        RegisterCommonServices(identityContext);
        var service = Services.GetRequiredService<IIdentityAdministrationService>() as FakeIdentityAdministrationService;
        var signInManager = Services.GetRequiredService<SignInManager<ApplicationUser>>() as FakeSignInManager;
        service!.UpdateProfileResult = IdentityOperationResult.Failure("Profile update failed.");

        var cut = Render(BuildManageProfileHost(user));
        cut.WaitForAssertion(() => Assert.Contains("Save profile", cut.Markup, StringComparison.OrdinalIgnoreCase));

        cut.Find("input[id='Input.PhoneNumber']").Change("507-555-2222");
        cut.Find("form").Submit();

        Assert.Contains("Profile update failed.", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(signInManager);
        Assert.Equal(0, signInManager.RefreshCount);
    }

    private void RegisterCommonServices(SqliteIdentityTestContext identityContext)
    {
        Services.AddLogging();
        Services.AddSingleton(identityContext.UserManager);
        Services.AddSingleton<UserManager<ApplicationUser>>(identityContext.UserManager);

        var accessor = new HttpContextAccessor();
        Services.AddSingleton<IHttpContextAccessor>(accessor);

        var signInManager = new FakeSignInManager(identityContext.UserManager, accessor);
        Services.AddSingleton(signInManager);
        Services.AddSingleton<SignInManager<ApplicationUser>>(signInManager);

        Services.AddSingleton<IIdentityAdministrationService>(new FakeIdentityAdministrationService());

        var redirectManagerType = typeof(ManageIndex).Assembly.GetType("Anchor.Web.Components.Account.IdentityRedirectManager")
            ?? throw new InvalidOperationException("Could not resolve IdentityRedirectManager type.");

        Services.AddScoped(
            redirectManagerType,
            provider => Activator.CreateInstance(redirectManagerType, provider.GetRequiredService<NavigationManager>())
                ?? throw new InvalidOperationException("Could not create IdentityRedirectManager."));
    }

    private RenderFragment BuildManageProfileHost(ApplicationUser user)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id)
                ],
                "TestAuth"));

        var accessor = Services.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = httpContext;

        return builder =>
        {
            builder.OpenComponent<CascadingValue<HttpContext>>(0);
            builder.AddAttribute(1, "Value", httpContext);
            builder.AddAttribute(2, "IsFixed", true);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent(0, typeof(ManageIndex));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        SqliteIdentityTestContext identityContext,
        string email,
        string firstName,
        string lastName,
        string phoneNumber)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber
        };

        var result = await identityContext.UserManager.CreateAsync(user, "Password1!");
        Assert.True(result.Succeeded);
        return user;
    }

    private sealed class FakeIdentityAdministrationService : IIdentityAdministrationService
    {
        public UpdateManagedUserProfileRequest? LastUpdatedProfile { get; private set; }

        public IdentityOperationResult UpdateProfileResult { get; set; } = IdentityOperationResult.Success();

        public Task<IReadOnlyList<ManagedUserSummary>> GetUsersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ManagedUserSummary>>([]);

        public Task<BootstrapSecurityOverview> GetSecurityOverviewAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new BootstrapSecurityOverview(1, 1, 1));

        public Task<IdentityOperationResult> CreateUserAsync(CreateManagedUserRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityOperationResult.Success());

        public Task<IdentityOperationResult> UpdateUserProfileAsync(UpdateManagedUserProfileRequest request, CancellationToken cancellationToken = default)
        {
            LastUpdatedProfile = request;
            return Task.FromResult(UpdateProfileResult);
        }

        public Task<IdentityOperationResult> ResetUserPasswordAsync(ResetManagedUserPasswordRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityOperationResult.Success());

        public Task<IdentityOperationResult> AddRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityOperationResult.Success());

        public Task<IdentityOperationResult> RemoveRoleAsync(string userId, string roleName, string actingUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityOperationResult.Success());

        public Task<IdentityOperationResult> SetEmailConfirmedAsync(string userId, bool emailConfirmed, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityOperationResult.Success());
    }

    private sealed class FakeSignInManager : SignInManager<ApplicationUser>
    {
        public FakeSignInManager(UserManager<ApplicationUser> userManager, IHttpContextAccessor contextAccessor)
            : base(
                userManager,
                contextAccessor,
                new FakeUserClaimsPrincipalFactory(),
                Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
                NullLogger<SignInManager<ApplicationUser>>.Instance,
                new AuthenticationSchemeProvider(Microsoft.Extensions.Options.Options.Create(new AuthenticationOptions())),
                new FakeUserConfirmation())
        {
        }

        public int RefreshCount { get; private set; }

        public override Task RefreshSignInAsync(ApplicationUser user)
        {
            RefreshCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserClaimsPrincipalFactory : IUserClaimsPrincipalFactory<ApplicationUser>
    {
        public Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
        {
            var principal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id)
                    ],
                    "TestAuth"));

            return Task.FromResult(principal);
        }
    }

    private sealed class FakeUserConfirmation : IUserConfirmation<ApplicationUser>
    {
        public Task<bool> IsConfirmedAsync(UserManager<ApplicationUser> manager, ApplicationUser user) =>
            Task.FromResult(true);
    }
}
