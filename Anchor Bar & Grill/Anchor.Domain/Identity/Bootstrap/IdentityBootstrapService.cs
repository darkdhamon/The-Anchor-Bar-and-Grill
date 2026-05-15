using Anchor.Domain.Identity.Configuration;
using Microsoft.Extensions.Options;

namespace Anchor.Domain.Identity.Bootstrap;

public sealed class IdentityBootstrapService(
    IIdentityBootstrapRepository repository,
    IOptionsMonitor<AnchorIdentityOptions> optionsMonitor) : IIdentityBootstrapService
{
    public const string FallbackBootstrapEmail = "admin@anchor.local";
    public const string FallbackBootstrapPassword = "ChangeMe123!";

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleName in ApplicationRoles.All)
        {
            await repository.EnsureRoleExistsAsync(roleName, cancellationToken);
        }

        var coverage = await repository.GetRoleCoverageAsync(cancellationToken);
        if (coverage.HasMinimumCoverage)
        {
            return;
        }

        var options = optionsMonitor.CurrentValue;
        var bootstrapEmail = string.IsNullOrWhiteSpace(options.BootstrapAdmin.Email)
            ? FallbackBootstrapEmail
            : options.BootstrapAdmin.Email.Trim();
        var bootstrapPassword = string.IsNullOrWhiteSpace(options.BootstrapAdmin.Password)
            ? FallbackBootstrapPassword
            : options.BootstrapAdmin.Password;

        var result = await repository.EnsureBootstrapUserAsync(
            new BootstrapUserSeed(
                Email: bootstrapEmail,
                Password: bootstrapPassword,
                AccountConfirmed: true,
                EmailConfirmed: true,
                MustChangePassword: true,
                IsBootstrapAccount: true,
                Roles: [ApplicationRoles.Admin, ApplicationRoles.It]),
            cancellationToken);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Errors));
        }
    }
}
