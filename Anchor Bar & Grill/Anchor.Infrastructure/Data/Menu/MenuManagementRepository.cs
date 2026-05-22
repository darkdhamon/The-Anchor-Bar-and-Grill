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
            .Select(section => new MenuSectionReferenceRecord(section.MenuSectionId, section.Family, section.IsArchived))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<MenuItemReferenceRecord?> GetItemReferenceAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        dbContext.MenuItems
            .AsNoTracking()
            .Where(item => item.MenuItemId == itemId)
            .Select(item => new MenuItemReferenceRecord(
                item.MenuItemId,
                item.MenuSectionId,
                item.Section.Family,
                item.Name,
                item.Description,
                item.IsArchived,
                item.FoodTabs.Select(link => link.Tab).ToList(),
                item.Special != null))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> SectionHasDependentsAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
        dbContext.MenuItems.AnyAsync(item => item.MenuSectionId == sectionId, cancellationToken);

    public async Task<Guid> UpsertSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default)
    {
        MenuSectionEntity entity;
        if (request.SectionId is { } sectionId)
        {
            entity = await dbContext.MenuSections.SingleAsync(section => section.MenuSectionId == sectionId, cancellationToken);
        }
        else
        {
            entity = new MenuSectionEntity { MenuSectionId = Guid.NewGuid() };
            await dbContext.MenuSections.AddAsync(entity, cancellationToken);
        }

        entity.Name = request.Name;
        entity.Family = request.Family;
        entity.SortOrder = request.SortOrder;
        entity.IsVisibleToGuests = request.IsVisibleToGuests;
        entity.IsArchived = request.IsArchived;

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
                .Include(item => item.FoodTabs)
                .Include(item => item.Special)
                .SingleAsync(item => item.MenuItemId == itemId, cancellationToken);
        }
        else
        {
            entity = new MenuItemEntity { MenuItemId = Guid.NewGuid() };
            await dbContext.MenuItems.AddAsync(entity, cancellationToken);
        }

        entity.MenuSectionId = request.SectionId;
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.ImagePath = request.ImagePath;
        entity.SortOrder = request.SortOrder;
        entity.IsVisibleToGuests = request.IsVisibleToGuests;
        entity.IsArchived = request.IsArchived;
        entity.OfferStartsOn = request.OfferStartsOn;
        entity.OfferEndsOn = request.OfferEndsOn;
        entity.IsSeasonal = request.IsSeasonal;

        await SyncPriceVariantsAsync(entity.MenuItemId, request.PriceVariants, existingPriceVariantIds, cancellationToken);
        SyncFoodTabs(entity, request.FoodTabs);

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

    private static void SyncFoodTabs(MenuItemEntity entity, IReadOnlyList<MenuTab> requestedTabs)
    {
        var requestedTabSet = requestedTabs
            .Distinct()
            .ToHashSet();
        var existingTabs = entity.FoodTabs
            .Select(link => link.Tab)
            .ToHashSet();

        foreach (var link in entity.FoodTabs.Where(link => !requestedTabSet.Contains(link.Tab)).ToArray())
        {
            entity.FoodTabs.Remove(link);
        }

        foreach (var tab in requestedTabSet.Where(tab => !existingTabs.Contains(tab)))
        {
            entity.FoodTabs.Add(new MenuItemTabEntity
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

        var itemIds = requests.Select(request => request.RecordId).ToArray();
        var items = await dbContext.MenuItems
            .Where(item => itemIds.Contains(item.MenuItemId))
            .ToDictionaryAsync(item => item.MenuItemId, cancellationToken);

        foreach (var request in requests)
        {
            items[request.RecordId].SortOrder = request.SortOrder;
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
            .Include(menuItem => menuItem.FoodTabs)
            .Include(menuItem => menuItem.Special)
            .SingleAsync(menuItem => menuItem.MenuItemId == itemId, cancellationToken);
        dbContext.MenuItems.Remove(item);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}
