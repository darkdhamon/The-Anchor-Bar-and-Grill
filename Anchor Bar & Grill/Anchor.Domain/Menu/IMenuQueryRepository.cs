namespace Anchor.Domain.Menu;

public interface IMenuQueryRepository
{
    Task<PublicMenuSnapshot> GetPublicMenuSnapshotAsync(
        MenuTab tab,
        DateOnly today,
        DateOnly comingSoonCutoff,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MenuItemRecord>> GetHomeSpecialItemsAsync(
        DateOnly today,
        DateOnly comingSoonCutoff,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<MenuTab>> GetTabsWithVisibleContentAsync(
        DateOnly today,
        DateOnly comingSoonCutoff,
        CancellationToken cancellationToken = default);

    Task<MenuManagementSnapshot> GetMenuManagementSnapshotAsync(CancellationToken cancellationToken = default);
}
