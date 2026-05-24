using Anchor.Domain.Menu;

namespace Anchor.Domain.Tests.Menu;

public sealed class MenuQueryServiceTests
{
    private static readonly Guid SectionId = Guid.Parse("F11A7F7A-CBF9-4F3B-924E-9C2EDC7969E4");
    private static readonly Guid SecondarySectionId = Guid.Parse("B8B8C2B5-B168-45F2-97B0-EA750485F7D2");

    [Fact]
    public async Task GetPublicMenuAsync_marks_future_items_as_coming_soon_without_limited_time_label()
    {
        var today = new DateOnly(2026, 5, 16);
        var repository = new FakeMenuQueryRepository
        {
            Snapshot = new PublicMenuSnapshot(
                MenuTab.Lunch,
                [CreateSection("Appetizers", [MenuTab.Lunch])],
                [
                    CreateItem(
                        "Mini Tacos",
                        "Served with salsa and sour cream.",
                        1,
                        "Appetizers",
                        [MenuTab.Lunch],
                        today.AddDays(5),
                        today.AddDays(25),
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 9m, 1)])
                ],
                [CreateWindow(MenuTab.Lunch, DayOfWeek.Monday, false, null, null, false)]),
            TabsWithContent = [MenuTab.Lunch]
        };

        var service = new MenuQueryService(repository);

        var result = await service.GetPublicMenuAsync(MenuTab.Lunch, today);

        var item = Assert.Single(Assert.Single(result.Sections).Items);
        Assert.Equal(["Coming Soon"], item.StatusLabels);
        Assert.Equal("Offered May 21 - Jun 10", item.OfferDateSummary);
    }

    [Fact]
    public async Task GetPublicMenuAsync_derives_seasonal_and_limited_time_for_active_items()
    {
        var today = new DateOnly(2026, 5, 16);
        var repository = new FakeMenuQueryRepository
        {
            Snapshot = new PublicMenuSnapshot(
                MenuTab.Lunch,
                [CreateSection("Appetizers", [MenuTab.Lunch])],
                [
                    CreateItem(
                        "Quesadillas",
                        "Loaded with cheese.",
                        1,
                        "Appetizers",
                        [MenuTab.Lunch],
                        today.AddDays(-2),
                        today.AddDays(30),
                        true,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 11m, 1)]),
                    CreateItem(
                        "Fish Tacos",
                        "Boom Boom sauce.",
                        2,
                        "Appetizers",
                        [MenuTab.Lunch],
                        today.AddDays(-4),
                        today.AddDays(18),
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 10m, 1)])
                ],
                [CreateWindow(MenuTab.Lunch, DayOfWeek.Monday, false, null, null, false)]),
            TabsWithContent = [MenuTab.Lunch]
        };

        var service = new MenuQueryService(repository);

        var result = await service.GetPublicMenuAsync(MenuTab.Lunch, today);

        Assert.Collection(
            Assert.Single(result.Sections).Items.OrderBy(item => item.Name).ToArray(),
            first =>
            {
                Assert.Equal("Fish Tacos", first.Name);
                Assert.Equal(["Limited Time"], first.StatusLabels);
            },
            second =>
            {
                Assert.Equal("Quesadillas", second.Name);
                Assert.Equal(["Seasonal"], second.StatusLabels);
            });
    }

    [Fact]
    public async Task GetPublicMenuAsync_places_special_items_first_and_marks_today()
    {
        var today = new DateOnly(2026, 5, 18);
        var repository = new FakeMenuQueryRepository
        {
            Snapshot = new PublicMenuSnapshot(
                MenuTab.Dinner,
                [CreateSection("Burgers", [MenuTab.Dinner], "Served with fries.")],
                [
                    CreateItem(
                        "Classic Hamburger",
                        "Fresh hand-pattied burger.",
                        1,
                        "Burgers",
                        [MenuTab.Dinner],
                        null,
                        null,
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 11m, 1)]),
                    CreateItem(
                        "Monday Night Burgers",
                        "Weeknight burger draw.",
                        99,
                        "Burgers",
                        [MenuTab.Dinner],
                        null,
                        null,
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 11m, 1)],
                        new MenuItemSpecialRecord(
                            Guid.NewGuid(),
                            MenuItemSpecialScheduleKind.WeeklyRecurring,
                            DayOfWeek.Monday,
                            new DateOnly(2026, 1, 1),
                            null,
                            new TimeOnly(17, 0),
                            null,
                            false,
                            "$11 basket special"))
                ],
                [CreateWindow(MenuTab.Dinner, DayOfWeek.Monday, true, new TimeOnly(17, 0), new TimeOnly(20, 0), false)]),
            TabsWithContent = [MenuTab.Dinner]
        };

        var service = new MenuQueryService(repository);

        var result = await service.GetPublicMenuAsync(MenuTab.Dinner, today);

        Assert.Equal("Specials", result.Sections[0].Name);
        var section = Assert.Single(result.Sections, section => section.Name == "Burgers");
        var firstItem = section.Items[0];
        Assert.Equal("Monday Night Burgers", firstItem.Name);
        Assert.NotNull(firstItem.Special);
        Assert.Equal("Monday", firstItem.Special!.BadgeLabel);
        Assert.True(firstItem.Special.IsToday);
        Assert.Equal("Served with fries.", section.Callout);
        Assert.Single(result.Sections[0].Items);
        Assert.Equal("Monday Night Burgers", result.Sections[0].Items[0].Name);
    }

    [Fact]
    public async Task GetPublicMenuAsync_shows_multi_section_item_only_in_sections_allowed_for_the_requested_tab()
    {
        var today = new DateOnly(2026, 5, 22);

        var breakfastRepository = new FakeMenuQueryRepository
        {
            Snapshot = new PublicMenuSnapshot(
                MenuTab.Breakfast,
                [
                    CreateSection(SectionId, "Breakfast Specials", [MenuTab.Breakfast]),
                    CreateSection(SecondarySectionId, "Soups & Salads", [MenuTab.Lunch, MenuTab.Dinner])
                ],
                [
                    CreateItem(
                        "Everything Toast",
                        "Breakfast and lunch feature.",
                        99,
                        [new MenuItemSectionAssignmentRecord(SectionId, "Breakfast Specials", 1), new MenuItemSectionAssignmentRecord(SecondarySectionId, "Soups & Salads", 8)],
                        [MenuTab.Breakfast, MenuTab.Lunch],
                        null,
                        null,
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 9m, 1)])
                ],
                [CreateWindow(MenuTab.Breakfast, DayOfWeek.Friday, true, new TimeOnly(10, 0), new TimeOnly(13, 0), false)]),
            TabsWithContent = [MenuTab.Breakfast]
        };

        var lunchRepository = new FakeMenuQueryRepository
        {
            Snapshot = new PublicMenuSnapshot(
                MenuTab.Lunch,
                [
                    CreateSection(SectionId, "Breakfast Specials", [MenuTab.Breakfast]),
                    CreateSection(SecondarySectionId, "Soups & Salads", [MenuTab.Lunch, MenuTab.Dinner])
                ],
                [
                    CreateItem(
                        "Everything Toast",
                        "Breakfast and lunch feature.",
                        99,
                        [new MenuItemSectionAssignmentRecord(SectionId, "Breakfast Specials", 1), new MenuItemSectionAssignmentRecord(SecondarySectionId, "Soups & Salads", 8)],
                        [MenuTab.Breakfast, MenuTab.Lunch],
                        null,
                        null,
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 9m, 1)])
                ],
                [CreateWindow(MenuTab.Lunch, DayOfWeek.Friday, true, new TimeOnly(11, 0), new TimeOnly(16, 0), false)]),
            TabsWithContent = [MenuTab.Lunch]
        };

        var dinnerRepository = new FakeMenuQueryRepository
        {
            Snapshot = new PublicMenuSnapshot(
                MenuTab.Dinner,
                [],
                [],
                [CreateWindow(MenuTab.Dinner, DayOfWeek.Friday, true, new TimeOnly(16, 0), new TimeOnly(22, 0), false)]),
            TabsWithContent = [MenuTab.Dinner]
        };

        var breakfastResult = await new MenuQueryService(breakfastRepository).GetPublicMenuAsync(MenuTab.Breakfast, today);
        var lunchResult = await new MenuQueryService(lunchRepository).GetPublicMenuAsync(MenuTab.Lunch, today);
        var dinnerResult = await new MenuQueryService(dinnerRepository).GetPublicMenuAsync(MenuTab.Dinner, today);

        Assert.Equal("Breakfast Specials", Assert.Single(breakfastResult.Sections).Name);
        Assert.Equal("Everything Toast", Assert.Single(Assert.Single(breakfastResult.Sections).Items).Name);

        Assert.Equal("Soups & Salads", Assert.Single(lunchResult.Sections).Name);
        Assert.Equal("Everything Toast", Assert.Single(Assert.Single(lunchResult.Sections).Items).Name);

        Assert.Empty(dinnerResult.Sections);
    }

    [Fact]
    public async Task GetPublicMenuAsync_renders_visible_child_sections_inside_their_parent()
    {
        var today = new DateOnly(2026, 5, 22);
        var parentSectionId = Guid.Parse("D5D5E4B2-7DE8-4D14-8B8E-6161F3E77372");
        var childSectionId = Guid.Parse("C3E6E48C-B9D9-4D95-AFB1-6F7D2FD79C44");

        var repository = new FakeMenuQueryRepository
        {
            Snapshot = new PublicMenuSnapshot(
                MenuTab.Breakfast,
                [
                    CreateSection(parentSectionId, "Breakfast", [MenuTab.Breakfast]),
                    CreateSection(childSectionId, "Pancakes", [MenuTab.Breakfast], parentSectionId: parentSectionId)
                ],
                [
                    CreateItem(
                        "Blueberry Pancakes",
                        "Weekend stack.",
                        3,
                        [new MenuItemSectionAssignmentRecord(childSectionId, "Pancakes", 3)],
                        [MenuTab.Breakfast],
                        null,
                        null,
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 11m, 1)])
                ],
                [CreateWindow(MenuTab.Breakfast, DayOfWeek.Friday, true, new TimeOnly(9, 0), new TimeOnly(12, 0), false)]),
            TabsWithContent = [MenuTab.Breakfast]
        };

        var result = await new MenuQueryService(repository).GetPublicMenuAsync(MenuTab.Breakfast, today);

        var rootSection = Assert.Single(result.Sections);
        Assert.Equal("Breakfast", rootSection.Name);
        var childEntry = Assert.Single(rootSection.Entries, entry => entry.ChildSection is not null);
        Assert.Equal("Pancakes", childEntry.ChildSection!.Name);
        Assert.Equal("Blueberry Pancakes", Assert.Single(childEntry.ChildSection.Items).Name);
    }

    [Fact]
    public async Task GetPublicMenuAsync_preserves_mixed_parent_section_order_for_child_sections_and_direct_items()
    {
        var today = new DateOnly(2026, 5, 22);
        var parentSectionId = Guid.Parse("B1A8C19F-C0D5-41A5-82D7-EE84B0C5B289");
        var childSectionId = Guid.Parse("3AFC6D55-6AE6-423C-AE2A-B1F70A7E39F5");

        var repository = new FakeMenuQueryRepository
        {
            Snapshot = new PublicMenuSnapshot(
                MenuTab.Breakfast,
                [
                    CreateSection(parentSectionId, "Breakfast Specials", [MenuTab.Breakfast]),
                    CreateSection(childSectionId, "Omelets", [MenuTab.Breakfast], "Choice of breakfast potato or hashbrowns.", parentSectionId, 11)
                ],
                [
                    CreateItem(
                        "Everything Toast",
                        "Breakfast favorite.",
                        1,
                        [new MenuItemSectionAssignmentRecord(parentSectionId, "Breakfast Specials", 1)],
                        [MenuTab.Breakfast],
                        null,
                        null,
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 13m, 1)]),
                    CreateItem(
                        "Red Eye",
                        "Breakfast plate.",
                        2,
                        [new MenuItemSectionAssignmentRecord(parentSectionId, "Breakfast Specials", 2)],
                        [MenuTab.Breakfast],
                        null,
                        null,
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 12m, 1)]),
                    CreateItem(
                        "Breakfast Skillet",
                        "Hearty skillet.",
                        3,
                        [new MenuItemSectionAssignmentRecord(parentSectionId, "Breakfast Specials", 3)],
                        [MenuTab.Breakfast],
                        null,
                        null,
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 14m, 1)]),
                    CreateItem(
                        "Ham & Cheese",
                        "Classic omelet.",
                        11,
                        [new MenuItemSectionAssignmentRecord(childSectionId, "Omelets", 1)],
                        [MenuTab.Breakfast],
                        null,
                        null,
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 13m, 1)])
                ],
                [CreateWindow(MenuTab.Breakfast, DayOfWeek.Friday, true, new TimeOnly(9, 0), new TimeOnly(12, 0), false)]),
            TabsWithContent = [MenuTab.Breakfast]
        };

        var result = await new MenuQueryService(repository).GetPublicMenuAsync(MenuTab.Breakfast, today);

        var rootSection = Assert.Single(result.Sections);
        Assert.Equal(
            ["Everything Toast", "Red Eye", "Breakfast Skillet", "Omelets"],
            rootSection.Entries.Select(entry => entry.Item?.Name ?? entry.ChildSection!.Name).ToArray());
        Assert.Equal("Ham & Cheese", Assert.Single(rootSection.Entries.Last().ChildSection!.Items).Name);
    }

    [Fact]
    public async Task GetPublicMenuAsync_promotes_visible_child_section_when_parent_is_not_visible_for_that_tab()
    {
        var today = new DateOnly(2026, 5, 22);
        var parentSectionId = Guid.Parse("1A035410-65C9-4D46-9B8A-5CF1D8547CB2");
        var childSectionId = Guid.Parse("A17D846F-1BA3-4F53-9B09-1EDC2C57F912");

        var repository = new FakeMenuQueryRepository
        {
            Snapshot = new PublicMenuSnapshot(
                MenuTab.Lunch,
                [
                    CreateSection(parentSectionId, "Breakfast", [MenuTab.Breakfast]),
                    CreateSection(childSectionId, "Breakfast Specials", [MenuTab.Lunch], parentSectionId: parentSectionId)
                ],
                [
                    CreateItem(
                        "Everything Toast",
                        "Cross-service favorite.",
                        1,
                        [new MenuItemSectionAssignmentRecord(childSectionId, "Breakfast Specials", 1)],
                        [MenuTab.Lunch],
                        null,
                        null,
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 9m, 1)])
                ],
                [CreateWindow(MenuTab.Lunch, DayOfWeek.Friday, true, new TimeOnly(11, 0), new TimeOnly(16, 0), false)]),
            TabsWithContent = [MenuTab.Lunch]
        };

        var result = await new MenuQueryService(repository).GetPublicMenuAsync(MenuTab.Lunch, today);

        var rootSection = Assert.Single(result.Sections);
        Assert.Equal("Breakfast Specials", rootSection.Name);
        Assert.Equal("Everything Toast", Assert.Single(rootSection.Items).Name);
    }

    [Fact]
    public async Task GetPublicMenuAsync_flattens_deeper_legacy_descendant_items_under_the_first_visible_child_section()
    {
        var today = new DateOnly(2026, 5, 22);
        var rootSectionId = Guid.Parse("EBE22348-7EBA-44D9-A1BC-94817FB3CBF0");
        var childSectionId = Guid.Parse("AEBE9CB6-E0D1-437D-8E01-D14E6739FC49");
        var grandchildSectionId = Guid.Parse("D5791528-195D-4B06-B84D-C3B4AA2E2C70");

        var repository = new FakeMenuQueryRepository
        {
            Snapshot = new PublicMenuSnapshot(
                MenuTab.Breakfast,
                [
                    CreateSection(rootSectionId, "Breakfast Specials", [MenuTab.Breakfast]),
                    CreateSection(childSectionId, "Omelets", [MenuTab.Breakfast], parentSectionId: rootSectionId),
                    CreateSection(grandchildSectionId, "Chef Features", [MenuTab.Breakfast], parentSectionId: childSectionId)
                ],
                [
                    CreateItem(
                        "Ham & Cheese",
                        "Classic omelet.",
                        1,
                        [new MenuItemSectionAssignmentRecord(grandchildSectionId, "Chef Features", 1)],
                        [MenuTab.Breakfast],
                        null,
                        null,
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 13m, 1)])
                ],
                [CreateWindow(MenuTab.Breakfast, DayOfWeek.Friday, true, new TimeOnly(9, 0), new TimeOnly(12, 0), false)]),
            TabsWithContent = [MenuTab.Breakfast]
        };

        var result = await new MenuQueryService(repository).GetPublicMenuAsync(MenuTab.Breakfast, today);

        var rootSection = Assert.Single(result.Sections);
        Assert.Equal("Breakfast Specials", rootSection.Name);

        var childEntry = Assert.Single(rootSection.Entries, entry => entry.ChildSection is not null);
        Assert.Equal("Omelets", childEntry.ChildSection!.Name);
        Assert.Equal("Ham & Cheese", Assert.Single(childEntry.ChildSection.Items).Name);
    }

    [Fact]
    public async Task GetHomeSpecialsAsync_projects_special_items_for_homepage()
    {
        var today = new DateOnly(2026, 5, 18);
        var repository = new FakeMenuQueryRepository
        {
            HomeSpecialItems =
            [
                CreateItem(
                    "Monday Night Burgers",
                    "Weeknight burger draw.",
                    1,
                    "Burgers",
                    [MenuTab.Dinner],
                    null,
                    null,
                    false,
                    [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 11m, 1)],
                    new MenuItemSpecialRecord(
                        Guid.NewGuid(),
                        MenuItemSpecialScheduleKind.WeeklyRecurring,
                        DayOfWeek.Monday,
                        new DateOnly(2026, 1, 1),
                        null,
                        new TimeOnly(17, 0),
                        null,
                        false,
                        "$11 basket special"))
            ]
        };

        var service = new MenuQueryService(repository);

        var result = await service.GetHomeSpecialsAsync(today);

        var special = Assert.Single(result);
        Assert.Equal("Monday Night Burgers", special.Title);
        Assert.Equal("$11 basket special", special.Callout);
        Assert.True(special.IsToday);
    }

    [Fact]
    public async Task GetHomeSpecialsAsync_orders_weekly_specials_by_their_next_occurrence_in_the_preview_window()
    {
        var today = new DateOnly(2026, 5, 22);
        var repository = new FakeMenuQueryRepository
        {
            HomeSpecialItems =
            [
                CreateItem(
                    "Monday Night Burgers",
                    "Weeknight burger draw.",
                    1,
                    "Burgers",
                    [MenuTab.Dinner],
                    null,
                    null,
                    false,
                    [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 11m, 1)],
                    new MenuItemSpecialRecord(
                        Guid.NewGuid(),
                        MenuItemSpecialScheduleKind.WeeklyRecurring,
                        DayOfWeek.Monday,
                        new DateOnly(2026, 1, 1),
                        null,
                        new TimeOnly(17, 0),
                        null,
                        false,
                        "$11 basket special")),
                CreateItem(
                    "Sunday Pork Chop Dinner",
                    "End-of-week dinner tradition.",
                    2,
                    "Dinner Specials",
                    [MenuTab.Dinner],
                    null,
                    null,
                    false,
                    [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 17m, 1)],
                    new MenuItemSpecialRecord(
                        Guid.NewGuid(),
                        MenuItemSpecialScheduleKind.WeeklyRecurring,
                        DayOfWeek.Sunday,
                        new DateOnly(2026, 1, 1),
                        null,
                        new TimeOnly(15, 0),
                        null,
                        false,
                        "$17 dinner plate"))
            ]
        };

        var result = await new MenuQueryService(repository).GetHomeSpecialsAsync(today);

        Assert.Equal(
            ["Sunday Pork Chop Dinner", "Monday Night Burgers"],
            result.Select(item => item.Title).ToArray());
    }

    [Fact]
    public async Task GetSuggestedPublicTabAsync_prefers_the_active_food_service_over_drinks()
    {
        var repository = new FakeMenuQueryRepository
        {
            ServiceWindows =
            [
                CreateWindow(MenuTab.Lunch, DayOfWeek.Wednesday, true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
                CreateWindow(MenuTab.Drinks, DayOfWeek.Wednesday, true, new TimeOnly(11, 0), new TimeOnly(22, 0), false)
            ]
        };

        var service = new MenuQueryService(repository);

        var result = await service.GetSuggestedPublicTabAsync(new DateOnly(2026, 5, 20), new TimeOnly(12, 30));

        Assert.Equal(MenuTab.Lunch, result);
    }

    [Fact]
    public async Task GetSuggestedPublicTabAsync_returns_next_opening_when_no_service_is_active()
    {
        var repository = new FakeMenuQueryRepository
        {
            ServiceWindows =
            [
                CreateWindow(MenuTab.Dinner, DayOfWeek.Monday, true, new TimeOnly(17, 0), new TimeOnly(20, 0), false),
                CreateWindow(MenuTab.Drinks, DayOfWeek.Monday, false, null, null, false),
                CreateWindow(MenuTab.Breakfast, DayOfWeek.Tuesday, true, new TimeOnly(10, 0), new TimeOnly(13, 0), false)
            ]
        };

        var service = new MenuQueryService(repository);

        var result = await service.GetSuggestedPublicTabAsync(new DateOnly(2026, 5, 18), new TimeOnly(9, 0));

        Assert.Equal(MenuTab.Dinner, result);
    }

    [Fact]
    public async Task GetSuggestedPublicTabAsync_treats_overnight_windows_as_active_after_midnight()
    {
        var repository = new FakeMenuQueryRepository
        {
            ServiceWindows =
            [
                CreateWindow(MenuTab.Drinks, DayOfWeek.Friday, true, new TimeOnly(11, 0), new TimeOnly(2, 0), true),
                CreateWindow(MenuTab.Dinner, DayOfWeek.Saturday, true, new TimeOnly(16, 0), new TimeOnly(22, 0), false)
            ]
        };

        var service = new MenuQueryService(repository);

        var result = await service.GetSuggestedPublicTabAsync(new DateOnly(2026, 5, 23), new TimeOnly(1, 0));

        Assert.Equal(MenuTab.Drinks, result);
    }

    private static MenuServiceWindowRecord CreateWindow(MenuTab tab, DayOfWeek day, bool isAvailable, TimeOnly? opensAt, TimeOnly? closesAt, bool closesNextDay) =>
        new(tab, day, isAvailable, opensAt, closesAt, closesNextDay);

    private static MenuSectionRecord CreateSection(
        Guid sectionId,
        string name,
        IReadOnlyList<MenuTab> menuTabs,
        string? callout = null,
        Guid? parentSectionId = null,
        int sortOrder = 1) =>
        new(
            sectionId,
            name,
            callout,
            MenuFamily.Food,
            parentSectionId,
            menuTabs,
            sortOrder,
            true,
            false);

    private static MenuSectionRecord CreateSection(string name, IReadOnlyList<MenuTab> menuTabs, string? callout = null) =>
        CreateSection(SectionId, name, menuTabs, callout);

    private static MenuItemRecord CreateItem(
        string name,
        string description,
        int sortOrder,
        string sectionName,
        IReadOnlyList<MenuTab> menuTabs,
        DateOnly? offerStartsOn,
        DateOnly? offerEndsOn,
        bool isSeasonal,
        IReadOnlyList<MenuItemPriceVariantRecord> priceVariants,
        MenuItemSpecialRecord? special = null) =>
        CreateItem(
            name,
            description,
            sortOrder,
            [new MenuItemSectionAssignmentRecord(SectionId, sectionName, sortOrder)],
            menuTabs,
            offerStartsOn,
            offerEndsOn,
            isSeasonal,
            priceVariants,
            special);

    private static MenuItemRecord CreateItem(
        string name,
        string description,
        int sortOrder,
        IReadOnlyList<MenuItemSectionAssignmentRecord> assignments,
        IReadOnlyList<MenuTab> menuTabs,
        DateOnly? offerStartsOn,
        DateOnly? offerEndsOn,
        bool isSeasonal,
        IReadOnlyList<MenuItemPriceVariantRecord> priceVariants,
        MenuItemSpecialRecord? special = null) =>
        new(
            Guid.NewGuid(),
            MenuFamily.Food,
            name,
            description,
            null,
            sortOrder,
            true,
            false,
            offerStartsOn,
            offerEndsOn,
            isSeasonal,
            priceVariants,
            assignments,
            false,
            menuTabs,
            special);

    private sealed class FakeMenuQueryRepository : IMenuQueryRepository
    {
        public PublicMenuSnapshot Snapshot { get; set; } = new(MenuTab.Lunch, [], [], []);

        public IReadOnlyCollection<MenuTab> TabsWithContent { get; set; } = [];

        public IReadOnlyList<MenuItemRecord> HomeSpecialItems { get; set; } = [];

        public IReadOnlyList<MenuServiceWindowRecord> ServiceWindows { get; set; } = [];

        public Task<IReadOnlyList<MenuServiceWindowRecord>> GetPublicServiceWindowsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ServiceWindows);

        public Task<PublicMenuSnapshot> GetPublicMenuSnapshotAsync(MenuTab tab, DateOnly today, DateOnly comingSoonCutoff, CancellationToken cancellationToken = default) =>
            Task.FromResult(Snapshot);

        public Task<IReadOnlyList<MenuItemRecord>> GetHomeSpecialItemsAsync(DateOnly today, DateOnly comingSoonCutoff, CancellationToken cancellationToken = default) =>
            Task.FromResult(HomeSpecialItems);

        public Task<IReadOnlyCollection<MenuTab>> GetTabsWithVisibleContentAsync(DateOnly today, DateOnly comingSoonCutoff, CancellationToken cancellationToken = default) =>
            Task.FromResult(TabsWithContent);

        public Task<MenuManagementSnapshot> GetMenuManagementSnapshotAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new MenuManagementSnapshot([], [], []));
    }
}
