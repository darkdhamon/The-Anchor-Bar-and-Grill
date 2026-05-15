using Anchor.Domain.Identity.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Anchor.Web.Data;

public sealed class IdentityAdministrationRepository(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IIdentityAdministrationRepository
{
    public async Task<IReadOnlyList<ManagedUserSummary>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Email ?? user.UserName)
            .Select(user => new
            {
                user.Id,
                Email = user.Email ?? user.UserName ?? "(no email)",
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.EmailConfirmed,
                user.MustChangePassword,
                user.IsBootstrapAccount
            })
            .ToListAsync(cancellationToken);

        var rolesByUserId = await GetRoleLookupAsync(cancellationToken);

        return users
            .Select(user => new ManagedUserSummary(
                UserId: user.Id,
                Email: user.Email,
                FirstName: user.FirstName,
                LastName: user.LastName,
                PhoneNumber: user.PhoneNumber,
                EmailConfirmed: user.EmailConfirmed,
                MustChangePassword: user.MustChangePassword,
                IsBootstrapAccount: user.IsBootstrapAccount,
                Roles: rolesByUserId.GetValueOrDefault(user.Id, Array.Empty<string>())))
            .ToList();
    }

    public async Task<ManagedUserSummary?> GetUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(candidate => candidate.Id == userId)
            .Select(candidate => new
            {
                candidate.Id,
                Email = candidate.Email ?? candidate.UserName ?? "(no email)",
                candidate.FirstName,
                candidate.LastName,
                candidate.PhoneNumber,
                candidate.EmailConfirmed,
                candidate.MustChangePassword,
                candidate.IsBootstrapAccount
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var rolesByUserId = await GetRoleLookupAsync(cancellationToken);

        return new ManagedUserSummary(
            UserId: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            EmailConfirmed: user.EmailConfirmed,
            MustChangePassword: user.MustChangePassword,
            IsBootstrapAccount: user.IsBootstrapAccount,
            Roles: rolesByUserId.GetValueOrDefault(user.Id, Array.Empty<string>()));
    }

    public async Task<IdentityOperationResult> CreateUserAsync(CreateManagedUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = request.EmailConfirmed,
            MustChangePassword = true
        };

        var result = await userManager.CreateAsync(user, request.TemporaryPassword);

        return result.Succeeded
            ? IdentityOperationResult.Success()
            : IdentityOperationResult.Failure(result.Errors.Select(error => error.Description));
    }

    public async Task<IdentityOperationResult> UpdateUserProfileAsync(UpdateManagedUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return IdentityOperationResult.Failure("The selected user could not be found.");
        }

        var hasChanges = false;

        if (!string.Equals(user.FirstName, request.FirstName, StringComparison.Ordinal))
        {
            user.FirstName = request.FirstName;
            hasChanges = true;
        }

        if (!string.Equals(user.LastName, request.LastName, StringComparison.Ordinal))
        {
            user.LastName = request.LastName;
            hasChanges = true;
        }

        if (!string.Equals(user.PhoneNumber, request.PhoneNumber, StringComparison.Ordinal))
        {
            user.PhoneNumber = request.PhoneNumber;
            user.PhoneNumberConfirmed = false;
            hasChanges = true;
        }

        if (!hasChanges)
        {
            return IdentityOperationResult.Success();
        }

        var result = await userManager.UpdateAsync(user);

        return result.Succeeded
            ? IdentityOperationResult.Success()
            : IdentityOperationResult.Failure(result.Errors.Select(error => error.Description));
    }

    public async Task<IdentityOperationResult> ResetUserPasswordAsync(ResetManagedUserPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return IdentityOperationResult.Failure("The selected user could not be found.");
        }

        var validationErrors = new List<IdentityError>();
        foreach (var validator in userManager.PasswordValidators)
        {
            var validationResult = await validator.ValidateAsync(userManager, user, request.TemporaryPassword);
            if (!validationResult.Succeeded)
            {
                validationErrors.AddRange(validationResult.Errors);
            }
        }

        if (validationErrors.Count > 0)
        {
            return IdentityOperationResult.Failure(validationErrors.Select(error => error.Description));
        }

        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, request.TemporaryPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.MustChangePassword = true;
        var updateResult = await userManager.UpdateAsync(user);

        return updateResult.Succeeded
            ? IdentityOperationResult.Success()
            : IdentityOperationResult.Failure(updateResult.Errors.Select(error => error.Description));
    }

    public Task<int> CountUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default) =>
        CountDistinctUsersInRoleAsync(roleName, cancellationToken);

    public Task<int> CountBootstrapUsersAsync(CancellationToken cancellationToken = default) =>
        dbContext.Users.CountAsync(user => user.IsBootstrapAccount, cancellationToken);

    public async Task<IdentityOperationResult> AddRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityOperationResult.Failure("The selected user could not be found.");
        }

        if (await userManager.IsInRoleAsync(user, roleName))
        {
            return IdentityOperationResult.Success();
        }

        var result = await userManager.AddToRoleAsync(user, roleName);
        return result.Succeeded
            ? IdentityOperationResult.Success()
            : IdentityOperationResult.Failure(result.Errors.Select(error => error.Description));
    }

    public async Task<IdentityOperationResult> RemoveRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityOperationResult.Failure("The selected user could not be found.");
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            return IdentityOperationResult.Success();
        }

        var result = await userManager.RemoveFromRoleAsync(user, roleName);
        return result.Succeeded
            ? IdentityOperationResult.Success()
            : IdentityOperationResult.Failure(result.Errors.Select(error => error.Description));
    }

    public async Task<IdentityOperationResult> SetEmailConfirmedAsync(string userId, bool emailConfirmed, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return IdentityOperationResult.Failure("The selected user could not be found.");
        }

        if (user.EmailConfirmed == emailConfirmed)
        {
            return IdentityOperationResult.Success();
        }

        user.EmailConfirmed = emailConfirmed;
        var result = await userManager.UpdateAsync(user);

        return result.Succeeded
            ? IdentityOperationResult.Success()
            : IdentityOperationResult.Failure(result.Errors.Select(error => error.Description));
    }

    private async Task<Dictionary<string, IReadOnlyCollection<string>>> GetRoleLookupAsync(CancellationToken cancellationToken)
    {
        var rolePairs = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where role.Name != null
            select new
            {
                userRole.UserId,
                RoleName = role.Name
            })
            .ToListAsync(cancellationToken);

        return rolePairs
            .GroupBy(pair => pair.UserId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<string>)group
                    .Select(pair => pair.RoleName!)
                    .OrderBy(roleName => roleName, StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);
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
