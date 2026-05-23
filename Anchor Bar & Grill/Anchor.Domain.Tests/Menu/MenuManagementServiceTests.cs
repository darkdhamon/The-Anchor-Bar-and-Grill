using Anchor.Domain.Menu;

namespace Anchor.Domain.Tests.Menu;

public sealed class MenuManagementServiceTests
{
    private static readonly Guid FoodSectionId = Guid.Parse("6D0FA124-F2D4-457D-9E38-4402A5FD8D2A");
    private static readonly Guid BreakfastSectionId = Guid.Parse("2BB14995-EF90-44C3-95E6-A7CC7E8FA128");
    private static readonly Guid LunchSectionId = Guid.Parse("4C8D4F8B-0AB8-4D87-80EB-7761D417A2EE");
    private static readonly Guid DrinkSectionId = Guid.Parse("EA9E07D4-D8ED-4856-A367-FF8F8CA8C1FC");
    private static readonly Guid ItemId = Guid.Parse("F0248589-E957-49FF-B878-61E9E931B785");
    private static readonly Guid SecondItemId = Guid.Parse("AF623E03-43BE-4AF9-B929-535BFB14A976");

    [Fact]
    public async Task SaveItemAsync_rejects_drinks_tab_for_food_items()
    {
        var repository = CreateRepositoryWithFoodSection();
        var service = CreateService(repository);

        var result = await service.SaveItemAsync(
            CreateItemRequest(
                null,
                FoodSectionId,
                "Burger",
                "Guest-facing description",
                [MenuTab.Drinks],
                [new SaveMenuItemPriceVariantRequest(null, "Regular", 12m, 1)]));

        Assert.False(result.Succeeded);
        Assert.Contains("cannot be assigned to the Drinks tab", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveItemAsync_rejects_food_tabs_for_drink_items()
    {
        var repository = new FakeMenuManagementRepository
        {
            SectionReferences =
            {
                [DrinkSectionId] = CreateSectionReference(DrinkSectionId, MenuFamily.Drink, [MenuTab.Drinks])
            }
        };
        var service = CreateService(repository);

        var result = await service.SaveItemAsync(
            CreateItemRequest(
                null,
                DrinkSectionId,
                "House Old Fashioned",
                "Built for slow sipping.",
                [MenuTab.Lunch],
                [new SaveMenuItemPriceVariantRequest(null, "Regular", 9m, 1)]));

        Assert.False(result.Succeeded);
        Assert.Contains("cannot be assigned to Breakfast, Lunch, or Dinner", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveItemAsync_requires_at_least_one_price_variant()
    {
        var repository = CreateRepositoryWithFoodSection();
        var service = CreateService(repository);

        var result = await service.SaveItemAsync(
            CreateItemRequest(
                null,
                FoodSectionId,
                "Burger",
                "Guest-facing description",
                [MenuTab.Lunch],
                []));

        Assert.False(result.Succeeded);
        Assert.Contains("at least one price variant", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveItemAsync_allows_blank_description()
    {
        var repository = CreateRepositoryWithFoodSection();
        var service = CreateService(repository);

        var result = await service.SaveItemAsync(
            CreateItemRequest(
                null,
                FoodSectionId,
                "Pepsi",
                string.Empty,
                [MenuTab.Lunch],
                [new SaveMenuItemPriceVariantRequest(null, "Regular", 3m, 1)]));

        Assert.True(result.Succeeded);
        Assert.NotNull(repository.LastItemRequest);
        Assert.Equal(string.Empty, repository.LastItemRequest!.Description);
    }

    [Fact]
    public async Task SaveItemAsync_allows_multiple_sections_when_item_tabs_fit_within_section_defaults()
    {
        var repository = new FakeMenuManagementRepository
        {
            SectionReferences =
            {
                [BreakfastSectionId] = CreateSectionReference(BreakfastSectionId, MenuFamily.Food, [MenuTab.Breakfast]),
                [LunchSectionId] = CreateSectionReference(LunchSectionId, MenuFamily.Food, [MenuTab.Lunch, MenuTab.Dinner])
            }
        };
        var service = CreateService(repository);

        var result = await service.SaveItemAsync(
            CreateItemRequest(
                null,
                "Everything Toast",
                "Breakfast and lunch feature.",
                [MenuTab.Breakfast, MenuTab.Lunch],
                [new SaveMenuItemPriceVariantRequest(null, "Regular", 12m, 1)],
                [new SaveMenuItemSectionAssignmentRequest(BreakfastSectionId, 1), new SaveMenuItemSectionAssignmentRequest(LunchSectionId, 3)]));

        Assert.True(result.Succeeded);
        Assert.NotNull(repository.LastItemRequest);
        Assert.Equal([MenuTab.Breakfast, MenuTab.Lunch], repository.LastItemRequest!.MenuTabs);
        Assert.Equal(
            [BreakfastSectionId, LunchSectionId],
            repository.LastItemRequest.SectionAssignments.Select(assignment => assignment.SectionId).ToArray());
    }

    [Fact]
    public async Task SaveItemAsync_rejects_item_visibility_outside_selected_section_defaults()
    {
        var repository = new FakeMenuManagementRepository
        {
            SectionReferences =
            {
                [BreakfastSectionId] = CreateSectionReference(BreakfastSectionId, MenuFamily.Food, [MenuTab.Breakfast]),
                [LunchSectionId] = CreateSectionReference(LunchSectionId, MenuFamily.Food, [MenuTab.Lunch])
            }
        };
        var service = CreateService(repository);

        var result = await service.SaveItemAsync(
            CreateItemRequest(
                null,
                "Everything Toast",
                "Breakfast and lunch feature.",
                [MenuTab.Breakfast, MenuTab.Dinner],
                [new SaveMenuItemPriceVariantRequest(null, "Regular", 12m, 1)],
                [new SaveMenuItemSectionAssignmentRequest(BreakfastSectionId, 1), new SaveMenuItemSectionAssignmentRequest(LunchSectionId, 3)]));

        Assert.False(result.Succeeded);
        Assert.Contains("not allowed by the selected sections", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveSectionAsync_rejects_duplicate_section_names_after_trim_and_case_normalization()
    {
        var repository = new FakeMenuManagementRepository();
        repository.SectionNameMatches[MenuNameRules.NormalizeForLookup("Appetizers")] = Guid.NewGuid();
        var service = CreateService(repository);

        var result = await service.SaveSectionAsync(
            new SaveMenuSectionRequest(
                null,
                "  appetizers  ",
                "Shareables",
                MenuFamily.Food,
                [MenuTab.Lunch, MenuTab.Dinner],
                1,
                true,
                false));

        Assert.False(result.Succeeded);
        Assert.Contains("already exists", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveItemAsync_rejects_duplicate_item_names_after_trim_and_case_normalization()
    {
        var repository = CreateRepositoryWithFoodSection();
        repository.ItemNameMatches[MenuNameRules.NormalizeForLookup("Classic Hamburger")] = SecondItemId;
        var service = CreateService(repository);

        var result = await service.SaveItemAsync(
            CreateItemRequest(
                null,
                FoodSectionId,
                " classic hamburger ",
                string.Empty,
                [MenuTab.Lunch],
                [new SaveMenuItemPriceVariantRequest(null, "Regular", 11m, 1)]));

        Assert.False(result.Succeeded);
        Assert.Contains("already exists", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveSectionAsync_trims_and_persists_optional_callout()
    {
        var repository = new FakeMenuManagementRepository();
        var service = CreateService(repository);

        var result = await service.SaveSectionAsync(
            new SaveMenuSectionRequest(
                null,
                "Breakfast Plates",
                "  Includes choice of Bloody Mary or Screwdriver  ",
                MenuFamily.Food,
                [MenuTab.Breakfast],
                1,
                true,
                false));

        Assert.True(result.Succeeded);
        Assert.NotNull(repository.LastSectionRequest);
        Assert.Equal("Includes choice of Bloody Mary or Screwdriver", repository.LastSectionRequest!.Callout);
    }

    [Fact]
    public async Task SaveSectionAsync_rejects_parent_section_from_a_different_family()
    {
        var repository = new FakeMenuManagementRepository
        {
            SectionReferences =
            {
                [DrinkSectionId] = CreateSectionReference(DrinkSectionId, MenuFamily.Drink, [MenuTab.Drinks])
            }
        };
        var service = CreateService(repository);

        var result = await service.SaveSectionAsync(
            new SaveMenuSectionRequest(
                null,
                "Breakfast Specials",
                null,
                MenuFamily.Food,
                DrinkSectionId,
                [MenuTab.Breakfast],
                1,
                true,
                false));

        Assert.False(result.Succeeded);
        Assert.Contains("same menu family", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveItemAsync_requires_weekday_for_weekly_specials()
    {
        var repository = CreateRepositoryWithFoodSection();
        var service = CreateService(repository);

        var result = await service.SaveItemAsync(
            CreateItemRequest(
                null,
                FoodSectionId,
                "Monday Night Burgers",
                "Burger-night feature.",
                [MenuTab.Dinner],
                [new SaveMenuItemPriceVariantRequest(null, "Regular", 11m, 1)],
                new SaveMenuItemSpecialRequest(
                    MenuItemSpecialScheduleKind.WeeklyRecurring,
                    Array.Empty<DayOfWeek>(),
                    null,
                    null,
                    new TimeOnly(17, 0),
                    null,
                    false,
                    "$11 basket special")));

        Assert.False(result.Succeeded);
        Assert.Contains("Choose at least one weekday", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveItemAsync_persists_recurring_season_window_fields()
    {
        var repository = CreateRepositoryWithFoodSection();
        var service = CreateService(repository);

        var result = await service.SaveItemAsync(
            CreateItemRequest(
                null,
                FoodSectionId,
                "Pumpkin Pancakes",
                "Seasonal breakfast stack.",
                [MenuTab.Breakfast],
                [new SaveMenuItemPriceVariantRequest(null, "Regular", 12m, 1)])
            with
            {
                SeasonStartMonth = 10,
                SeasonStartDay = 15,
                SeasonEndMonth = 4,
                SeasonEndDay = 1
            });

        Assert.True(result.Succeeded);
        Assert.NotNull(repository.LastItemRequest);
        Assert.Equal(10, repository.LastItemRequest!.SeasonStartMonth);
        Assert.Equal(15, repository.LastItemRequest.SeasonStartDay);
        Assert.Equal(4, repository.LastItemRequest.SeasonEndMonth);
        Assert.Equal(1, repository.LastItemRequest.SeasonEndDay);
    }

    [Fact]
    public async Task SaveItemAsync_rejects_next_day_special_without_end_time()
    {
        var repository = CreateRepositoryWithFoodSection();
        var service = CreateService(repository);

        var result = await service.SaveItemAsync(
            CreateItemRequest(
                null,
                FoodSectionId,
                "Late Night Wings",
                "Wings after dark.",
                [MenuTab.Dinner],
                [new SaveMenuItemPriceVariantRequest(null, "Regular", 16m, 1)],
                new SaveMenuItemSpecialRequest(
                    MenuItemSpecialScheduleKind.WeeklyRecurring,
                    [DayOfWeek.Wednesday],
                    null,
                    null,
                    new TimeOnly(22, 0),
                    null,
                    true,
                    null)));

        Assert.False(result.Succeeded);
        Assert.Contains("end time", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveItemAsync_preserves_item_offer_fields_for_special_items()
    {
        var repository = CreateRepositoryWithFoodSection();
        var service = CreateService(repository);

        var result = await service.SaveItemAsync(
            CreateItemRequest(
                ItemId,
                FoodSectionId,
                "Wing Night",
                "Sauced wings.",
                [MenuTab.Dinner],
                [new SaveMenuItemPriceVariantRequest(null, "Regular", 16m, 1)],
                new SaveMenuItemSpecialRequest(
                    MenuItemSpecialScheduleKind.WeeklyRecurring,
                    [DayOfWeek.Wednesday],
                    null,
                    null,
                    new TimeOnly(17, 0),
                    null,
                    false,
                    "$16 dozen special"),
                offerStartsOn: new DateOnly(2026, 5, 1),
                offerEndsOn: new DateOnly(2026, 5, 31),
                isSeasonal: true));

        Assert.True(result.Succeeded);
        Assert.NotNull(repository.LastItemRequest);
        Assert.Equal(new DateOnly(2026, 5, 1), repository.LastItemRequest!.OfferStartsOn);
        Assert.Equal(new DateOnly(2026, 5, 31), repository.LastItemRequest.OfferEndsOn);
        Assert.True(repository.LastItemRequest.IsSeasonal);
        Assert.NotNull(repository.LastItemRequest.Special);
        Assert.Equal("$16 dozen special", repository.LastItemRequest.Special!.Callout);
    }

    [Fact]
    public async Task SaveServiceWindowsAsync_allows_overnight_windows()
    {
        var repository = new FakeMenuManagementRepository();
        var service = CreateService(repository);

        var result = await service.SaveServiceWindowsAsync(
            new SaveMenuServiceWindowRequest(
                MenuTab.Drinks,
                OrderedDays
                    .Select(day => day == DayOfWeek.Friday
                        ? new SaveMenuServiceWindowDayRequest(day, true, new TimeOnly(11, 0), new TimeOnly(0, 0), true)
                        : new SaveMenuServiceWindowDayRequest(day, false, null, null, false))
                    .ToArray()));

        Assert.True(result.Succeeded);
        Assert.NotNull(repository.LastServiceWindowRequest);
        var friday = repository.LastServiceWindowRequest!.Days.Single(day => day.DayOfWeek == DayOfWeek.Friday);
        Assert.True(friday.ClosesNextDay);
        Assert.Equal(new TimeOnly(0, 0), friday.ClosesAt);
    }

    [Fact]
    public async Task ReorderItemsAsync_persists_requested_sort_orders()
    {
        var repository = new FakeMenuManagementRepository
        {
            Snapshot = new MenuManagementSnapshot(
                [],
                [
                    CreateItemRecord(ItemId, MenuFamily.Food, "Burger", 1, [CreateAssignment(FoodSectionId, "Food")]),
                    CreateItemRecord(SecondItemId, MenuFamily.Food, "Fries", 2, [CreateAssignment(FoodSectionId, "Food")])
                ],
                [])
        };
        var service = CreateService(repository);

        var result = await service.ReorderItemsAsync(
            [
                new SaveMenuSortOrderRequest(ItemId, 2, FoodSectionId),
                new SaveMenuSortOrderRequest(SecondItemId, 1, FoodSectionId)
            ]);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                new SaveMenuSortOrderRequest(ItemId, 2, FoodSectionId),
                new SaveMenuSortOrderRequest(SecondItemId, 1, FoodSectionId)
            ],
            repository.LastItemReorderRequests);
    }

    [Fact]
    public async Task ReorderItemsAsync_rejects_duplicate_sort_orders()
    {
        var repository = new FakeMenuManagementRepository
        {
            Snapshot = new MenuManagementSnapshot(
                [],
                [
                    CreateItemRecord(ItemId, MenuFamily.Food, "Burger", 1, [CreateAssignment(FoodSectionId, "Food")]),
                    CreateItemRecord(SecondItemId, MenuFamily.Food, "Fries", 2, [CreateAssignment(FoodSectionId, "Food")])
                ],
                [])
        };
        var service = CreateService(repository);

        var result = await service.ReorderItemsAsync(
            [
                new SaveMenuSortOrderRequest(ItemId, 1),
                new SaveMenuSortOrderRequest(SecondItemId, 1)
            ]);

        Assert.False(result.Succeeded);
        Assert.Contains("must be unique", result.Errors[0], StringComparison.OrdinalIgnoreCase);
        Assert.Null(repository.LastItemReorderRequests);
    }

    [Fact]
    public async Task ReorderSectionsAsync_rejects_missing_section()
    {
        var repository = new FakeMenuManagementRepository
        {
            Snapshot = new MenuManagementSnapshot([], [], [])
        };
        var service = CreateService(repository);

        var result = await service.ReorderSectionsAsync([new SaveMenuSortOrderRequest(FoodSectionId, 1)]);

        Assert.False(result.Succeeded);
        Assert.Contains("could not be found", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    private static readonly DayOfWeek[] OrderedDays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    ];

    private static FakeMenuManagementRepository CreateRepositoryWithFoodSection() =>
        new()
        {
            SectionReferences =
            {
                [FoodSectionId] = CreateSectionReference(FoodSectionId, MenuFamily.Food, [MenuTab.Breakfast, MenuTab.Lunch, MenuTab.Dinner])
            },
            ItemReferences =
            {
                [ItemId] = new MenuItemReferenceRecord(
                    ItemId,
                    MenuFamily.Food,
                    "Existing item",
                    "Existing item description",
                    false,
                    [CreateAssignment(FoodSectionId, "Food")],
                    false,
                    [MenuTab.Dinner],
                    false)
            }
        };

    private static MenuManagementService CreateService(FakeMenuManagementRepository repository) =>
        new(repository, new FakeMenuOperationLogSink());

    private sealed class FakeMenuManagementRepository : IMenuManagementRepository
    {
        public Dictionary<Guid, MenuSectionReferenceRecord> SectionReferences { get; init; } = [];

        public Dictionary<Guid, MenuItemReferenceRecord> ItemReferences { get; } = [];

        public Dictionary<string, Guid> SectionNameMatches { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, Guid> ItemNameMatches { get; } = new(StringComparer.Ordinal);

        public MenuManagementSnapshot Snapshot { get; set; } = new([], [], []);

        public bool SectionHasDependents { get; set; }

        public SaveMenuSectionRequest? LastSectionRequest { get; private set; }

        public SaveMenuItemRequest? LastItemRequest { get; private set; }

        public SaveMenuServiceWindowRequest? LastServiceWindowRequest { get; private set; }

        public IReadOnlyList<SaveMenuSortOrderRequest>? LastSectionReorderRequests { get; private set; }

        public IReadOnlyList<SaveMenuSortOrderRequest>? LastItemReorderRequests { get; private set; }

        public Task<MenuManagementSnapshot> GetMenuManagementSnapshotAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Snapshot);

        public Task<MenuSectionReferenceRecord?> GetSectionReferenceAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(SectionReferences.TryGetValue(sectionId, out var section) ? section : null);

        public Task<IReadOnlyList<MenuSectionReferenceRecord>> GetSectionReferencesAsync(IReadOnlyList<Guid> sectionIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MenuSectionReferenceRecord>>(
                sectionIds
                    .Where(SectionReferences.ContainsKey)
                    .Select(sectionId => SectionReferences[sectionId])
                    .ToArray());

        public Task<MenuItemReferenceRecord?> GetItemReferenceAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ItemReferences.TryGetValue(itemId, out var item) ? item : null);

        public Task<Guid?> FindSectionIdByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default) =>
            Task.FromResult(SectionNameMatches.TryGetValue(normalizedName, out var sectionId) ? (Guid?)sectionId : null);

        public Task<Guid?> FindItemIdByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default) =>
            Task.FromResult(ItemNameMatches.TryGetValue(normalizedName, out var itemId) ? (Guid?)itemId : null);

        public Task<bool> SectionHasDependentsAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(SectionHasDependents);

        public Task<Guid> UpsertSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default)
        {
            LastSectionRequest = request;
            return Task.FromResult(request.SectionId ?? Guid.NewGuid());
        }

        public Task<Guid> UpsertItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default)
        {
            LastItemRequest = request;
            return Task.FromResult(request.ItemId ?? Guid.NewGuid());
        }

        public Task UpsertServiceWindowsAsync(SaveMenuServiceWindowRequest request, CancellationToken cancellationToken = default)
        {
            LastServiceWindowRequest = request;
            return Task.CompletedTask;
        }

        public Task ReorderSectionsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default)
        {
            LastSectionReorderRequests = requests.ToArray();
            return Task.CompletedTask;
        }

        public Task ReorderItemsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default)
        {
            LastItemReorderRequests = requests.ToArray();
            return Task.CompletedTask;
        }

        public Task ArchiveSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeMenuOperationLogSink : IMenuOperationLogSink
    {
        public Task WriteAsync(MenuOperationLogEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private static SaveMenuItemRequest CreateItemRequest(
        Guid? itemId,
        Guid sectionId,
        string name,
        string description,
        IReadOnlyList<MenuTab> menuTabs,
        IReadOnlyList<SaveMenuItemPriceVariantRequest> priceVariants,
        SaveMenuItemSpecialRequest? special = null,
        DateOnly? offerStartsOn = null,
        DateOnly? offerEndsOn = null,
        bool isSeasonal = false,
        bool usesSectionVisibility = false) =>
        CreateItemRequest(
            itemId,
            name,
            description,
            menuTabs,
            priceVariants,
            [new SaveMenuItemSectionAssignmentRequest(sectionId, 1)],
            special,
            offerStartsOn,
            offerEndsOn,
            isSeasonal,
            usesSectionVisibility);

    private static SaveMenuItemRequest CreateItemRequest(
        Guid? itemId,
        string name,
        string description,
        IReadOnlyList<MenuTab> menuTabs,
        IReadOnlyList<SaveMenuItemPriceVariantRequest> priceVariants,
        IReadOnlyList<SaveMenuItemSectionAssignmentRequest> sectionAssignments,
        SaveMenuItemSpecialRequest? special = null,
        DateOnly? offerStartsOn = null,
        DateOnly? offerEndsOn = null,
        bool isSeasonal = false,
        bool usesSectionVisibility = false) =>
        new(
            itemId,
            name,
            description,
            null,
            1,
            true,
            false,
            offerStartsOn,
            offerEndsOn,
            isSeasonal,
            priceVariants,
            sectionAssignments,
            usesSectionVisibility,
            menuTabs,
            special);

    private static MenuSectionReferenceRecord CreateSectionReference(Guid sectionId, MenuFamily family, IReadOnlyList<MenuTab> menuTabs) =>
        new(sectionId, family, menuTabs, false);

    private static MenuItemSectionAssignmentRecord CreateAssignment(Guid sectionId, string sectionName, int sortOrder = 1) =>
        new(sectionId, sectionName, sortOrder);

    private static MenuItemRecord CreateItemRecord(
        Guid itemId,
        MenuFamily family,
        string name,
        int sortOrder,
        IReadOnlyList<MenuItemSectionAssignmentRecord> assignments) =>
        new(
            itemId,
            family,
            name,
            "desc",
            null,
            sortOrder,
            true,
            false,
            null,
            null,
            false,
            [],
            assignments,
            true,
            [],
            null);
}
