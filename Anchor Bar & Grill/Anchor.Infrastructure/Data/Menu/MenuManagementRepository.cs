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
                item.FoodTabs.Select(link => link.Tab).ToList()))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> SectionHasDependentsAsync(Guid sectionId, CancellationToken cancellationToken = default)
    {
        if (await dbContext.MenuItems.AnyAsync(item => item.MenuSectionId == sectionId, cancellationToken))
        {
            return true;
        }

        return await dbContext.RecurringSpecials.AnyAsync(special => special.MenuSectionId == sectionId, cancellationToken);
    }

    public Task<bool> ItemHasLinkedSpecialsAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        dbContext.RecurringSpecials.AnyAsync(special => special.LinkedMenuItemId == itemId, cancellationToken);

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
        MenuItemEntity entity;
        if (request.ItemId is { } itemId)
        {
            entity = await dbContext.MenuItems
                .Include(item => item.PriceVariants)
                .Include(item => item.FoodTabs)
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

        entity.PriceVariants.Clear();
        foreach (var variant in request.PriceVariants)
        {
            entity.PriceVariants.Add(new MenuItemPriceVariantEntity
            {
                MenuItemPriceVariantId = variant.PriceVariantId ?? Guid.NewGuid(),
                Label = variant.Label,
                Amount = variant.Amount,
                SortOrder = variant.SortOrder
            });
        }

        entity.FoodTabs.Clear();
        foreach (var tab in request.FoodTabs.Distinct())
        {
            entity.FoodTabs.Add(new MenuItemTabEntity
            {
                MenuItemId = entity.MenuItemId,
                Tab = tab
            });
        }

        return entity.MenuItemId;
    }

    public async Task<Guid> UpsertRecurringSpecialAsync(SaveRecurringSpecialRequest request, CancellationToken cancellationToken = default)
    {
        RecurringSpecialEntity entity;
        if (request.SpecialId is { } specialId)
        {
            entity = await dbContext.RecurringSpecials.SingleAsync(special => special.RecurringSpecialId == specialId, cancellationToken);
        }
        else
        {
            entity = new RecurringSpecialEntity { RecurringSpecialId = Guid.NewGuid() };
            await dbContext.RecurringSpecials.AddAsync(entity, cancellationToken);
        }

        entity.Tab = request.Tab;
        entity.MenuSectionId = request.SectionId;
        entity.DayOfWeek = request.DayOfWeek;
        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.TimeNote = request.TimeNote;
        entity.PriceNote = request.PriceNote;
        entity.LinkedMenuItemId = request.LinkedMenuItemId;
        entity.SortOrder = request.SortOrder;
        entity.IsVisibleToGuests = request.IsVisibleToGuests;
        entity.IsArchived = request.IsArchived;

        return entity.RecurringSpecialId;
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

    public async Task ReorderRecurringSpecialsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default)
    {
        dbContext.ChangeTracker.Clear();

        var specialIds = requests.Select(request => request.RecordId).ToArray();
        var specials = await dbContext.RecurringSpecials
            .Where(special => specialIds.Contains(special.RecurringSpecialId))
            .ToDictionaryAsync(special => special.RecurringSpecialId, cancellationToken);

        foreach (var request in requests)
        {
            specials[request.RecordId].SortOrder = request.SortOrder;
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
            .SingleAsync(menuItem => menuItem.MenuItemId == itemId, cancellationToken);
        dbContext.MenuItems.Remove(item);
    }

    public async Task ArchiveRecurringSpecialAsync(Guid specialId, CancellationToken cancellationToken = default)
    {
        var special = await dbContext.RecurringSpecials.SingleAsync(item => item.RecurringSpecialId == specialId, cancellationToken);
        special.IsArchived = true;
        special.IsVisibleToGuests = false;
    }

    public async Task DeleteRecurringSpecialAsync(Guid specialId, CancellationToken cancellationToken = default)
    {
        var special = await dbContext.RecurringSpecials.SingleAsync(item => item.RecurringSpecialId == specialId, cancellationToken);
        dbContext.RecurringSpecials.Remove(special);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}
