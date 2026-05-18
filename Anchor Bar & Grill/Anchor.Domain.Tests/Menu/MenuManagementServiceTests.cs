using Anchor.Domain.Menu;

namespace Anchor.Domain.Tests.Menu;

public sealed class MenuManagementServiceTests
{
    private static readonly Guid FoodSectionId = Guid.Parse("6D0FA124-F2D4-457D-9E38-4402A5FD8D2A");
    private static readonly Guid DrinkSectionId = Guid.Parse("EA9E07D4-D8ED-4856-A367-FF8F8CA8C1FC");
    private static readonly Guid ItemId = Guid.Parse("F0248589-E957-49FF-B878-61E9E931B785");
    private static readonly Guid SecondItemId = Guid.Parse("AF623E03-43BE-4AF9-B929-535BFB14A976");
    private static readonly Guid SpecialId = Guid.Parse("0EBE4D22-A04B-43E2-95AB-C86750001D9C");

    [Fact]
    public async Task SaveItemAsync_rejects_drinks_tab_for_food_items()
    {
        var repository = new FakeMenuManagementRepository
        {
            SectionReferences =
            {
                [FoodSectionId] = new MenuSectionReferenceRecord(FoodSectionId, MenuFamily.Food, false)
            }
        };
        var service = new MenuManagementService(repository, new FakeMenuOperationLogSink());

        var result = await service.SaveItemAsync(
            new SaveMenuItemRequest(
                null,
                FoodSectionId,
                "Burger",
                "Guest-facing description",
                null,
                1,
                true,
                false,
                null,
                null,
                false,
                [new SaveMenuItemPriceVariantRequest(null, "Regular", 12m, 1)],
                [MenuTab.Drinks]));

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
                [DrinkSectionId] = new MenuSectionReferenceRecord(DrinkSectionId, MenuFamily.Drink, false)
            }
        };
        var service = new MenuManagementService(repository, new FakeMenuOperationLogSink());

        var result = await service.SaveItemAsync(
            new SaveMenuItemRequest(
                null,
                DrinkSectionId,
                "House Old Fashioned",
                "Built for slow sipping.",
                null,
                1,
                true,
                false,
                null,
                null,
                false,
                [new SaveMenuItemPriceVariantRequest(null, "Regular", 9m, 1)],
                [MenuTab.Lunch]));

        Assert.False(result.Succeeded);
        Assert.Contains("cannot be assigned to Breakfast, Lunch, or Dinner", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveItemAsync_requires_at_least_one_price_variant()
    {
        var repository = new FakeMenuManagementRepository
        {
            SectionReferences =
            {
                [FoodSectionId] = new MenuSectionReferenceRecord(FoodSectionId, MenuFamily.Food, false)
            }
        };
        var service = new MenuManagementService(repository, new FakeMenuOperationLogSink());

        var result = await service.SaveItemAsync(
            new SaveMenuItemRequest(
                null,
                FoodSectionId,
                "Burger",
                "Guest-facing description",
                null,
                1,
                true,
                false,
                null,
                null,
                false,
                [],
                [MenuTab.Lunch]));

        Assert.False(result.Succeeded);
        Assert.Contains("at least one price variant", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveRecurringSpecialAsync_requires_linked_menu_item()
    {
        var repository = new FakeMenuManagementRepository();
        var service = new MenuManagementService(repository, new FakeMenuOperationLogSink());

        var result = await service.SaveRecurringSpecialAsync(
            new SaveRecurringSpecialRequest(
                null,
                MenuTab.Dinner,
                FoodSectionId,
                DayOfWeek.Friday,
                "Fish Fry",
                "Friday night feature.",
                "After 4:00 PM",
                "$15 plate",
                null,
                1,
                true,
                false));

        Assert.False(result.Succeeded);
        Assert.Contains("choose a menu item", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveRecurringSpecialAsync_uses_linked_item_metadata()
    {
        var repository = new FakeMenuManagementRepository
        {
            ItemReferences =
            {
                [ItemId] = new MenuItemReferenceRecord(ItemId, FoodSectionId, MenuFamily.Food, "Classic Hamburger", "Fresh hand-pattied burger.", false, [MenuTab.Dinner])
            }
        };
        var service = new MenuManagementService(repository, new FakeMenuOperationLogSink());

        var result = await service.SaveRecurringSpecialAsync(
            new SaveRecurringSpecialRequest(
                null,
                MenuTab.Dinner,
                Guid.Empty,
                DayOfWeek.Friday,
                "Ignored title",
                "Ignored description",
                "After 4:00 PM",
                "$15 plate",
                ItemId,
                1,
                true,
                false));

        Assert.True(result.Succeeded);
        Assert.NotNull(repository.LastRecurringSpecialRequest);
        Assert.Equal(FoodSectionId, repository.LastRecurringSpecialRequest!.SectionId);
        Assert.Equal("Classic Hamburger", repository.LastRecurringSpecialRequest.Title);
        Assert.Equal("Fresh hand-pattied burger.", repository.LastRecurringSpecialRequest.Description);
    }

    [Fact]
    public async Task DeleteItemAsync_rejects_when_item_has_linked_specials()
    {
        var repository = new FakeMenuManagementRepository
        {
            ItemReferences =
            {
                [ItemId] = new MenuItemReferenceRecord(ItemId, FoodSectionId, MenuFamily.Food, "Classic Hamburger", "Fresh hand-pattied burger.", false, [MenuTab.Dinner])
            },
            ItemHasLinkedSpecials = true
        };
        var service = new MenuManagementService(repository, new FakeMenuOperationLogSink());

        var result = await service.DeleteItemAsync(ItemId);

        Assert.False(result.Succeeded);
        Assert.Contains("Remove or update recurring specials", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveSectionAsync_rejects_family_change_when_section_has_dependents()
    {
        var repository = new FakeMenuManagementRepository
        {
            SectionReferences =
            {
                [FoodSectionId] = new MenuSectionReferenceRecord(FoodSectionId, MenuFamily.Food, false)
            },
            SectionHasDependents = true
        };
        var service = new MenuManagementService(repository, new FakeMenuOperationLogSink());

        var result = await service.SaveSectionAsync(
            new SaveMenuSectionRequest(
                FoodSectionId,
                "Appetizers",
                MenuFamily.Drink,
                1,
                true,
                false));

        Assert.False(result.Succeeded);
        Assert.Contains("before changing it from food to drink", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveServiceWindowsAsync_rejects_available_days_without_times()
    {
        var repository = new FakeMenuManagementRepository();
        var service = new MenuManagementService(repository, new FakeMenuOperationLogSink());

        var result = await service.SaveServiceWindowsAsync(
            new SaveMenuServiceWindowRequest(
                MenuTab.Drinks,
                OrderedDays
                    .Select(day => new SaveMenuServiceWindowDayRequest(day, day == DayOfWeek.Friday, day == DayOfWeek.Friday ? null : new TimeOnly(11, 0), null, false))
                    .ToArray()));

        Assert.False(result.Succeeded);
        Assert.Contains("both opening and closing times", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveServiceWindowsAsync_allows_overnight_windows()
    {
        var repository = new FakeMenuManagementRepository();
        var service = new MenuManagementService(repository, new FakeMenuOperationLogSink());

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
                    new MenuItemRecord(ItemId, FoodSectionId, "Food", MenuFamily.Food, "Burger", "desc", null, 1, true, false, null, null, false, [], []),
                    new MenuItemRecord(SecondItemId, FoodSectionId, "Food", MenuFamily.Food, "Fries", "desc", null, 2, true, false, null, null, false, [], [])
                ],
                [],
                [])
        };
        var service = new MenuManagementService(repository, new FakeMenuOperationLogSink());

        var result = await service.ReorderItemsAsync(
            [
                new SaveMenuSortOrderRequest(ItemId, 2),
                new SaveMenuSortOrderRequest(SecondItemId, 1)
            ]);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                new SaveMenuSortOrderRequest(ItemId, 2),
                new SaveMenuSortOrderRequest(SecondItemId, 1)
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
                    new MenuItemRecord(ItemId, FoodSectionId, "Food", MenuFamily.Food, "Burger", "desc", null, 1, true, false, null, null, false, [], []),
                    new MenuItemRecord(SecondItemId, FoodSectionId, "Food", MenuFamily.Food, "Fries", "desc", null, 2, true, false, null, null, false, [], [])
                ],
                [],
                [])
        };
        var service = new MenuManagementService(repository, new FakeMenuOperationLogSink());

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
            Snapshot = new MenuManagementSnapshot([], [], [], [])
        };
        var service = new MenuManagementService(repository, new FakeMenuOperationLogSink());

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

    private sealed class FakeMenuManagementRepository : IMenuManagementRepository
    {
        public Dictionary<Guid, MenuSectionReferenceRecord> SectionReferences { get; } = [];

        public Dictionary<Guid, MenuItemReferenceRecord> ItemReferences { get; } = [];

        public MenuManagementSnapshot Snapshot { get; set; } = new([], [], [], []);

        public bool SectionHasDependents { get; set; }

        public bool ItemHasLinkedSpecials { get; set; }

        public SaveMenuServiceWindowRequest? LastServiceWindowRequest { get; private set; }

        public SaveRecurringSpecialRequest? LastRecurringSpecialRequest { get; private set; }

        public IReadOnlyList<SaveMenuSortOrderRequest>? LastSectionReorderRequests { get; private set; }

        public IReadOnlyList<SaveMenuSortOrderRequest>? LastItemReorderRequests { get; private set; }

        public IReadOnlyList<SaveMenuSortOrderRequest>? LastSpecialReorderRequests { get; private set; }

        public Task<MenuManagementSnapshot> GetMenuManagementSnapshotAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Snapshot);

        public Task<MenuSectionReferenceRecord?> GetSectionReferenceAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(SectionReferences.TryGetValue(sectionId, out var section) ? section : null);

        public Task<MenuItemReferenceRecord?> GetItemReferenceAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ItemReferences.TryGetValue(itemId, out var item) ? item : null);

        public Task<bool> SectionHasDependentsAsync(Guid sectionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(SectionHasDependents);

        public Task<bool> ItemHasLinkedSpecialsAsync(Guid itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ItemHasLinkedSpecials);

        public Task<Guid> UpsertSectionAsync(SaveMenuSectionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(request.SectionId ?? Guid.NewGuid());

        public Task<Guid> UpsertItemAsync(SaveMenuItemRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(request.ItemId ?? Guid.NewGuid());

        public Task<Guid> UpsertRecurringSpecialAsync(SaveRecurringSpecialRequest request, CancellationToken cancellationToken = default)
        {
            LastRecurringSpecialRequest = request;
            return Task.FromResult(request.SpecialId ?? Guid.NewGuid());
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

        public Task ReorderRecurringSpecialsAsync(IReadOnlyList<SaveMenuSortOrderRequest> requests, CancellationToken cancellationToken = default)
        {
            LastSpecialReorderRequests = requests.ToArray();
            return Task.CompletedTask;
        }

        public Task ArchiveSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteSectionAsync(Guid sectionId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ArchiveItemAsync(Guid itemId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ArchiveRecurringSpecialAsync(Guid specialId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteRecurringSpecialAsync(Guid specialId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeMenuOperationLogSink : IMenuOperationLogSink
    {
        public Task WriteAsync(MenuOperationLogEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
