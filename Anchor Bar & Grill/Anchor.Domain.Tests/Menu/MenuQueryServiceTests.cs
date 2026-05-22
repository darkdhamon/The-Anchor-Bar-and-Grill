using Anchor.Domain.Menu;

namespace Anchor.Domain.Tests.Menu;

public sealed class MenuQueryServiceTests
{
    private static readonly Guid SectionId = Guid.Parse("F11A7F7A-CBF9-4F3B-924E-9C2EDC7969E4");

    [Fact]
    public async Task GetPublicMenuAsync_marks_future_items_as_coming_soon_without_limited_time_label()
    {
        var today = new DateOnly(2026, 5, 16);
        var repository = new FakeMenuQueryRepository
        {
            Snapshot = new PublicMenuSnapshot(
                MenuTab.Lunch,
                [new MenuSectionRecord(SectionId, "Appetizers", MenuFamily.Food, 1, true, false)],
                [
                    new MenuItemRecord(
                        Guid.NewGuid(),
                        SectionId,
                        "Appetizers",
                        MenuFamily.Food,
                        "Mini Tacos",
                        "Served with salsa and sour cream.",
                        null,
                        1,
                        true,
                        false,
                        today.AddDays(5),
                        today.AddDays(25),
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 9m, 1)],
                        [MenuTab.Lunch],
                        null)
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
                [new MenuSectionRecord(SectionId, "Appetizers", MenuFamily.Food, 1, true, false)],
                [
                    new MenuItemRecord(
                        Guid.NewGuid(),
                        SectionId,
                        "Appetizers",
                        MenuFamily.Food,
                        "Quesadillas",
                        "Loaded with cheese.",
                        null,
                        1,
                        true,
                        false,
                        today.AddDays(-2),
                        today.AddDays(30),
                        true,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 11m, 1)],
                        [MenuTab.Lunch],
                        null),
                    new MenuItemRecord(
                        Guid.NewGuid(),
                        SectionId,
                        "Appetizers",
                        MenuFamily.Food,
                        "Fish Tacos",
                        "Boom Boom sauce.",
                        null,
                        2,
                        true,
                        false,
                        today.AddDays(-4),
                        today.AddDays(18),
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 10m, 1)],
                        [MenuTab.Lunch],
                        null)
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
                [new MenuSectionRecord(SectionId, "Burgers", MenuFamily.Food, 1, true, false)],
                [
                    new MenuItemRecord(
                        Guid.NewGuid(),
                        SectionId,
                        "Burgers",
                        MenuFamily.Food,
                        "Classic Hamburger",
                        "Fresh hand-pattied burger.",
                        null,
                        1,
                        true,
                        false,
                        null,
                        null,
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 11m, 1)],
                        [MenuTab.Dinner],
                        null),
                    new MenuItemRecord(
                        Guid.NewGuid(),
                        SectionId,
                        "Burgers",
                        MenuFamily.Food,
                        "Monday Night Burgers",
                        "Weeknight burger draw.",
                        null,
                        99,
                        true,
                        false,
                        null,
                        null,
                        false,
                        [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 11m, 1)],
                        [MenuTab.Dinner],
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

        var section = Assert.Single(result.Sections);
        var firstItem = section.Items[0];
        Assert.Equal("Monday Night Burgers", firstItem.Name);
        Assert.NotNull(firstItem.Special);
        Assert.Equal("Monday", firstItem.Special!.BadgeLabel);
        Assert.True(firstItem.Special.IsToday);
    }

    [Fact]
    public async Task GetHomeSpecialsAsync_projects_special_items_for_homepage()
    {
        var today = new DateOnly(2026, 5, 18);
        var repository = new FakeMenuQueryRepository
        {
            HomeSpecialItems =
            [
                new MenuItemRecord(
                    Guid.NewGuid(),
                    SectionId,
                    "Burgers",
                    MenuFamily.Food,
                    "Monday Night Burgers",
                    "Weeknight burger draw.",
                    null,
                    1,
                    true,
                    false,
                    null,
                    null,
                    false,
                    [new MenuItemPriceVariantRecord(Guid.NewGuid(), "Regular", 11m, 1)],
                    [MenuTab.Dinner],
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
