namespace Anchor.Domain.Menu;

public interface IMenuManagementService
{
    Task<MenuOperationResult> SaveSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default);

    Task<MenuOperationResult> SaveItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default);

    Task<MenuOperationResult> SaveServiceWindowsAsync(SaveMenuServiceWindowRequest request, CancellationToken cancellationToken = default);

    Task<MenuOperationResult> ReorderSectionsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default);

    Task<MenuOperationResult> ReorderItemsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default);

    Task<MenuOperationResult> ArchiveSectionAsync(Guid sectionId, CancellationToken cancellationToken = default);

    Task<MenuOperationResult> DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default);

    Task<MenuOperationResult> ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<MenuOperationResult> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default);
}
