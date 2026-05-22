using Anchor.Domain.Menu;
using Microsoft.EntityFrameworkCore;

namespace Anchor.Infrastructure.Data.Menu;

public sealed class MenuManagementRepository(ApplicationDbContext dbContext) : IMenuManagementRepository
{
    public Task<MenuManagementSnapshot> GetMenuManagementSnapshotAsync(CancellationToken cancellationToken = default) =>
        new MenuQueryRepository(dbContext).GetMenuManagementSnapshotAsync(cancellationToken);

    public Task<MenuSectionReferenceRecord?> GetSectionReferenceAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
        dbContext.MenuSections
            .AsNoTracking()
            .Where(section => section.MenuSectionId == sectionId)
            .Select(section => new MenuSectionReferenceRecord(
                section.MenuSectionId,
                section.Family,
                section.MenuTabs
                    .OrderBy(link => link.Tab)
                    .Select(link => link.Tab)
                    .ToList(),
                section.IsArchived))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<MenuSectionReferenceRecord>> GetSectionReferencesAsync(IReadOnlyList<Guid> sectionIds, CancellationToken cancellationToken = default) =>
        await dbContext.MenuSections
            .AsNoTracking()
            .Where(section => sectionIds.Contains(section.MenuSectionId))
            .Select(section => new MenuSectionReferenceRecord(
                section.MenuSectionId,
                section.Family,
                section.MenuTabs
                    .OrderBy(link => link.Tab)
                    .Select(link => link.Tab)
                    .ToList(),
                section.IsArchived))
            .ToListAsync(cancellationToken);

