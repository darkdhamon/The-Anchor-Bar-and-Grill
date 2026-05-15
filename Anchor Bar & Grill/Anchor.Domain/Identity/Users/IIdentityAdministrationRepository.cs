namespace Anchor.Domain.Identity.Users;

public interface IIdentityAdministrationRepository
{
    Task<IReadOnlyList<ManagedUserSummary>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<ManagedUserSummary?> GetUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> CreateUserAsync(CreateManagedUserRequest request, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> UpdateUserProfileAsync(UpdateManagedUserProfileRequest request, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> ResetUserPasswordAsync(ResetManagedUserPasswordRequest request, CancellationToken cancellationToken = default);

    Task<int> CountUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default);

    Task<int> CountBootstrapUsersAsync(CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> AddRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> RemoveRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> SetEmailConfirmedAsync(string userId, bool emailConfirmed, CancellationToken cancellationToken = default);
}
