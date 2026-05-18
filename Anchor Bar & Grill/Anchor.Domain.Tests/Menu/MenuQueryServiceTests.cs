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
                        [MenuTab.Lunch])
                ],
                [],
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
                        [MenuTab.Lunch]),
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
                        [MenuTab.Lunch])
                ],
                [],
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
    public async Task GetPublicMenuAsync_groups_specials_into_their_section_and_marks_today()
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
                        [MenuTab.Dinner])
                ],
                [
                    new MenuRecurringSpecialRecord(
                        Guid.NewGuid(),
                        MenuTab.Dinner,
                        SectionId,
                        "Burgers",
                        DayOfWeek.Monday,
                        "Monday Night Burgers",
                        "Weeknight burger draw.",
                        "After 5:00 PM",
                        "$11 basket special",
                        null,
                        null,
                        1,
                        true,
                        false)
                ],
                [CreateWindow(MenuTab.Dinner, DayOfWeek.Monday, true, new TimeOnly(17, 0), new TimeOnly(20, 0), false)]),
            TabsWithContent = [MenuTab.Dinner]
        };

        var service = new MenuQueryService(repository);

        var result = await service.GetPublicMenuAsync(MenuTab.Dinner, today);

        var section = Assert.Single(result.Sections);
        var special = Assert.Single(section.Specials);
        Assert.Equal("Monday", special.DayLabel);
        Assert.True(special.IsToday);
        Assert.Equal("Burgers", section.Name);
    }

    private static MenuServiceWindowRecord CreateWindow(MenuTab tab, DayOfWeek day, bool isAvailable, TimeOnly? opensAt, TimeOnly? closesAt, bool closesNextDay) =>
        new(tab, day, isAvailable, opensAt, closesAt, closesNextDay);

    private sealed class FakeMenuQueryRepository : IMenuQueryRepository
    {
        public PublicMenuSnapshot Snapshot { get; set; } = new(MenuTab.Lunch, [], [], [], []);

        public IReadOnlyCollection<MenuTab> TabsWithContent { get; set; } = [];

        public IReadOnlyList<MenuRecurringSpecialRecord> HomeSpecials { get; set; } = [];

        public Task<PublicMenuSnapshot> GetPublicMenuSnapshotAsync(MenuTab tab, DateOnly today, DateOnly comingSoonCutoff, CancellationToken cancellationToken = default) =>
            Task.FromResult(Snapshot);

        public Task<IReadOnlyList<MenuRecurringSpecialRecord>> GetHomeRecurringSpecialsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(HomeSpecials);

        public Task<IReadOnlyCollection<MenuTab>> GetTabsWithVisibleContentAsync(DateOnly today, DateOnly comingSoonCutoff, CancellationToken cancellationToken = default) =>
            Task.FromResult(TabsWithContent);

        public Task<MenuManagementSnapshot> GetMenuManagementSnapshotAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new MenuManagementSnapshot([], [], [], []));
    }
}
