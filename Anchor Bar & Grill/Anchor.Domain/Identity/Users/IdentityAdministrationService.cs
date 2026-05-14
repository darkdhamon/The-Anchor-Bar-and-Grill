using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Anchor.Domain.Identity.Users;

public sealed class IdentityAdministrationService(IIdentityAdministrationRepository repository) : IIdentityAdministrationService
{
    private static readonly Regex PhoneNumberPattern = new(
        @"^[0-9()+.\-\s]{7,25}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public Task<IReadOnlyList<ManagedUserSummary>> GetUsersAsync(CancellationToken cancellationToken = default) =>
        repository.GetUsersAsync(cancellationToken);

    public async Task<BootstrapSecurityOverview> GetSecurityOverviewAsync(CancellationToken cancellationToken = default)
    {
        var adminCount = await repository.CountUsersInRoleAsync(Identity.ApplicationRoles.Admin, cancellationToken);
        var itCount = await repository.CountUsersInRoleAsync(Identity.ApplicationRoles.It, cancellationToken);
        var bootstrapCount = await repository.CountBootstrapUsersAsync(cancellationToken);

        return new BootstrapSecurityOverview(adminCount, itCount, bootstrapCount);
    }

    public Task<IdentityOperationResult> CreateUserAsync(CreateManagedUserRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return Task.FromResult(IdentityOperationResult.Failure("An email address is required."));
        }

        if (string.IsNullOrWhiteSpace(request.TemporaryPassword))
        {
            return Task.FromResult(IdentityOperationResult.Failure("A temporary password is required."));
        }

        return repository.CreateUserAsync(
            request with
            {
                Email = normalizedEmail,
                TemporaryPassword = request.TemporaryPassword.Trim()
            },
            cancellationToken);
    }

    public Task<IdentityOperationResult> UpdateUserProfileAsync(UpdateManagedUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedUserId = request.UserId.Trim();
        if (string.IsNullOrWhiteSpace(normalizedUserId))
        {
            return Task.FromResult(IdentityOperationResult.Failure("A user identifier is required."));
        }

        var normalizedFirstName = NormalizeOptionalValue(request.FirstName);
        if (normalizedFirstName is { Length: > 100 })
        {
            return Task.FromResult(IdentityOperationResult.Failure("First name cannot be longer than 100 characters."));
        }

        var normalizedLastName = NormalizeOptionalValue(request.LastName);
        if (normalizedLastName is { Length: > 100 })
        {
            return Task.FromResult(IdentityOperationResult.Failure("Last name cannot be longer than 100 characters."));
        }

        var normalizedPhoneNumber = NormalizeOptionalValue(request.PhoneNumber);
        if (normalizedPhoneNumber is not null && !PhoneNumberPattern.IsMatch(normalizedPhoneNumber))
        {
            return Task.FromResult(IdentityOperationResult.Failure("Enter a valid phone number before saving the user profile."));
        }

        return repository.UpdateUserProfileAsync(
            request with
            {
                UserId = normalizedUserId,
                FirstName = normalizedFirstName,
                LastName = normalizedLastName,
                PhoneNumber = normalizedPhoneNumber
            },
            cancellationToken);
    }

    public Task<IdentityOperationResult> AddRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        if (!Identity.ApplicationRoles.IsManagedRole(roleName))
        {
            return Task.FromResult(IdentityOperationResult.Failure($"'{roleName}' is not a managed application role."));
        }

        return repository.AddRoleAsync(userId, roleName, cancellationToken);
    }

    public async Task<IdentityOperationResult> RemoveRoleAsync(
        string userId,
        string roleName,
        string actingUserId,
        CancellationToken cancellationToken = default)
    {
        if (!Identity.ApplicationRoles.IsManagedRole(roleName))
        {
            return IdentityOperationResult.Failure($"'{roleName}' is not a managed application role.");
        }

        var user = await repository.GetUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return IdentityOperationResult.Failure("The selected user could not be found.");
        }

        if (!user.Roles.Contains(roleName, StringComparer.Ordinal))
        {
            return IdentityOperationResult.Success();
        }

        if (roleName == Identity.ApplicationRoles.Admin)
        {
            if (string.Equals(userId, actingUserId, StringComparison.Ordinal))
            {
                return IdentityOperationResult.Failure("You cannot remove the Admin role from your own account.");
            }

            var adminCount = await repository.CountUsersInRoleAsync(Identity.ApplicationRoles.Admin, cancellationToken);
            if (adminCount <= 1)
            {
                return IdentityOperationResult.Failure("You cannot remove the last Admin user.");
            }
        }

        if (roleName == Identity.ApplicationRoles.It)
        {
            var itCount = await repository.CountUsersInRoleAsync(Identity.ApplicationRoles.It, cancellationToken);
            if (itCount <= 1)
            {
                return IdentityOperationResult.Failure("You cannot remove the last IT user.");
            }
        }

        return await repository.RemoveRoleAsync(userId, roleName, cancellationToken);
    }

    public Task<IdentityOperationResult> SetEmailConfirmedAsync(string userId, bool emailConfirmed, CancellationToken cancellationToken = default) =>
        repository.SetEmailConfirmedAsync(userId, emailConfirmed, cancellationToken);

    private static string? NormalizeOptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
