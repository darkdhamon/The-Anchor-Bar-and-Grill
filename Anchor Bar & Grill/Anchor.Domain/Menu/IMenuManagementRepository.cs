namespace Anchor.Domain.Menu;

public interface IMenuManagementRepository
{
    Task<MenuManagementSnapshot> GetMenuManagementSnapshotAsync(CancellationToken cancellationToken = default);

    Task<MenuSectionReferenceRecord?> GetSectionReferenceAsync(Guid sectionId, CancellationToken cancellationToken = default);

    Task<MenuItemReferenceRecord?> GetItemReferenceAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<bool> SectionHasDependentsAsync(Guid sectionId, CancellationToken cancellationToken = default);

    Task<bool> ItemHasLinkedSpecialsAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<Guid> UpsertSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default);

    Task<Guid> UpsertItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default);

    Task<Guid> UpsertRecurringSpecialAsync(SaveRecurringSpecialRequest request, CancellationToken cancellationToken = default);

    Task UpsertServiceWindowsAsync(SaveMenuServiceWindowRequest request, CancellationToken cancellationToken = default);

    Task ReorderSectionsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default);

    Task ReorderItemsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default);

    Task ReorderRecurringSpecialsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default);

    Task ArchiveSectionAsync(Guid sectionId, CancellationToken cancellationToken = default);

    Task DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default);

    Task ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task ArchiveRecurringSpecialAsync(Guid specialId, CancellationToken cancellationToken = default);

    Task DeleteRecurringSpecialAsync(Guid specialId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
