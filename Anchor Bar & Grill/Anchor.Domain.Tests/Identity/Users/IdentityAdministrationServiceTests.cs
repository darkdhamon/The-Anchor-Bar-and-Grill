using Anchor.Domain.Identity.Users;

namespace Anchor.Domain.Tests.Identity.Users;

public sealed class IdentityAdministrationServiceTests
{
    [Fact]
    public async Task GetSecurityOverviewAsync_returns_current_counts()
    {
        var repository = new FakeIdentityAdministrationRepository
        {
            AdminCount = 2,
            ItCount = 1,
            BootstrapCount = 1
        };
        var service = new IdentityAdministrationService(repository);

        var overview = await service.GetSecurityOverviewAsync();

        Assert.Equal(2, overview.AdminUserCount);
        Assert.Equal(1, overview.ItUserCount);
        Assert.Equal(1, overview.BootstrapAccountCount);
        Assert.True(overview.HasMinimumCoverage);
    }

    [Fact]
    public async Task AddRoleAsync_rejects_unknown_roles()
    {
        var repository = new FakeIdentityAdministrationRepository();
        var service = new IdentityAdministrationService(repository);

        var result = await service.AddRoleAsync("user-1", "UnknownRole");

        Assert.False(result.Succeeded);
        Assert.Contains("not a managed application role", result.Errors.Single(), StringComparison.Ordinal);
        Assert.Null(repository.LastAddedRole);
    }

    [Fact]
    public async Task CreateUserAsync_rejects_blank_email()
    {
        var repository = new FakeIdentityAdministrationRepository();
        var service = new IdentityAdministrationService(repository);

        var result = await service.CreateUserAsync(new CreateManagedUserRequest("   ", "Password1!", false));

        Assert.False(result.Succeeded);
        Assert.Contains("email address is required", result.Errors.Single(), StringComparison.OrdinalIgnoreCase);
        Assert.Null(repository.LastCreatedUser);
    }

    [Fact]
    public async Task CreateUserAsync_trims_email_and_password_before_persisting()
    {
        var repository = new FakeIdentityAdministrationRepository();
        var service = new IdentityAdministrationService(repository);

        var result = await service.CreateUserAsync(new CreateManagedUserRequest(" staff@anchor.test ", " Password1! ", true));

        Assert.True(result.Succeeded);
        Assert.NotNull(repository.LastCreatedUser);
        Assert.Equal("staff@anchor.test", repository.LastCreatedUser.Email);
        Assert.Equal("Password1!", repository.LastCreatedUser.TemporaryPassword);
        Assert.True(repository.LastCreatedUser.EmailConfirmed);
    }

    [Fact]
    public async Task RemoveRoleAsync_blocks_removal_of_the_last_admin()
    {
        var repository = new FakeIdentityAdministrationRepository
        {
            AdminCount = 1,
            Users =
            [
                new ManagedUserSummary("user-1", "admin@anchor.test", true, false, false, [ApplicationRoles.Admin])
            ]
        };
        var service = new IdentityAdministrationService(repository);

        var result = await service.RemoveRoleAsync("user-1", ApplicationRoles.Admin, "user-2");

        Assert.False(result.Succeeded);
        Assert.Contains("last Admin", result.Errors.Single(), StringComparison.Ordinal);
        Assert.Null(repository.LastRemovedRole);
    }

    [Fact]
    public async Task RemoveRoleAsync_blocks_admin_self_removal_even_when_other_admins_exist()
    {
        var repository = new FakeIdentityAdministrationRepository
        {
            AdminCount = 2,
            Users =
            [
                new ManagedUserSummary("user-1", "admin@anchor.test", true, false, false, [ApplicationRoles.Admin])
            ]
        };
        var service = new IdentityAdministrationService(repository);

        var result = await service.RemoveRoleAsync("user-1", ApplicationRoles.Admin, "user-1");

        Assert.False(result.Succeeded);
        Assert.Contains("own account", result.Errors.Single(), StringComparison.OrdinalIgnoreCase);
        Assert.Null(repository.LastRemovedRole);
    }

    [Fact]
    public async Task RemoveRoleAsync_blocks_removal_of_the_last_it_user()
    {
        var repository = new FakeIdentityAdministrationRepository
        {
            ItCount = 1,
            Users =
            [
                new ManagedUserSummary("user-1", "it@anchor.test", true, false, false, [ApplicationRoles.It])
            ]
        };
        var service = new IdentityAdministrationService(repository);

        var result = await service.RemoveRoleAsync("user-1", ApplicationRoles.It, "user-2");

        Assert.False(result.Succeeded);
        Assert.Contains("last IT", result.Errors.Single(), StringComparison.Ordinal);
        Assert.Null(repository.LastRemovedRole);
    }

    [Fact]
    public async Task RemoveRoleAsync_allows_managed_role_removal_when_minimum_coverage_remains()
    {
        var repository = new FakeIdentityAdministrationRepository
        {
            AdminCount = 2,
            Users =
            [
                new ManagedUserSummary("user-1", "admin@anchor.test", true, false, false, [ApplicationRoles.Admin])
            ]
        };
        var service = new IdentityAdministrationService(repository);

        var result = await service.RemoveRoleAsync("user-1", ApplicationRoles.Admin, "user-2");

        Assert.True(result.Succeeded);
        Assert.Equal(ApplicationRoles.Admin, repository.LastRemovedRole);
    }

    private sealed class FakeIdentityAdministrationRepository : IIdentityAdministrationRepository
    {
        public IReadOnlyList<ManagedUserSummary> Users { get; set; } = [];

        public int AdminCount { get; set; }

        public int ItCount { get; set; }

        public int BootstrapCount { get; set; }

        public string? LastAddedRole { get; private set; }

        public CreateManagedUserRequest? LastCreatedUser { get; private set; }

        public string? LastRemovedRole { get; private set; }

        public Task<IReadOnlyList<ManagedUserSummary>> GetUsersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Users);

        public Task<ManagedUserSummary?> GetUserAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.SingleOrDefault(user => user.UserId == userId));

        public Task<IdentityOperationResult> CreateUserAsync(CreateManagedUserRequest request, CancellationToken cancellationToken = default)
        {
            LastCreatedUser = request;
            return Task.FromResult(IdentityOperationResult.Success());
        }

        public Task<int> CountUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default) =>
            Task.FromResult(roleName switch
            {
                ApplicationRoles.Admin => AdminCount,
                ApplicationRoles.It => ItCount,
                _ => 0
            });

        public Task<int> CountBootstrapUsersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(BootstrapCount);

        public Task<IdentityOperationResult> AddRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
        {
            LastAddedRole = roleName;
            return Task.FromResult(IdentityOperationResult.Success());
        }

        public Task<IdentityOperationResult> RemoveRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
        {
            LastRemovedRole = roleName;
            return Task.FromResult(IdentityOperationResult.Success());
        }

        public Task<IdentityOperationResult> SetEmailConfirmedAsync(string userId, bool emailConfirmed, CancellationToken cancellationToken = default) =>
            Task.FromResult(IdentityOperationResult.Success());
    }
}
