using Anchor.Domain.Identity.Bootstrap;
using Anchor.Domain.Identity.Configuration;
using Anchor.Domain.Identity.Users;
using Anchor.Domain.Tests.Support;

namespace Anchor.Domain.Tests.Identity.Bootstrap;

public sealed class IdentityBootstrapServiceTests
{
    [Fact]
    public async Task BootstrapAsync_ensures_roles_and_seeds_user_when_coverage_is_missing()
    {
        var repository = new FakeBootstrapRepository
        {
            Coverage = new BootstrapRoleCoverage(AdminUserCount: 0, ItUserCount: 0)
        };
        var options = new TestOptionsMonitor<AnchorIdentityOptions>(new AnchorIdentityOptions
        {
            BootstrapAdmin = new BootstrapAdminOptions
            {
                Email = "owner@anchor.test",
                Password = "Configured123!"
            }
        });
        var service = new IdentityBootstrapService(repository, options);

        await service.BootstrapAsync();

        Assert.Equal(ApplicationRoles.All, repository.EnsuredRoles);
        Assert.NotNull(repository.Seed);
        Assert.Equal("owner@anchor.test", repository.Seed!.Email);
        Assert.Equal("Configured123!", repository.Seed.Password);
        Assert.True(repository.Seed.AccountConfirmed);
        Assert.True(repository.Seed.EmailConfirmed);
        Assert.True(repository.Seed.MustChangePassword);
        Assert.True(repository.Seed.IsBootstrapAccount);
        Assert.Equal([ApplicationRoles.Admin, ApplicationRoles.It], repository.Seed.Roles);
    }

    [Fact]
    public async Task BootstrapAsync_uses_hardcoded_fallback_credentials_when_configuration_is_blank()
    {
        var repository = new FakeBootstrapRepository
        {
            Coverage = new BootstrapRoleCoverage(AdminUserCount: 0, ItUserCount: 0)
        };
        var options = new TestOptionsMonitor<AnchorIdentityOptions>(new AnchorIdentityOptions());
        var service = new IdentityBootstrapService(repository, options);

        await service.BootstrapAsync();

        Assert.NotNull(repository.Seed);
        Assert.Equal(IdentityBootstrapService.FallbackBootstrapEmail, repository.Seed!.Email);
        Assert.Equal(IdentityBootstrapService.FallbackBootstrapPassword, repository.Seed.Password);
    }

    [Fact]
    public async Task BootstrapAsync_skips_user_seed_when_minimum_coverage_exists()
    {
        var repository = new FakeBootstrapRepository
        {
            Coverage = new BootstrapRoleCoverage(AdminUserCount: 1, ItUserCount: 1)
        };
        var options = new TestOptionsMonitor<AnchorIdentityOptions>(new AnchorIdentityOptions());
        var service = new IdentityBootstrapService(repository, options);

        await service.BootstrapAsync();

        Assert.Equal(ApplicationRoles.All, repository.EnsuredRoles);
        Assert.Null(repository.Seed);
    }

    [Fact]
    public async Task BootstrapAsync_throws_when_user_seed_fails()
    {
        var repository = new FakeBootstrapRepository
        {
            Coverage = new BootstrapRoleCoverage(AdminUserCount: 0, ItUserCount: 0),
            SeedResult = IdentityOperationResult.Failure("Create user failed.")
        };
        var options = new TestOptionsMonitor<AnchorIdentityOptions>(new AnchorIdentityOptions());
        var service = new IdentityBootstrapService(repository, options);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.BootstrapAsync());

        Assert.Contains("Create user failed.", exception.Message, StringComparison.Ordinal);
    }

    private sealed class FakeBootstrapRepository : IIdentityBootstrapRepository
    {
        public List<string> EnsuredRoles { get; } = [];

        public BootstrapRoleCoverage Coverage { get; set; } = new(0, 0);

        public BootstrapUserSeed? Seed { get; private set; }

        public IdentityOperationResult SeedResult { get; set; } = IdentityOperationResult.Success();

        public Task EnsureRoleExistsAsync(string roleName, CancellationToken cancellationToken = default)
        {
            EnsuredRoles.Add(roleName);
            return Task.CompletedTask;
        }

        public Task<BootstrapRoleCoverage> GetRoleCoverageAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Coverage);

        public Task<IdentityOperationResult> EnsureBootstrapUserAsync(BootstrapUserSeed seed, CancellationToken cancellationToken = default)
        {
            Seed = seed;
            return Task.FromResult(SeedResult);
        }
    }
}
