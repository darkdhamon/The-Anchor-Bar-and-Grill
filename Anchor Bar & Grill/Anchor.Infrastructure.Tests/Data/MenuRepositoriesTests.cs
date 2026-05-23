using Anchor.Domain.Menu;
using Anchor.Infrastructure.Data.Menu;
using Anchor.Infrastructure.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Anchor.Infrastructure.Tests.Data;

public sealed class MenuRepositoriesTests
{
    private static readonly Guid BurgersSectionId = Guid.Parse("198CCF8A-72FD-4278-A360-F36D5871E58B");
    private static readonly Guid SoupsSectionId = Guid.Parse("31E9CB99-5FCA-4A4A-A04B-89B97C926A52");
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
            .Where(item => item.SectionAssignments.Any(assignment => assignment.SectionId == BurgersSectionId))
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
    public async Task GetPublicMenuSnapshotAsync_respects_multi_section_visibility_and_item_overrides()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();

        var breakfastSectionId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        context.DbContext.MenuSections.Add(new MenuSectionEntity
        {
            MenuSectionId = breakfastSectionId,
            Name = "Breakfast Specials",
            NormalizedName = MenuNameRules.NormalizeForLookup("Breakfast Specials"),
            Family = MenuFamily.Food,
            SortOrder = 20,
            IsVisibleToGuests = true,
            IsArchived = false
        });
        context.DbContext.MenuSectionTabs.Add(new MenuSectionTabEntity
        {
            MenuSectionId = breakfastSectionId,
            Tab = MenuTab.Breakfast
        });

        context.DbContext.MenuItems.Add(new MenuItemEntity
        {
            MenuItemId = itemId,
            Name = "Everything Toast",
            NormalizedName = MenuNameRules.NormalizeForLookup("Everything Toast"),
            Description = "Breakfast special that crosses into lunch.",
            SortOrder = 50,
            IsVisibleToGuests = true,
            IsArchived = false,
            UsesSectionVisibility = false
        });
        context.DbContext.MenuItemSectionAssignments.AddRange(
            new MenuItemSectionAssignmentEntity
            {
                MenuItemId = itemId,
                MenuSectionId = breakfastSectionId,
                SortOrder = 1
            },
            new MenuItemSectionAssignmentEntity
            {
                MenuItemId = itemId,
                MenuSectionId = SoupsSectionId,
                SortOrder = 8
            });
        context.DbContext.MenuItemTabs.AddRange(
            new MenuItemTabEntity
            {
                MenuItemId = itemId,
                Tab = MenuTab.Breakfast
            },
            new MenuItemTabEntity
            {
                MenuItemId = itemId,
                Tab = MenuTab.Lunch
            });
        context.DbContext.MenuItemPriceVariants.Add(new MenuItemPriceVariantEntity
        {
            MenuItemPriceVariantId = Guid.NewGuid(),
            MenuItemId = itemId,
            Label = "Regular",
            Amount = 9m,
            SortOrder = 1
        });
        await context.DbContext.SaveChangesAsync();

        var repository = new MenuQueryRepository(context.DbContext);

        var breakfastSnapshot = await repository.GetPublicMenuSnapshotAsync(
            MenuTab.Breakfast,
            new DateOnly(2026, 5, 22),
            new DateOnly(2026, 6, 21));
        var lunchSnapshot = await repository.GetPublicMenuSnapshotAsync(
            MenuTab.Lunch,
            new DateOnly(2026, 5, 22),
            new DateOnly(2026, 6, 21));
        var dinnerSnapshot = await repository.GetPublicMenuSnapshotAsync(
            MenuTab.Dinner,
            new DateOnly(2026, 5, 22),
            new DateOnly(2026, 6, 21));

        Assert.Contains(breakfastSnapshot.Sections, section => section.SectionId == breakfastSectionId);
        Assert.Contains(breakfastSnapshot.Items, item => item.ItemId == itemId);

        Assert.Contains(lunchSnapshot.Sections, section => section.SectionId == SoupsSectionId);
        Assert.Contains(lunchSnapshot.Items, item => item.ItemId == itemId);

