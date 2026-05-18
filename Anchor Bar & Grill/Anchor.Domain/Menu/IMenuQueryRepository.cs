namespace Anchor.Domain.Menu;

public interface IMenuQueryRepository
{
    Task<PublicMenuSnapshot> GetPublicMenuSnapshotAsync(
        MenuTab tab,
        DateOnly today,
        DateOnly comingSoonCutoff,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MenuRecurringSpecialRecord>> GetHomeRecurringSpecialsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MenuTab>> GetTabsWithVisibleContentAsync(
        DateOnly today,
        DateOnly comingSoonCutoff,
        CancellationToken cancellationToken = default);

    Task<MenuManagementSnapshot> GetMenuManagementSnapshotAsync(CancellationToken cancellationToken = default);
}