    public Task<MenuItemReferenceRecord?> GetItemReferenceAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        dbContext.MenuItems
            .AsNoTracking()
            .Where(item => item.MenuItemId == itemId)
            .Select(item => new MenuItemReferenceRecord(
                item.MenuItemId,
                item.SectionAssignments
                    .Select(assignment => assignment.Section.Family)
                    .FirstOrDefault(),
                item.Name,
                item.Description,
                item.IsArchived,
                item.SectionAssignments
                    .OrderBy(assignment => assignment.SortOrder)
                    .ThenBy(assignment => assignment.Section.SortOrder)
                    .ThenBy(assignment => assignment.Section.Name)
                    .Select(assignment => new MenuItemSectionAssignmentRecord(
                        assignment.MenuSectionId,
                        assignment.Section.Name,
                        assignment.SortOrder))
                    .ToList(),
                item.UsesSectionVisibility,
                item.MenuTabs.Select(link => link.Tab).ToList(),
                item.Special != null))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Guid?> FindSectionIdByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default) =>
        dbContext.MenuSections
            .AsNoTracking()
            .Where(section => section.NormalizedName == normalizedName)
            .Select(section => (Guid?)section.MenuSectionId)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Guid?> FindItemIdByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default) =>
        dbContext.MenuItems
            .AsNoTracking()
            .Where(item => item.NormalizedName == normalizedName)
            .Select(item => (Guid?)item.MenuItemId)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> SectionHasDependentsAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
        dbContext.MenuItemSectionAssignments.AnyAsync(assignment => assignment.MenuSectionId == sectionId, cancellationToken);

    public async Task<Guid> UpsertSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default)
    {
        MenuSectionEntity entity;
        if (request.SectionId is { } sectionId)
        {
            entity = await dbContext.MenuSections
                .Include(section => section.MenuTabs)
                .SingleAsync(section => section.MenuSectionId == sectionId, cancellationToken);
        }
        else
        {
            entity = new MenuSectionEntity { MenuSectionId = Guid.NewGuid() };
            await dbContext.MenuSections.AddAsync(entity, cancellationToken);
        }

        entity.Name = request.Name;
        entity.NormalizedName = MenuNameRules.NormalizeForLookup(request.Name);
        entity.Callout = request.Callout;
        entity.Family = request.Family;
        entity.SortOrder = request.SortOrder;
        entity.IsVisibleToGuests = request.IsVisibleToGuests;
        entity.IsArchived = request.IsArchived;
        SyncSectionTabs(entity, request.MenuTabs);

        return entity.MenuSectionId;
    }

    public async Task<Guid> UpsertItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default)
    {
        dbContext.ChangeTracker.Clear();

        Guid[] existingPriceVariantIds = [];
        MenuItemEntity entity;
        if (request.ItemId is { } itemId)
        {
            existingPriceVariantIds = await dbContext.MenuItemPriceVariants
                .AsNoTracking()
                .Where(variant => variant.MenuItemId == itemId)
                .OrderBy(variant => variant.SortOrder)
                .ThenBy(variant => variant.MenuItemPriceVariantId)
                .Select(variant => variant.MenuItemPriceVariantId)
                .ToArrayAsync(cancellationToken);

            entity = await dbContext.MenuItems
                .Include(item => item.MenuTabs)
                .Include(item => item.SectionAssignments)
                .Include(item => item.Special)
                .SingleAsync(item => item.MenuItemId == itemId, cancellationToken);
        }
        else
        {
            entity = new MenuItemEntity { MenuItemId = Guid.NewGuid() };
            await dbContext.MenuItems.AddAsync(entity, cancellationToken);
        }

        entity.Name = request.Name;
        entity.NormalizedName = MenuNameRules.NormalizeForLookup(request.Name);
        entity.Description = request.Description;
        entity.ImagePath = request.ImagePath;
        entity.SortOrder = request.SortOrder;
        entity.IsVisibleToGuests = request.IsVisibleToGuests;
        entity.IsArchived = request.IsArchived;
        entity.OfferStartsOn = request.OfferStartsOn;
        entity.OfferEndsOn = request.OfferEndsOn;
        entity.IsSeasonal = request.IsSeasonal;
        entity.UsesSectionVisibility = request.UsesSectionVisibility;

        await SyncPriceVariantsAsync(entity.MenuItemId, request.PriceVariants, existingPriceVariantIds, cancellationToken);
        SyncSectionAssignments(entity, request.SectionAssignments);
        SyncMenuTabs(entity, request.MenuTabs);

        if (request.Special is null)
        {
            if (entity.Special is not null)
            {
                dbContext.MenuItemSpecials.Remove(entity.Special);
                entity.Special = null;
            }
        }
        else
        {
            entity.Special ??= new MenuItemSpecialEntity { MenuItemId = entity.MenuItemId };
            entity.Special.ScheduleKind = request.Special.ScheduleKind;
            entity.Special.DayOfWeek = request.Special.DayOfWeek;
            entity.Special.StartDate = request.Special.StartDate;
            entity.Special.EndDate = request.Special.EndDate;
            entity.Special.StartsAt = request.Special.StartsAt;
            entity.Special.EndsAt = request.Special.EndsAt;
            entity.Special.ClosesNextDay = request.Special.ClosesNextDay;
            entity.Special.Callout = request.Special.Callout;
        }

        return entity.MenuItemId;
    }

    private async Task SyncPriceVariantsAsync(
        Guid itemId,
        IReadOnlyList<SaveMenuItemPriceVariantRequest> requestedVariants,
        IReadOnlyList<Guid> existingPriceVariantIds,
        CancellationToken cancellationToken)
    {
        if (existingPriceVariantIds.Count > 0)
        {
            await dbContext.MenuItemPriceVariants
                .Where(variant => variant.MenuItemId == itemId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        var orderedRequestedVariants = requestedVariants
            .OrderBy(variant => variant.SortOrder)
            .ToArray();

        for (var index = 0; index < orderedRequestedVariants.Length; index++)
        {
            var requestedVariant = orderedRequestedVariants[index];
            var priceVariantId = requestedVariant.PriceVariantId
                ?? (index < existingPriceVariantIds.Count ? existingPriceVariantIds[index] : Guid.NewGuid());

            await dbContext.MenuItemPriceVariants.AddAsync(new MenuItemPriceVariantEntity
            {
                MenuItemPriceVariantId = priceVariantId,
                MenuItemId = itemId,
                Label = requestedVariant.Label,
                Amount = requestedVariant.Amount,
                SortOrder = requestedVariant.SortOrder
            }, cancellationToken);
        }
    }

    private static void SyncSectionAssignments(MenuItemEntity entity, IReadOnlyList<SaveMenuItemSectionAssignmentRequest> requestedAssignments)
    {
        var requestedAssignmentsBySection = requestedAssignments
            .GroupBy(assignment => assignment.SectionId)
            .Select(group => group.First())
            .ToDictionary(assignment => assignment.SectionId);
        var existingAssignmentsBySection = entity.SectionAssignments
            .ToDictionary(assignment => assignment.MenuSectionId);

        foreach (var assignment in entity.SectionAssignments.Where(assignment => !requestedAssignmentsBySection.ContainsKey(assignment.MenuSectionId)).ToArray())
        {
            entity.SectionAssignments.Remove(assignment);
        }

        foreach (var request in requestedAssignmentsBySection.Values)
        {
            if (existingAssignmentsBySection.TryGetValue(request.SectionId, out var existingAssignment))
            {
                existingAssignment.SortOrder = request.SortOrder;
                continue;
            }

            entity.SectionAssignments.Add(new MenuItemSectionAssignmentEntity
            {
                MenuItemId = entity.MenuItemId,
                MenuSectionId = request.SectionId,
                SortOrder = request.SortOrder
            });
        }
    }

    private static void SyncSectionTabs(MenuSectionEntity entity, IReadOnlyList<MenuTab> requestedTabs)
    {
        var requestedTabSet = requestedTabs
            .Distinct()
            .ToHashSet();
        var existingTabs = entity.MenuTabs
            .Select(link => link.Tab)
            .ToHashSet();

        foreach (var link in entity.MenuTabs.Where(link => !requestedTabSet.Contains(link.Tab)).ToArray())
        {
            entity.MenuTabs.Remove(link);
        }

        foreach (var tab in requestedTabSet.Where(tab => !existingTabs.Contains(tab)))
        {
            entity.MenuTabs.Add(new MenuSectionTabEntity
            {
                MenuSectionId = entity.MenuSectionId,
                Tab = tab
            });
        }
    }

    private static void SyncMenuTabs(MenuItemEntity entity, IReadOnlyList<MenuTab> requestedTabs)
    {
        var requestedTabSet = requestedTabs
            .Distinct()
            .ToHashSet();
        var existingTabs = entity.MenuTabs
            .Select(link => link.Tab)
            .ToHashSet();

        foreach (var link in entity.MenuTabs.Where(link => !requestedTabSet.Contains(link.Tab)).ToArray())
        {
            entity.MenuTabs.Remove(link);
        }

        foreach (var tab in requestedTabSet.Where(tab => !existingTabs.Contains(tab)))
        {
            entity.MenuTabs.Add(new MenuItemTabEntity
            {
                MenuItemId = entity.MenuItemId,
                Tab = tab
            });
        }
    }

    public async Task UpsertServiceWindowsAsync(SaveMenuServiceWindowRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.MenuServiceWindows
            .Where(window => window.Tab == request.Tab)
            .ToDictionaryAsync(window => window.DayOfWeek, cancellationToken);

        foreach (var day in request.Days)
        {
            if (!existing.TryGetValue(day.DayOfWeek, out var entity))
            {
                entity = new MenuServiceWindowEntity
                {
                    Tab = request.Tab,
                    DayOfWeek = day.DayOfWeek
                };

                await dbContext.MenuServiceWindows.AddAsync(entity, cancellationToken);
            }

            entity.IsAvailable = day.IsAvailable;
            entity.OpensAt = day.IsAvailable ? day.OpensAt : null;
            entity.ClosesAt = day.IsAvailable ? day.ClosesAt : null;
            entity.ClosesNextDay = day.IsAvailable && day.ClosesNextDay;
        }
    }

    public async Task ReorderSectionsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default)
    {
        dbContext.ChangeTracker.Clear();

        var sectionIds = requests.Select(request => request.RecordId).ToArray();
        var sections = await dbContext.MenuSections
            .Where(section => sectionIds.Contains(section.MenuSectionId))
            .ToDictionaryAsync(section => section.MenuSectionId, cancellationToken);

        foreach (var request in requests)
        {
            sections[request.RecordId].SortOrder = request.SortOrder;
        }
    }

    public async Task ReorderItemsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default)
    {
        dbContext.ChangeTracker.Clear();

        var sectionIds = requests
            .Select(request => request.ContextId)
            .Distinct()
            .ToArray();
        var itemIds = requests
            .Select(request => request.RecordId)
            .Distinct()
            .ToArray();
        var assignments = await dbContext.MenuItemSectionAssignments
            .Where(assignment => sectionIds.Contains(assignment.MenuSectionId) && itemIds.Contains(assignment.MenuItemId))
            .ToDictionaryAsync(assignment => new { assignment.MenuItemId, assignment.MenuSectionId }, cancellationToken);

        foreach (var request in requests.Where(request => request.ContextId is not null))
        {
            assignments[new { MenuItemId = request.RecordId, MenuSectionId = request.ContextId!.Value }].SortOrder = request.SortOrder;
        }
    }

    public async Task ArchiveSectionAsync(Guid sectionId, CancellationToken cancellationToken = default)
    {
        var section = await dbContext.MenuSections.SingleAsync(item => item.MenuSectionId == sectionId, cancellationToken);
        section.IsArchived = true;
        section.IsVisibleToGuests = false;
    }

    public async Task DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default)
    {
        var section = await dbContext.MenuSections.SingleAsync(item => item.MenuSectionId == sectionId, cancellationToken);
        dbContext.MenuSections.Remove(section);
    }

    public async Task ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.MenuItems.SingleAsync(menuItem => menuItem.MenuItemId == itemId, cancellationToken);
        item.IsArchived = true;
        item.IsVisibleToGuests = false;
    }

    public async Task DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.MenuItems
            .Include(menuItem => menuItem.PriceVariants)
            .Include(menuItem => menuItem.MenuTabs)
            .Include(menuItem => menuItem.SectionAssignments)
            .Include(menuItem => menuItem.Special)
            .SingleAsync(menuItem => menuItem.MenuItemId == itemId, cancellationToken);
        dbContext.MenuItems.Remove(item);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}
