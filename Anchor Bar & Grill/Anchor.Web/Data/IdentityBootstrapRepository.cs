using Anchor.Domain.Identity.Bootstrap;
using Anchor.Domain.Identity.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Anchor.Web.Data;

public sealed class IdentityBootstrapRepository(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager) : IIdentityBootstrapRepository
{
    public async Task EnsureRoleExistsAsync(string roleName, CancellationToken cancellationToken = default)
    {
        if (await roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        var result = await roleManager.CreateAsync(new IdentityRole(roleName));
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Errors.Select(error => error.Description)));
        }
    }

    public async Task<BootstrapRoleCoverage> GetRoleCoverageAsync(CancellationToken cancellationToken = default)
    {
        var adminCount = await CountDistinctUsersInRoleAsync(Anchor.Domain.Identity.ApplicationRoles.Admin, cancellationToken);
        var itCount = await CountDistinctUsersInRoleAsync(Anchor.Domain.Identity.ApplicationRoles.It, cancellationToken);

        return new BootstrapRoleCoverage(adminCount, itCount);
    }

    public async Task<IdentityOperationResult> EnsureBootstrapUserAsync(BootstrapUserSeed seed, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(seed.Email);
        var createdNewUser = false;

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = seed.Email,
                Email = seed.Email,
                AccountConfirmed = seed.AccountConfirmed,
                EmailConfirmed = seed.EmailConfirmed,
                MustChangePassword = seed.MustChangePassword,
                IsBootstrapAccount = seed.IsBootstrapAccount
            };

            var createResult = await userManager.CreateAsync(user, seed.Password);
            if (!createResult.Succeeded)
            {
                return IdentityOperationResult.Failure(createResult.Errors.Select(error => error.Description));
            }

            createdNewUser = true;
        }
        else
        {
            var requiresUpdate = false;

            if (!string.Equals(user.Email, seed.Email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = seed.Email;
                requiresUpdate = true;
            }

            if (!string.Equals(user.UserName, seed.Email, StringComparison.OrdinalIgnoreCase))
            {
                user.UserName = seed.Email;
                requiresUpdate = true;
            }

            if (!user.AccountConfirmed && seed.AccountConfirmed)
            {
                user.AccountConfirmed = true;
                requiresUpdate = true;
            }

            if (!user.EmailConfirmed && seed.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                requiresUpdate = true;
            }

            if (!user.IsBootstrapAccount && seed.IsBootstrapAccount)
            {
                user.IsBootstrapAccount = true;
                requiresUpdate = true;
            }

            if (requiresUpdate)
            {
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return IdentityOperationResult.Failure(updateResult.Errors.Select(error => error.Description));
                }
            }
        }

        foreach (var roleName in seed.Roles)
        {
            if (await userManager.IsInRoleAsync(user, roleName))
            {
                continue;
            }

            var addToRoleResult = await userManager.AddToRoleAsync(user, roleName);
            if (!addToRoleResult.Succeeded)
            {
                return IdentityOperationResult.Failure(addToRoleResult.Errors.Select(error => error.Description));
            }
        }

        if (createdNewUser)
        {
            return IdentityOperationResult.Success();
        }

        return IdentityOperationResult.Success();
    }

    private async Task<int> CountDistinctUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var roleId = await dbContext.Roles
            .AsNoTracking()
            .Where(role => role.Name == roleName)
            .Select(role => role.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (roleId is null)
        {
            return 0;
        }

        return await dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.RoleId == roleId)
            .Select(userRole => userRole.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
    }
}
