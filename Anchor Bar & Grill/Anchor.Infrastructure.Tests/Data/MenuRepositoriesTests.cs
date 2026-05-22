using Anchor.Domain.Menu;
using Anchor.Infrastructure.Data.Menu;
using Anchor.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Anchor.Infrastructure.Tests.Data;

public sealed class MenuRepositoriesTests
{
    private static readonly Guid BurgersSectionId = Guid.Parse("198CCF8A-72FD-4278-A360-F36D5871E58B");
    private static readonly Guid ClassicHamburgerItemId = Guid.Parse("7626D0DF-9F8A-4FE8-9062-3596165E148A");
    private static readonly Guid MondayNightBurgersItemId = Guid.Parse("33D64E7B-D5B7-481A-97FC-7F250A68C27E");

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
        Assert.DoesNotContain(snapshot.Items, item => item.Special is not null);
    }

    [Fact]
    public async Task GetPublicMenuSnapshotAsync_returns_seeded_dinner_special_items()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuQueryRepository(context.DbContext);

        var snapshot = await repository.GetPublicMenuSnapshotAsync(
            MenuTab.Dinner,
            new DateOnly(2026, 5, 18),
            new DateOnly(2026, 6, 17));

        var sectionItems = snapshot.Items
            .Where(item => item.SectionId == BurgersSectionId)
            .OrderByDescending(item => item.Special is not null)
            .ThenBy(item => item.SortOrder)
            .ToArray();

        Assert.Contains(sectionItems, item => item.ItemId == MondayNightBurgersItemId && item.Special is not null);
        Assert.True(sectionItems.First().Special is not null);
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
        Assert.Equal(7, snapshot.ServiceWindows.Count);
        Assert.Contains(snapshot.ServiceWindows, window => window.DayOfWeek == DayOfWeek.Friday && window.ClosesNextDay);
    }

    [Fact]
    public async Task GetPublicServiceWindowsAsync_returns_all_public_service_windows()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuQueryRepository(context.DbContext);

        var windows = await repository.GetPublicServiceWindowsAsync();

        Assert.Equal(28, windows.Count);
        Assert.Contains(windows, window => window.Tab == MenuTab.Drinks && window.DayOfWeek == DayOfWeek.Friday && window.ClosesNextDay);
        Assert.Contains(windows, window => window.Tab == MenuTab.Breakfast && window.DayOfWeek == DayOfWeek.Saturday && window.IsAvailable);
    }

    [Fact]
    public async Task GetMenuManagementSnapshotAsync_includes_hidden_archived_special_items()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var hiddenSectionId = Guid.NewGuid();
        var hiddenItemId = Guid.NewGuid();

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
            Name = "Late Night Old Fashioned",
            Description = "Archived drink profile.",
            SortOrder = 1,
            IsVisibleToGuests = false,
            IsArchived = true
        });
        context.DbContext.MenuItemSpecials.Add(new MenuItemSpecialEntity
        {
            MenuItemId = hiddenItemId,
            ScheduleKind = MenuItemSpecialScheduleKind.WeeklyRecurring,
            DayOfWeek = DayOfWeek.Friday,
            StartDate = new DateOnly(2026, 1, 1),
            StartsAt = new TimeOnly(20, 0),
            Callout = "After 8 PM"
        });
        await context.DbContext.SaveChangesAsync();

        var repository = new MenuManagementRepository(context.DbContext);
        var snapshot = await repository.GetMenuManagementSnapshotAsync();

        Assert.Contains(snapshot.Sections, section => section.SectionId == hiddenSectionId && section.IsArchived);
        Assert.Contains(snapshot.Items, item => item.ItemId == hiddenItemId && item.IsArchived && item.Special is not null);
    }

    [Fact]
    public async Task UpsertItemAsync_persists_price_variants_food_tab_assignments_and_special_extension()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuManagementRepository(context.DbContext);
        var itemId = await repository.UpsertItemAsync(
            new SaveMenuItemRequest(
                null,
                BurgersSectionId,
                "Test Burger Night",
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
                [MenuTab.Dinner],
                new SaveMenuItemSpecialRequest(
                    MenuItemSpecialScheduleKind.WeeklyRecurring,
                    DayOfWeek.Monday,
                    new DateOnly(2026, 1, 1),
                    null,
                    new TimeOnly(17, 0),
                    null,
                    false,
                    "$12 basket special")));
        await repository.SaveChangesAsync();

        var savedItem = await context.DbContext.MenuItems
            .Include(item => item.PriceVariants)
            .Include(item => item.FoodTabs)
            .Include(item => item.Special)
            .SingleAsync(item => item.MenuItemId == itemId);

        Assert.Equal("Test Burger Night", savedItem.Name);
        Assert.Equal(2, savedItem.PriceVariants.Count);
        Assert.Contains(savedItem.FoodTabs, link => link.Tab == MenuTab.Dinner);
        Assert.NotNull(savedItem.Special);
        Assert.Equal(DayOfWeek.Monday, savedItem.Special!.DayOfWeek);
        Assert.Equal("$12 basket special", savedItem.Special.Callout);
    }

    [Fact]
    public async Task UpsertItemAsync_updates_existing_item_without_replacing_price_variant_ids()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuManagementRepository(context.DbContext);

        var existingVariantIds = await context.DbContext.MenuItemPriceVariants
            .Where(variant => variant.MenuItemId == ClassicHamburgerItemId)
            .Select(variant => variant.MenuItemPriceVariantId)
            .OrderBy(id => id)
            .ToArrayAsync();

        await repository.UpsertItemAsync(
            new SaveMenuItemRequest(
                ClassicHamburgerItemId,
                BurgersSectionId,
                "Classic Hamburger",
                "Updated burger description.",
                null,
                7,
                true,
                false,
                null,
                null,
                false,
                [
                    new SaveMenuItemPriceVariantRequest(null, "Regular", 13m, 1)
                ],
                [MenuTab.Lunch, MenuTab.Dinner],
                null));
        await repository.SaveChangesAsync();

        var savedItem = await context.DbContext.MenuItems
            .Include(item => item.PriceVariants)
            .Include(item => item.FoodTabs)
            .SingleAsync(item => item.MenuItemId == ClassicHamburgerItemId);
        var savedVariantIds = savedItem.PriceVariants
            .Select(variant => variant.MenuItemPriceVariantId)
            .OrderBy(id => id)
            .ToArray();

        Assert.Equal(existingVariantIds, savedVariantIds);
        Assert.Equal("Updated burger description.", savedItem.Description);
        Assert.Equal(13m, savedItem.PriceVariants.Single().Amount);
        Assert.Equal([MenuTab.Lunch, MenuTab.Dinner], savedItem.FoodTabs.Select(link => link.Tab).OrderBy(tab => tab).ToArray());
    }

    [Fact]
    public async Task UpsertItemAsync_adds_a_new_price_variant_to_an_existing_item_without_replacing_existing_ids()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuManagementRepository(context.DbContext);

        var existingVariantId = await context.DbContext.MenuItemPriceVariants
            .Where(variant => variant.MenuItemId == ClassicHamburgerItemId)
            .Select(variant => variant.MenuItemPriceVariantId)
            .SingleAsync();

        await repository.UpsertItemAsync(
            new SaveMenuItemRequest(
                ClassicHamburgerItemId,
                BurgersSectionId,
                "Classic Hamburger",
                "Now available with two price options.",
                null,
                7,
                true,
                false,
                null,
                null,
                false,
                [
                    new SaveMenuItemPriceVariantRequest(existingVariantId, "Regular", 13m, 1),
                    new SaveMenuItemPriceVariantRequest(null, "Basket", 16m, 2)
                ],
                [MenuTab.Lunch, MenuTab.Dinner],
                null));
        await repository.SaveChangesAsync();

        var savedItem = await context.DbContext.MenuItems
            .Include(item => item.PriceVariants)
            .SingleAsync(item => item.MenuItemId == ClassicHamburgerItemId);

        Assert.Equal(2, savedItem.PriceVariants.Count);
        Assert.Contains(savedItem.PriceVariants, variant => variant.MenuItemPriceVariantId == existingVariantId && variant.Label == "Regular" && variant.Amount == 13m);
        Assert.Contains(savedItem.PriceVariants, variant => variant.MenuItemPriceVariantId != existingVariantId && variant.Label == "Basket" && variant.Amount == 16m);
    }

    [Fact]
    public async Task UpsertItemAsync_clears_stale_tracked_price_variants_before_updating_an_existing_item()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuManagementRepository(context.DbContext);

        var trackedItem = await context.DbContext.MenuItems
            .Include(item => item.PriceVariants)
            .SingleAsync(item => item.MenuItemId == ClassicHamburgerItemId);
        trackedItem.PriceVariants.Add(new MenuItemPriceVariantEntity
        {
            MenuItemPriceVariantId = Guid.NewGuid(),
            MenuItemId = ClassicHamburgerItemId,
            Label = "Phantom",
            Amount = 99m,
            SortOrder = 99
        });

        var existingVariantId = trackedItem.PriceVariants
            .Where(variant => variant.Label != "Phantom")
            .Select(variant => variant.MenuItemPriceVariantId)
            .Single();

        await repository.UpsertItemAsync(
            new SaveMenuItemRequest(
                ClassicHamburgerItemId,
                BurgersSectionId,
                "Classic Hamburger",
                "Updated after stale tracked state.",
                null,
                7,
                true,
                false,
                null,
                null,
                false,
                [
                    new SaveMenuItemPriceVariantRequest(existingVariantId, "Regular", 14m, 1)
                ],
                [MenuTab.Lunch, MenuTab.Dinner],
                null));
        await repository.SaveChangesAsync();

        var savedItem = await context.DbContext.MenuItems
            .AsNoTracking()
            .Include(item => item.PriceVariants)
            .SingleAsync(item => item.MenuItemId == ClassicHamburgerItemId);

        Assert.Single(savedItem.PriceVariants);
        Assert.DoesNotContain(savedItem.PriceVariants, variant => variant.Label == "Phantom");
        Assert.Equal(14m, savedItem.PriceVariants.Single().Amount);
    }

    [Fact]
    public async Task SectionHasDependentsAsync_and_GetItemReferenceAsync_report_seeded_references()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuManagementRepository(context.DbContext);

        Assert.True(await repository.SectionHasDependentsAsync(BurgersSectionId));

        var reference = await repository.GetItemReferenceAsync(MondayNightBurgersItemId);
        Assert.NotNull(reference);
        Assert.True(reference!.HasSpecial);
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
}
