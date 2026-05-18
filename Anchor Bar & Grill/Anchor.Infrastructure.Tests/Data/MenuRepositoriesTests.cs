using Anchor.Domain.Menu;
using Anchor.Infrastructure.Data;
using Anchor.Infrastructure.Data.Menu;
using Anchor.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Anchor.Infrastructure.Tests.Data;

public sealed class MenuRepositoriesTests
{
    private static readonly Guid BurgersSectionId = Guid.Parse("198CCF8A-72FD-4278-A360-F36D5871E58B");
    private static readonly Guid ClassicHamburgerItemId = Guid.Parse("7626D0DF-9F8A-4FE8-9062-3596165E148A");

    [Fact]
    public async Task GetPublicMenuSnapshotAsync_returns_seeded_lunch_content_with_price_variants()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuQueryRepository(context.DbContext);

        var snapshot = await repository.GetPublicMenuSnapshotAsync(
            MenuTab.Lunch,
            new DateOnly(2026, 5, 17),
            new DateOnly(2026, 6, 16));

        Assert.Equal(MenuTab.Lunch, snapshot.Tab);
        Assert.Contains(snapshot.Sections, section => section.Name == "Appetizers");
        Assert.Contains(snapshot.Items, item => item.Name == "Cheese Curds");
        Assert.Contains(snapshot.Items, item => item.Name == "Seasonal Soup" && item.PriceVariants.Count == 2);
        Assert.Empty(snapshot.Specials);
    }

    [Fact]
    public async Task GetPublicMenuSnapshotAsync_returns_empty_drinks_menu_with_overnight_hours()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuQueryRepository(context.DbContext);

        var snapshot = await repository.GetPublicMenuSnapshotAsync(
            MenuTab.Drinks,
            new DateOnly(2026, 5, 17),
            new DateOnly(2026, 6, 16));

        Assert.Empty(snapshot.Sections);
        Assert.Empty(snapshot.Items);
        Assert.Empty(snapshot.Specials);
        Assert.Equal(7, snapshot.ServiceWindows.Count);
        Assert.Contains(snapshot.ServiceWindows, window => window.DayOfWeek == DayOfWeek.Friday && window.ClosesNextDay);
    }

    [Fact]
    public async Task GetMenuManagementSnapshotAsync_includes_hidden_and_archived_content()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var hiddenSectionId = Guid.NewGuid();
        var hiddenItemId = Guid.NewGuid();
        var hiddenSpecialId = Guid.NewGuid();

        context.DbContext.MenuSections.Add(new MenuSectionEntity
        {
            MenuSectionId = hiddenSectionId,
            Name = "Hidden Cocktails",
            Family = MenuFamily.Drink,
            SortOrder = 9,
            IsVisibleToGuests = false,
            IsArchived = true
        });
        context.DbContext.MenuItems.Add(new MenuItemEntity
        {
            MenuItemId = hiddenItemId,
            MenuSectionId = hiddenSectionId,
            Name = "Smoked Old Fashioned",
            Description = "Archived drink profile.",
            SortOrder = 1,
            IsVisibleToGuests = false,
            IsArchived = true
        });
        context.DbContext.RecurringSpecials.Add(new RecurringSpecialEntity
        {
            RecurringSpecialId = hiddenSpecialId,
            Tab = MenuTab.Drinks,
            MenuSectionId = hiddenSectionId,
            DayOfWeek = DayOfWeek.Friday,
            Title = "Late Night Old Fashioned",
            Description = "After-hours cocktail special.",
            TimeNote = "After 8:00 PM",
            SortOrder = 1,
            IsVisibleToGuests = false,
            IsArchived = true
        });
        await context.DbContext.SaveChangesAsync();

        var repository = new MenuManagementRepository(context.DbContext);
        var snapshot = await repository.GetMenuManagementSnapshotAsync();

        Assert.Contains(snapshot.Sections, section => section.SectionId == hiddenSectionId && section.IsArchived);
        Assert.Contains(snapshot.Items, item => item.ItemId == hiddenItemId && item.IsArchived);
        Assert.Contains(snapshot.Specials, special => special.SpecialId == hiddenSpecialId && special.IsArchived);
    }

    [Fact]
    public async Task UpsertItemAsync_persists_price_variants_and_food_tab_assignments()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuManagementRepository(context.DbContext);
        var itemId = await repository.UpsertItemAsync(
            new SaveMenuItemRequest(
                null,
                BurgersSectionId,
                "Test Burger",
                "Built for repository coverage.",
                null,
                99,
                true,
                false,
                null,
                null,
                false,
                [
                    new SaveMenuItemPriceVariantRequest(null, "Single", 12m, 1),
                    new SaveMenuItemPriceVariantRequest(null, "Double", 15m, 2)
                ],
                [MenuTab.Breakfast, MenuTab.Dinner]));
        await repository.SaveChangesAsync();

        var savedItem = await context.DbContext.MenuItems
            .Include(item => item.PriceVariants)
            .Include(item => item.FoodTabs)
            .SingleAsync(item => item.MenuItemId == itemId);

        Assert.Equal("Test Burger", savedItem.Name);
        Assert.Equal(2, savedItem.PriceVariants.Count);
        Assert.Contains(savedItem.FoodTabs, link => link.Tab == MenuTab.Breakfast);
        Assert.Contains(savedItem.FoodTabs, link => link.Tab == MenuTab.Dinner);
    }

    [Fact]
    public async Task SectionHasDependentsAsync_and_ItemHasLinkedSpecialsAsync_report_seeded_references()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuManagementRepository(context.DbContext);

        Assert.True(await repository.SectionHasDependentsAsync(BurgersSectionId));
        Assert.True(await repository.ItemHasLinkedSpecialsAsync(ClassicHamburgerItemId));
    }

    [Fact]
    public async Task ReorderItemsAsync_updates_sort_orders_without_rewriting_price_variants()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuManagementRepository(context.DbContext);

        var beforeVariantIds = await context.DbContext.MenuItemPriceVariants
            .Where(variant => variant.MenuItemId == ClassicHamburgerItemId)
            .Select(variant => variant.MenuItemPriceVariantId)
            .OrderBy(id => id)
            .ToArrayAsync();

        await repository.ReorderItemsAsync([new SaveMenuSortOrderRequest(ClassicHamburgerItemId, 99)]);
        await repository.SaveChangesAsync();

        var savedItem = await context.DbContext.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.MenuItemId == ClassicHamburgerItemId);
        var afterVariantIds = await context.DbContext.MenuItemPriceVariants
            .Where(variant => variant.MenuItemId == ClassicHamburgerItemId)
            .Select(variant => variant.MenuItemPriceVariantId)
            .OrderBy(id => id)
            .ToArrayAsync();

        Assert.Equal(99, savedItem.SortOrder);
        Assert.Equal(beforeVariantIds, afterVariantIds);
    }

    [Fact]
    public async Task ReorderSectionsAsync_updates_section_sort_orders()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuManagementRepository(context.DbContext);

        await repository.ReorderSectionsAsync([new SaveMenuSortOrderRequest(BurgersSectionId, 42)]);
        await repository.SaveChangesAsync();

        var burgers = await context.DbContext.MenuSections
            .AsNoTracking()
            .SingleAsync(section => section.MenuSectionId == BurgersSectionId);

        Assert.Equal(42, burgers.SortOrder);
    }

    [Fact]
    public async Task UpsertRecurringSpecialAsync_persists_linked_menu_item_reference()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuManagementRepository(context.DbContext);

        var specialId = await repository.UpsertRecurringSpecialAsync(
            new SaveRecurringSpecialRequest(
                null,
                MenuTab.Dinner,
                BurgersSectionId,
                DayOfWeek.Saturday,
                "Burger Night",
                "Choose your burger and fries.",
                "After 5:00 PM",
                "$12 combo",
                ClassicHamburgerItemId,
                1,
                true,
                false));
        await repository.SaveChangesAsync();

        var special = await context.DbContext.RecurringSpecials
            .AsNoTracking()
            .SingleAsync(item => item.RecurringSpecialId == specialId);

        Assert.Equal(MenuTab.Dinner, special.Tab);
        Assert.Equal(BurgersSectionId, special.MenuSectionId);
        Assert.Equal("Burger Night", special.Title);
        Assert.Equal("Choose your burger and fries.", special.Description);
        Assert.Equal(ClassicHamburgerItemId, special.LinkedMenuItemId);
    }
}
