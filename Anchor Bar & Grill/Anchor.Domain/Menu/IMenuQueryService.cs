namespace Anchor.Domain.Menu;

public interface IMenuQueryService
{
    Task<PublicMenuView> GetPublicMenuAsync(MenuTab requestedTab, DateOnly today, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublicRecurringSpecialView>> GetHomeRecurringSpecialsAsync(DateOnly today, CancellationToken cancellationToken = default);

    Task<MenuManagementView> GetMenuManagementViewAsync(DateOnly today, CancellationToken cancellationToken = default);
}