        Assert.DoesNotContain(dinnerSnapshot.Items, item => item.ItemId == itemId);
    }

    [Fact]
    public async Task GetPublicMenuSnapshotAsync_includes_visible_ancestor_sections_for_descendant_content()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();

        var rootSectionId = Guid.NewGuid();
        var childSectionId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        context.DbContext.MenuSections.AddRange(
            new MenuSectionEntity
            {
                MenuSectionId = rootSectionId,
                Name = "Breakfast Specials",
                NormalizedName = MenuNameRules.NormalizeForLookup("Breakfast Specials"),
                Family = MenuFamily.Food,
                SortOrder = 90,
                IsVisibleToGuests = true,
                IsArchived = false
            },
            new MenuSectionEntity
            {
                MenuSectionId = childSectionId,
                Name = "Omelets",
                NormalizedName = MenuNameRules.NormalizeForLookup("Omelets"),
                Family = MenuFamily.Food,
                ParentSectionId = rootSectionId,
                SortOrder = 91,
                IsVisibleToGuests = true,
                IsArchived = false
            });
        context.DbContext.MenuSectionTabs.AddRange(
            new MenuSectionTabEntity
            {
                MenuSectionId = rootSectionId,
                Tab = MenuTab.Breakfast
            },
            new MenuSectionTabEntity
            {
                MenuSectionId = childSectionId,
                Tab = MenuTab.Breakfast
            });
        context.DbContext.MenuItems.Add(new MenuItemEntity
        {
            MenuItemId = itemId,
            Name = "Ham & Cheese",
            NormalizedName = MenuNameRules.NormalizeForLookup("Ham & Cheese"),
            Description = "Classic omelet.",
            SortOrder = 1,
            IsVisibleToGuests = true,
            IsArchived = false
        });
        context.DbContext.MenuItemSectionAssignments.Add(new MenuItemSectionAssignmentEntity
        {
            MenuItemId = itemId,
            MenuSectionId = childSectionId,
            SortOrder = 1
        });
        context.DbContext.MenuItemTabs.Add(new MenuItemTabEntity
        {
            MenuItemId = itemId,
            Tab = MenuTab.Breakfast
        });
        context.DbContext.MenuItemPriceVariants.Add(new MenuItemPriceVariantEntity
        {
            MenuItemPriceVariantId = Guid.NewGuid(),
            MenuItemId = itemId,
            Label = "Regular",
            Amount = 13m,
            SortOrder = 1
        });
        await context.DbContext.SaveChangesAsync();

        var repository = new MenuQueryRepository(context.DbContext);

        var snapshot = await repository.GetPublicMenuSnapshotAsync(
            MenuTab.Breakfast,
            new DateOnly(2026, 5, 22),
            new DateOnly(2026, 6, 21));

        Assert.Contains(snapshot.Sections, section => section.SectionId == rootSectionId);
        Assert.Contains(snapshot.Sections, section => section.SectionId == childSectionId);
        Assert.Contains(snapshot.Items, item => item.ItemId == itemId);
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
            NormalizedName = MenuNameRules.NormalizeForLookup("Hidden Cocktails"),
            Family = MenuFamily.Drink,
            SortOrder = 9,
            IsVisibleToGuests = false,
            IsArchived = true
        });
        context.DbContext.MenuItems.Add(new MenuItemEntity
        {
            MenuItemId = hiddenItemId,
            Name = "Late Night Old Fashioned",
            NormalizedName = MenuNameRules.NormalizeForLookup("Late Night Old Fashioned"),
            Description = "Archived drink profile.",
            SortOrder = 1,
            IsVisibleToGuests = false,
            IsArchived = true
        });
        context.DbContext.MenuItemSectionAssignments.Add(new MenuItemSectionAssignmentEntity
        {
            MenuItemId = hiddenItemId,
            MenuSectionId = hiddenSectionId,
            SortOrder = 1
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
            CreateItemRequest(
                null,
                "Test Burger Night",
                "Built for repository coverage.",
                99,
                [
                    new SaveMenuItemPriceVariantRequest(null, "Single", 12m, 1),
                    new SaveMenuItemPriceVariantRequest(null, "Double", 15m, 2)
                ],
                [new SaveMenuItemSectionAssignmentRequest(BurgersSectionId, 99)],
                false,
                [MenuTab.Dinner],
                new SaveMenuItemSpecialRequest(
                    MenuItemSpecialScheduleKind.WeeklyRecurring,
                    [DayOfWeek.Monday],
                    null,
                    null,
                    new TimeOnly(17, 0),
                    null,
                    false,
                    "$12 basket special")));
        await repository.SaveChangesAsync();

        var savedItem = await context.DbContext.MenuItems
            .Include(item => item.PriceVariants)
            .Include(item => item.MenuTabs)
            .Include(item => item.SectionAssignments)
            .Include(item => item.Special!)
                .ThenInclude(special => special.Days)
            .SingleAsync(item => item.MenuItemId == itemId);

        Assert.Equal("Test Burger Night", savedItem.Name);
        Assert.Equal(2, savedItem.PriceVariants.Count);
        Assert.Contains(savedItem.MenuTabs, link => link.Tab == MenuTab.Dinner);
        Assert.Contains(savedItem.SectionAssignments, assignment => assignment.MenuSectionId == BurgersSectionId);
        Assert.NotNull(savedItem.Special);
        Assert.Equal([DayOfWeek.Monday], savedItem.Special!.Days.Select(day => day.DayOfWeek).ToArray());
        Assert.Null(savedItem.Special.StartDate);
        Assert.Equal("$12 basket special", savedItem.Special.Callout);
    }

    [Fact]
    public async Task UpsertSectionAsync_persists_parent_section_relationship()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuManagementRepository(context.DbContext);

        var childSectionId = await repository.UpsertSectionAsync(
            new SaveMenuSectionRequest(
                null,
                "Burger Specials",
                "Rotating burger features.",
                MenuFamily.Food,
                BurgersSectionId,
                [MenuTab.Lunch, MenuTab.Dinner],
                25,
                true,
                false));
        await repository.SaveChangesAsync();

        var savedSection = await context.DbContext.MenuSections.SingleAsync(section => section.MenuSectionId == childSectionId);

        Assert.Equal(BurgersSectionId, savedSection.ParentSectionId);
    }

    [Fact]
    public async Task UpsertItemAsync_persists_recurring_season_fields()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuManagementRepository(context.DbContext);

        var itemId = await repository.UpsertItemAsync(
            CreateItemRequest(
                null,
                "Pumpkin Pancakes",
                "Seasonal breakfast stack.",
                25,
                [new SaveMenuItemPriceVariantRequest(null, "Regular", 12m, 1)],
                [new SaveMenuItemSectionAssignmentRequest(BurgersSectionId, 25)],
                false,
                [MenuTab.Dinner],
                null)
            with
            {
                OfferStartsOn = new DateOnly(2026, 10, 1),
                OfferEndsOn = new DateOnly(2027, 4, 15),
                SeasonStartMonth = 10,
                SeasonStartDay = 15,
                SeasonEndMonth = 4,
                SeasonEndDay = 1
            });
        await repository.SaveChangesAsync();

        var savedItem = await context.DbContext.MenuItems.SingleAsync(item => item.MenuItemId == itemId);

        Assert.Equal(10, savedItem.SeasonStartMonth);
        Assert.Equal(15, savedItem.SeasonStartDay);
        Assert.Equal(4, savedItem.SeasonEndMonth);
        Assert.Equal(1, savedItem.SeasonEndDay);
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
            CreateItemRequest(
                ClassicHamburgerItemId,
                "Classic Hamburger",
                "Updated burger description.",
                7,
                [
                    new SaveMenuItemPriceVariantRequest(null, "Regular", 13m, 1)
                ],
                [new SaveMenuItemSectionAssignmentRequest(BurgersSectionId, 7)],
                false,
                [MenuTab.Lunch, MenuTab.Dinner],
                null));
        await repository.SaveChangesAsync();

        var savedItem = await context.DbContext.MenuItems
            .Include(item => item.PriceVariants)
            .Include(item => item.MenuTabs)
            .SingleAsync(item => item.MenuItemId == ClassicHamburgerItemId);
        var savedVariantIds = savedItem.PriceVariants
            .Select(variant => variant.MenuItemPriceVariantId)
            .OrderBy(id => id)
            .ToArray();

        Assert.Equal(existingVariantIds, savedVariantIds);
        Assert.Equal("Updated burger description.", savedItem.Description);
        Assert.Equal(13m, savedItem.PriceVariants.Single().Amount);
        Assert.Equal([MenuTab.Lunch, MenuTab.Dinner], savedItem.MenuTabs.Select(link => link.Tab).OrderBy(tab => tab).ToArray());
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
            CreateItemRequest(
                ClassicHamburgerItemId,
                "Classic Hamburger",
                "Now available with two price options.",
                7,
                [
                    new SaveMenuItemPriceVariantRequest(existingVariantId, "Regular", 13m, 1),
                    new SaveMenuItemPriceVariantRequest(null, "Basket", 16m, 2)
                ],
                [new SaveMenuItemSectionAssignmentRequest(BurgersSectionId, 7)],
                false,
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
            CreateItemRequest(
                ClassicHamburgerItemId,
                "Classic Hamburger",
                "Updated after stale tracked state.",
                7,
                [
                    new SaveMenuItemPriceVariantRequest(existingVariantId, "Regular", 14m, 1)
                ],
                [new SaveMenuItemSectionAssignmentRequest(BurgersSectionId, 7)],
                false,
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
    public async Task FindNormalizedNameLookups_return_matching_section_and_item_ids()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();
        var repository = new MenuManagementRepository(context.DbContext);

        var sectionId = await repository.FindSectionIdByNormalizedNameAsync(MenuNameRules.NormalizeForLookup("burgers"));
        var itemId = await repository.FindItemIdByNormalizedNameAsync(MenuNameRules.NormalizeForLookup(" classic hamburger "));

        Assert.Equal(BurgersSectionId, sectionId);
        Assert.Equal(ClassicHamburgerItemId, itemId);
    }

    [Fact]
    public async Task Unique_indexes_on_normalized_names_reject_duplicate_sections_and_items()
    {
        await using var context = await SqliteIdentityTestContext.CreateAsync();

        context.DbContext.MenuSections.Add(new MenuSectionEntity
        {
            MenuSectionId = Guid.NewGuid(),
            Name = "Burgers Copy",
            NormalizedName = MenuNameRules.NormalizeForLookup("burgers"),
            Family = MenuFamily.Food,
            SortOrder = 99,
            IsVisibleToGuests = true,
            IsArchived = false
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.DbContext.SaveChangesAsync());

        context.DbContext.ChangeTracker.Clear();

        context.DbContext.MenuItems.Add(new MenuItemEntity
        {
            MenuItemId = Guid.NewGuid(),
            Name = "Classic Hamburger Copy",
            NormalizedName = MenuNameRules.NormalizeForLookup("classic hamburger"),
            Description = "Duplicate item name test.",
            SortOrder = 99,
            IsVisibleToGuests = true,
            IsArchived = false
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.DbContext.SaveChangesAsync());
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

        await repository.ReorderItemsAsync([new SaveMenuSortOrderRequest(ClassicHamburgerItemId, 99, BurgersSectionId)]);
        await repository.SaveChangesAsync();

        var savedAssignment = await context.DbContext.MenuItemSectionAssignments
            .AsNoTracking()
            .SingleAsync(assignment => assignment.MenuItemId == ClassicHamburgerItemId && assignment.MenuSectionId == BurgersSectionId);
        var afterVariantIds = await context.DbContext.MenuItemPriceVariants
            .Where(variant => variant.MenuItemId == ClassicHamburgerItemId)
            .Select(variant => variant.MenuItemPriceVariantId)
            .OrderBy(id => id)
            .ToArrayAsync();

        Assert.Equal(99, savedAssignment.SortOrder);
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

    private static SaveMenuItemRequest CreateItemRequest(
        Guid? itemId,
        string name,
        string description,
        int sortOrder,
        IReadOnlyList<SaveMenuItemPriceVariantRequest> priceVariants,
        IReadOnlyList<SaveMenuItemSectionAssignmentRequest> sectionAssignments,
        bool usesSectionVisibility,
        IReadOnlyList<MenuTab> menuTabs,
        SaveMenuItemSpecialRequest? special) =>
        new(
            itemId,
            name,
            description,
            null,
            sortOrder,
            true,
            false,
            null,
            null,
            false,
            priceVariants,
            sectionAssignments,
            usesSectionVisibility,
            menuTabs,
            special);
}
