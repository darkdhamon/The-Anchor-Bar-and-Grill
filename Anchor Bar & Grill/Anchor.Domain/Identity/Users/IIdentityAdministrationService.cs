namespace Anchor.Domain.Identity.Users;

public interface IIdentityAdministrationService
{
    Task<IReadOnlyList<ManagedUserSummary>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<BootstrapSecurityOverview> GetSecurityOverviewAsync(CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> CreateUserAsync(CreateManagedUserRequest request, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> AddRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> RemoveRoleAsync(string userId, string roleName, string actingUserId, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> SetEmailConfirmedAsync(string userId, bool emailConfirmed, CancellationToken cancellationToken = default);
}
