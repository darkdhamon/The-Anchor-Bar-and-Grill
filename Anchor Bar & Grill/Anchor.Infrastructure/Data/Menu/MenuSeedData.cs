using Anchor.Domain.Menu;

namespace Anchor.Infrastructure.Data.Menu;

internal static class MenuSeedData
{
    private static readonly Guid DinnerSpecialsSectionId = Guid.Parse("0E4DE526-5921-4C3B-8985-D83344642A41");
    private static readonly Guid AppetizersSectionId = Guid.Parse("D67BD219-6D64-4A08-8CE4-D036A0C7B16D");
    private static readonly Guid WingsSectionId = Guid.Parse("4A3A2D15-2AF0-44A7-84C8-67B603A3DDB4");
    private static readonly Guid SoupsSectionId = Guid.Parse("31E9CB99-5FCA-4A4A-A04B-89B97C926A52");
    private static readonly Guid SandwichesSectionId = Guid.Parse("7F644C28-9275-4DF8-8E4B-482F47568CFB");
    private static readonly Guid BurgersSectionId = Guid.Parse("198CCF8A-72FD-4278-A360-F36D5871E58B");
    private static readonly Guid WrapsSectionId = Guid.Parse("FA5DA0F9-7E81-4B9D-9E11-FA5B1F828C72");
    private static readonly Guid KidsSectionId = Guid.Parse("2EA5E671-E8AC-4C8A-B3D9-4C136A32A71B");
    private static readonly Guid DessertsSectionId = Guid.Parse("A8F0B603-E02D-49F5-873D-1BB6BFC16C0F");
    private static readonly Guid SundayPorkChopItemId = Guid.Parse("9E7F7A6B-C8DB-4E8D-B2EF-A60A40E91F70");

    private static readonly DateOnly OfferReferenceDate = new(2026, 5, 17);

    private static MenuItemEntity CreateItem(Guid itemId, Guid sectionId, string name, string description, string? imagePath, int sortOrder, DateOnly? startsOn, DateOnly? endsOn, bool isSeasonal) =>
        new()
        {
            MenuItemId = itemId,
            MenuSectionId = sectionId,
            Name = name,
            Description = description,
            ImagePath = imagePath,
            SortOrder = sortOrder,
            IsVisibleToGuests = true,
            IsArchived = false,
            OfferStartsOn = startsOn,
            OfferEndsOn = endsOn,
            IsSeasonal = isSeasonal
        };

    private static MenuItemEntity CreateHiddenItem(Guid itemId, Guid sectionId, string name, string description, string? imagePath, int sortOrder, DateOnly? startsOn, DateOnly? endsOn, bool isSeasonal)
    {
        var item = CreateItem(itemId, sectionId, name, description, imagePath, sortOrder, startsOn, endsOn, isSeasonal);
        item.IsVisibleToGuests = false;
        return item;
    }

    private static MenuItemPriceVariantEntity Price(Guid variantId, Guid itemId, string label, decimal amount, int sortOrder) =>
        new()
        {
            MenuItemPriceVariantId = variantId,
            MenuItemId = itemId,
            Label = label,
            Amount = amount,
            SortOrder = sortOrder
        };

    private static MenuItemTabEntity Tab(Guid itemId, MenuTab tab) => new() { MenuItemId = itemId, Tab = tab };

    private static DateOnly OfferStartingIn(int offsetDays) => OfferReferenceDate.AddDays(offsetDays);

    private static DateOnly OfferEndingIn(int startOffsetDays, int durationDays) => OfferStartingIn(startOffsetDays).AddDays(durationDays);

    public static IReadOnlyList<MenuSectionEntity> Sections { get; } =
    [
        new() { MenuSectionId = DinnerSpecialsSectionId, Name = "Dinner Specials", Family = MenuFamily.Food, SortOrder = 0, IsVisibleToGuests = true, IsArchived = false },
        new() { MenuSectionId = AppetizersSectionId, Name = "Appetizers", Family = MenuFamily.Food, SortOrder = 1, IsVisibleToGuests = true, IsArchived = false },
        new() { MenuSectionId = WingsSectionId, Name = "Wings", Family = MenuFamily.Food, SortOrder = 2, IsVisibleToGuests = true, IsArchived = false },
        new() { MenuSectionId = SoupsSectionId, Name = "Soups & Salads", Family = MenuFamily.Food, SortOrder = 3, IsVisibleToGuests = true, IsArchived = false },
        new() { MenuSectionId = SandwichesSectionId, Name = "Sandwiches", Family = MenuFamily.Food, SortOrder = 4, IsVisibleToGuests = true, IsArchived = false },
        new() { MenuSectionId = BurgersSectionId, Name = "Burgers", Family = MenuFamily.Food, SortOrder = 5, IsVisibleToGuests = true, IsArchived = false },
        new() { MenuSectionId = WrapsSectionId, Name = "Wraps", Family = MenuFamily.Food, SortOrder = 6, IsVisibleToGuests = true, IsArchived = false },
        new() { MenuSectionId = KidsSectionId, Name = "Kids Menu", Family = MenuFamily.Food, SortOrder = 7, IsVisibleToGuests = true, IsArchived = false },
        new() { MenuSectionId = DessertsSectionId, Name = "Desserts", Family = MenuFamily.Food, SortOrder = 8, IsVisibleToGuests = true, IsArchived = false }
    ];

    public static IReadOnlyList<MenuItemEntity> Items { get; } =
    [
        CreateItem(Guid.Parse("C88652A0-C9F2-4A7D-B4AC-8DDBFC9FF4E5"), AppetizersSectionId, "Cheese Curds", "Crisp white cheddar curds with your choice of dipping sauce.", "images/menu/appetizers.svg", 1, null, null, false),
        CreateItem(Guid.Parse("5C3A9530-0F24-4D62-883B-F01B0A4286C2"), AppetizersSectionId, "Mini Tacos", "Served with salsa and sour cream.", null, 2, OfferStartingIn(14), null, false),
        CreateItem(Guid.Parse("FF4EE65C-89E7-49F7-9023-8579CCB8307B"), AppetizersSectionId, "Quesadillas", "Loaded with cheese and served with salsa and sour cream.", null, 3, OfferStartingIn(-8), OfferEndingIn(-8, 62), true),
        CreateItem(Guid.Parse("E75D8A92-F1D2-4D58-9CD0-9B7E80CE9D80"), AppetizersSectionId, "Fish Tacos", "Finished with Boom Boom sauce for a bold bar-food favorite.", "images/menu/appetizers.svg", 4, OfferStartingIn(-5), OfferEndingIn(-5, 22), false),
        CreateItem(Guid.Parse("79663EF8-29FF-4D24-8B1C-CFA8DAD8BA72"), WingsSectionId, "Traditional or Boneless (6)", "Choice of one sauce.", "images/menu/wings.svg", 1, null, null, false),
        CreateItem(Guid.Parse("1C4D4F34-5260-4F7D-ABCB-1C6875B7EBF8"), WingsSectionId, "Traditional or Boneless (12)", "Choice of two sauces.", "images/menu/wings.svg", 2, null, null, false),
        CreateItem(Guid.Parse("CA5CD1B7-8C73-4B21-B3E4-8E98FEA44EE9"), WingsSectionId, "Add Fries", "Upgrade any wing order with a side of fries.", null, 3, null, null, false),
        CreateItem(Guid.Parse("73EA7283-893F-4D14-8081-39F63BD54D13"), SoupsSectionId, "The Anchor Salad", "Crisp greens, tomatoes, peppers, shaved red onions, and your choice of dressing.", "images/menu/salads.svg", 1, null, null, false),
        CreateItem(Guid.Parse("8C5BDE4D-3FB2-4A02-8AB5-40D3E0B49387"), SoupsSectionId, "Smoked Salmon Salad", "Finished with crumbled smoked salmon and poppyseed dressing.", "images/menu/salads.svg", 2, OfferStartingIn(9), OfferEndingIn(9, 45), true),
        CreateItem(Guid.Parse("E9D5A6C9-9A4C-4E98-8C72-2AE28BFCBA97"), SoupsSectionId, "BLT Salad", "Bacon, tomatoes, greens, and your choice of dressing.", null, 3, null, null, false),
        CreateItem(Guid.Parse("DB2A7B2F-D9E9-4433-80A3-BAEB5E5B5728"), SoupsSectionId, "Seasonal Soup", "Cup or bowl, updated as the kitchen rotates specials.", null, 4, null, null, false),
        CreateItem(Guid.Parse("590CC0E4-8BE8-48E8-97B8-908EA7A1FC9A"), SandwichesSectionId, "Grilled Chicken Sandwich", "Lettuce, tomato, and mayo on a toasted bun.", "images/menu/sandwiches.svg", 1, null, null, false),
        CreateItem(Guid.Parse("3B7745B6-66D4-4DB7-8EE3-B018834F58F7"), SandwichesSectionId, "Steak Sandwich", "Grilled sirloin, smoked gouda, peppers, and onions.", "images/menu/sandwiches.svg", 2, null, null, false),
        CreateItem(Guid.Parse("1AF4708E-E741-4621-95E3-6C8F24AF2BE6"), SandwichesSectionId, "Ranch Melt", "Swiss cheese, grilled ham, smoked bacon, and classic ranch.", null, 3, null, null, false),
        CreateItem(Guid.Parse("E1FD2B7F-D7E0-47CC-9E3E-4BC3A30AA4B8"), SandwichesSectionId, "Walleye Sandwich", "Breaded walleye on a toasted bun.", null, 4, null, null, false),
        CreateItem(Guid.Parse("7626D0DF-9F8A-4FE8-9062-3596165E148A"), BurgersSectionId, "Classic Hamburger", "Fresh hand-pattied burger; add cheese if desired.", "images/menu/burgers.svg", 1, null, null, false),
        CreateItem(Guid.Parse("ECFC8BFA-6C51-4607-B7FF-FE9F59DB8FBC"), BurgersSectionId, "Bacon Cheeseburger", "A familiar favorite with bacon and melty cheese.", "images/menu/burgers.svg", 2, null, null, false),
        CreateItem(Guid.Parse("6E97A8EE-16B1-4FEB-B6E0-2AB4E56658A0"), BurgersSectionId, "Western Burger", "Bacon, BBQ sauce, and a crisp onion ring.", null, 3, null, null, false),
        CreateItem(Guid.Parse("90DCE7E3-9CC6-4732-B7D2-F4D43056FBB8"), BurgersSectionId, "Sunrise Burger", "Bacon, American cheese, and egg.", null, 4, null, null, false),
        CreateItem(Guid.Parse("0D440A2B-06A3-47F9-B129-1544F2F391A8"), WrapsSectionId, "Chicken Wrap", "Grilled or crispy chicken, tomatoes, onions, lettuce, and dressing.", "images/menu/wraps.svg", 1, null, null, false),
        CreateItem(Guid.Parse("95F39C20-E1BA-4FD2-992D-8D9E19600D64"), WrapsSectionId, "Steak Wrap", "Steak, peppers, onions, cheese, lettuce, and dressing.", "images/menu/wraps.svg", 2, null, null, false),
        CreateItem(Guid.Parse("6F2A75A4-C1E2-458F-BDE4-D825F987CC3D"), WrapsSectionId, "Buffalo Chicken Wrap", "Crispy chicken tossed in buffalo with ranch-style cooling balance.", null, 3, null, null, false),
        CreateItem(Guid.Parse("B7AB3351-1B6B-45D0-B7B4-9782D79CFC65"), KidsSectionId, "Mac & Cheese", "Served with one side and a kid drink option.", null, 1, null, null, false),
        CreateItem(Guid.Parse("5B1C6127-F7F0-497A-88B9-537E9110176F"), KidsSectionId, "Mini Corn Dogs", "The classic choice for a quick family meal.", "images/menu/kids.svg", 2, null, null, false),
        CreateItem(Guid.Parse("06F858A2-F226-4B2F-A912-A6330BBF4EC1"), KidsSectionId, "Chicken Strips", "Served with sauce.", null, 3, null, null, false),
        CreateItem(Guid.Parse("8FCAA555-D618-4AD8-AE73-ABF51854A329"), DessertsSectionId, "Chocolate Lava Cake", "Served warm with ice cream.", "images/menu/desserts.svg", 1, OfferStartingIn(-2), OfferEndingIn(-2, 18), false),
        CreateItem(Guid.Parse("44472C07-5F31-482A-8506-8A3C11CF1F26"), DessertsSectionId, "Mini Donuts", "Fair-style donuts for a casual sweet finish.", "images/menu/desserts.svg", 2, null, null, false),
        CreateHiddenItem(SundayPorkChopItemId, DinnerSpecialsSectionId, "Sunday Pork Chop Dinner", "A hearty end-of-week dinner special that should read as a repeatable tradition.", null, 1, null, null, false)
    ];

    public static IReadOnlyList<MenuItemPriceVariantEntity> PriceVariants { get; } =
    [
        Price(Guid.Parse("916E3D70-2CB3-4273-A283-3241FB7FA0D0"), Guid.Parse("C88652A0-C9F2-4A7D-B4AC-8DDBFC9FF4E5"), "Regular", 9m, 1),
        Price(Guid.Parse("2D12EAF9-EBCA-4EA9-9ECC-663FF531D9ED"), Guid.Parse("5C3A9530-0F24-4D62-883B-F01B0A4286C2"), "Regular", 9m, 1),
        Price(Guid.Parse("65E43E63-0A48-4B0C-B3B1-3956101A4F56"), Guid.Parse("FF4EE65C-89E7-49F7-9023-8579CCB8307B"), "Regular", 11m, 1),
        Price(Guid.Parse("90CCF394-072E-47F2-80B5-85A00E58B6D5"), Guid.Parse("E75D8A92-F1D2-4D58-9CD0-9B7E80CE9D80"), "Regular", 10m, 1),
        Price(Guid.Parse("5FA1BBA0-2AFA-45EF-BE7B-6CC2C1354B9B"), Guid.Parse("79663EF8-29FF-4D24-8B1C-CFA8DAD8BA72"), "Regular", 9m, 1),
        Price(Guid.Parse("305BE09A-E819-48DE-B6BB-7771AABFD65A"), Guid.Parse("1C4D4F34-5260-4F7D-ABCB-1C6875B7EBF8"), "Regular", 16m, 1),
        Price(Guid.Parse("8E59CF80-D0FC-43A2-9442-9262F77D9B8A"), Guid.Parse("CA5CD1B7-8C73-4B21-B3E4-8E98FEA44EE9"), "Regular", 3m, 1),
        Price(Guid.Parse("388C7811-3724-4937-86F7-4E5DE1836535"), Guid.Parse("73EA7283-893F-4D14-8081-39F63BD54D13"), "Regular", 10m, 1),
        Price(Guid.Parse("D37F81B0-FB88-4DAF-9FB8-DBD62AFA8392"), Guid.Parse("8C5BDE4D-3FB2-4A02-8AB5-40D3E0B49387"), "Regular", 13m, 1),
        Price(Guid.Parse("957A3058-AD38-4DA8-AE9A-D2670E408831"), Guid.Parse("E9D5A6C9-9A4C-4E98-8C72-2AE28BFCBA97"), "Regular", 12m, 1),
        Price(Guid.Parse("4DB158E0-EE22-4B83-846A-F62446FD7FE5"), Guid.Parse("DB2A7B2F-D9E9-4433-80A3-BAEB5E5B5728"), "Cup", 4m, 1),
        Price(Guid.Parse("7B70FEB6-0DBA-4AA0-8B6E-1A791B196D31"), Guid.Parse("DB2A7B2F-D9E9-4433-80A3-BAEB5E5B5728"), "Bowl", 6m, 2),
        Price(Guid.Parse("3F6A665B-3334-4315-B208-57CF9CC4B234"), Guid.Parse("590CC0E4-8BE8-48E8-97B8-908EA7A1FC9A"), "Regular", 13m, 1),
        Price(Guid.Parse("D8E8C71B-0234-4742-811A-71AD0E65A094"), Guid.Parse("3B7745B6-66D4-4DB7-8EE3-B018834F58F7"), "Regular", 14m, 1),
        Price(Guid.Parse("7A5B50D6-A069-41D1-AF29-4D308A93E9DA"), Guid.Parse("1AF4708E-E741-4621-95E3-6C8F24AF2BE6"), "Regular", 13m, 1),
        Price(Guid.Parse("D1D39B7A-30A0-4A9A-B9B0-C8DD1C8C4C74"), Guid.Parse("E1FD2B7F-D7E0-47CC-9E3E-4BC3A30AA4B8"), "Regular", 14m, 1),
        Price(Guid.Parse("ABAA4242-5FA4-463F-AF31-83BBD4A97BEA"), Guid.Parse("7626D0DF-9F8A-4FE8-9062-3596165E148A"), "Regular", 11m, 1),
        Price(Guid.Parse("A57AA41F-25A7-46A0-B346-2B557DE1D9C1"), Guid.Parse("ECFC8BFA-6C51-4607-B7FF-FE9F59DB8FBC"), "Regular", 13m, 1),
        Price(Guid.Parse("724EB0D3-0208-43BC-9DA4-EAE38CB79F45"), Guid.Parse("6E97A8EE-16B1-4FEB-B6E0-2AB4E56658A0"), "Regular", 14m, 1),
        Price(Guid.Parse("3AEE6764-1CF7-492E-B597-E0A511E17978"), Guid.Parse("90DCE7E3-9CC6-4732-B7D2-F4D43056FBB8"), "Regular", 14m, 1),
        Price(Guid.Parse("B6CC31DE-9405-4AC0-B5E9-1505FBE9A83A"), Guid.Parse("0D440A2B-06A3-47F9-B129-1544F2F391A8"), "Regular", 13m, 1),
        Price(Guid.Parse("D3F87DDE-7690-4820-87A4-B065DD5B81D6"), Guid.Parse("95F39C20-E1BA-4FD2-992D-8D9E19600D64"), "Regular", 13m, 1),
        Price(Guid.Parse("C3ED0DA2-245D-44C1-854E-3428F91F2E2B"), Guid.Parse("6F2A75A4-C1E2-458F-BDE4-D825F987CC3D"), "Regular", 13m, 1),
        Price(Guid.Parse("1D14709D-C8B9-46B9-91CB-F997AC37BCE0"), Guid.Parse("B7AB3351-1B6B-45D0-B7B4-9782D79CFC65"), "Regular", 7m, 1),
        Price(Guid.Parse("D1A821D1-D919-45D4-A11D-13EDE02F145D"), Guid.Parse("5B1C6127-F7F0-497A-88B9-537E9110176F"), "Regular", 7m, 1),
        Price(Guid.Parse("40E09332-7D77-44FA-B03A-B24E9A65D1A5"), Guid.Parse("06F858A2-F226-4B2F-A912-A6330BBF4EC1"), "Regular", 7m, 1),
        Price(Guid.Parse("CF5A1B06-F3AD-47F7-8C3F-D5D0B6823B24"), Guid.Parse("8FCAA555-D618-4AD8-AE73-ABF51854A329"), "Regular", 6m, 1),
        Price(Guid.Parse("0DC7E9F1-2C5A-490A-8919-80D2983CD1E1"), Guid.Parse("44472C07-5F31-482A-8506-8A3C11CF1F26"), "Regular", 6m, 1),
        Price(Guid.Parse("DB1A72C1-6185-4F76-BF47-4A034A0DAEFE"), SundayPorkChopItemId, "Regular", 17m, 1)
    ];

    public static IReadOnlyList<MenuItemTabEntity> FoodItemTabs { get; } =
        Items.SelectMany(item => item.MenuItemId == SundayPorkChopItemId
            ? new[] { Tab(item.MenuItemId, MenuTab.Dinner) }
            : new[] { Tab(item.MenuItemId, MenuTab.Lunch), Tab(item.MenuItemId, MenuTab.Dinner) }).ToArray();

    public static IReadOnlyList<RecurringSpecialEntity> RecurringSpecials { get; } =
    [
        new() { RecurringSpecialId = Guid.Parse("33D64E7B-D5B7-481A-97FC-7F250A68C27E"), Tab = MenuTab.Dinner, MenuSectionId = BurgersSectionId, DayOfWeek = DayOfWeek.Monday, Title = "Monday Night Burgers", Description = "A dependable burger-night draw with fries and easy weeknight pricing.", TimeNote = "After 5:00 PM", PriceNote = "$11 basket special", LinkedMenuItemId = Guid.Parse("7626D0DF-9F8A-4FE8-9062-3596165E148A"), SortOrder = 1, IsVisibleToGuests = true, IsArchived = false },
        new() { RecurringSpecialId = Guid.Parse("5EE7BBEA-C2F4-4D5B-BCDB-BD0FD0A06704"), Tab = MenuTab.Dinner, MenuSectionId = AppetizersSectionId, DayOfWeek = DayOfWeek.Tuesday, Title = "Tuesday Taco Basket", Description = "A taco-night feature built for quick dinner traffic and casual bar seating.", TimeNote = "After 4:00 PM", PriceNote = "$10 dinner feature", LinkedMenuItemId = Guid.Parse("E75D8A92-F1D2-4D58-9CD0-9B7E80CE9D80"), SortOrder = 2, IsVisibleToGuests = true, IsArchived = false },
        new() { RecurringSpecialId = Guid.Parse("7E8222C3-63EC-4B4B-B777-D1E3AA7C5A86"), Tab = MenuTab.Dinner, MenuSectionId = WingsSectionId, DayOfWeek = DayOfWeek.Wednesday, Title = "Wing Night", Description = "Sauced wings with a strong shareable hook for midweek regulars.", TimeNote = "After 5:00 PM", PriceNote = "$16 dozen special", LinkedMenuItemId = Guid.Parse("1C4D4F34-5260-4F7D-ABCB-1C6875B7EBF8"), SortOrder = 3, IsVisibleToGuests = true, IsArchived = false },
        new() { RecurringSpecialId = Guid.Parse("88BB945A-B7B4-4725-972B-60A042E524E9"), Tab = MenuTab.Dinner, MenuSectionId = SandwichesSectionId, DayOfWeek = DayOfWeek.Friday, Title = "Friday Fish Fry", Description = "A Friday dinner anchor that deserves a permanent home in the guest menu flow.", TimeNote = "After 4:00 PM", PriceNote = "$15 dinner plate", LinkedMenuItemId = Guid.Parse("E1FD2B7F-D7E0-47CC-9E3E-4BC3A30AA4B8"), SortOrder = 4, IsVisibleToGuests = true, IsArchived = false },
        new() { RecurringSpecialId = Guid.Parse("6BAA63B3-55C9-4E47-8555-803573B9B38D"), Tab = MenuTab.Dinner, MenuSectionId = DinnerSpecialsSectionId, DayOfWeek = DayOfWeek.Sunday, Title = "Sunday Pork Chop Dinner", Description = "A hearty end-of-week dinner special that should read as a repeatable tradition.", TimeNote = "After 3:00 PM", PriceNote = "$17 dinner plate", LinkedMenuItemId = SundayPorkChopItemId, SortOrder = 5, IsVisibleToGuests = true, IsArchived = false }
    ];

    public static IReadOnlyList<MenuServiceWindowEntity> ServiceWindows { get; } =
    [
        Window(MenuTab.Breakfast, DayOfWeek.Monday, false, null, null, false),
        Window(MenuTab.Breakfast, DayOfWeek.Tuesday, false, null, null, false),
        Window(MenuTab.Breakfast, DayOfWeek.Wednesday, false, null, null, false),
        Window(MenuTab.Breakfast, DayOfWeek.Thursday, false, null, null, false),
        Window(MenuTab.Breakfast, DayOfWeek.Friday, false, null, null, false),
        Window(MenuTab.Breakfast, DayOfWeek.Saturday, true, new TimeOnly(10, 0), new TimeOnly(13, 0), false),
        Window(MenuTab.Breakfast, DayOfWeek.Sunday, true, new TimeOnly(10, 0), new TimeOnly(13, 0), false),

        Window(MenuTab.Lunch, DayOfWeek.Monday, false, null, null, false),
        Window(MenuTab.Lunch, DayOfWeek.Tuesday, true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
        Window(MenuTab.Lunch, DayOfWeek.Wednesday, true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
        Window(MenuTab.Lunch, DayOfWeek.Thursday, true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
        Window(MenuTab.Lunch, DayOfWeek.Friday, true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
        Window(MenuTab.Lunch, DayOfWeek.Saturday, true, new TimeOnly(11, 0), new TimeOnly(16, 0), false),
        Window(MenuTab.Lunch, DayOfWeek.Sunday, true, new TimeOnly(11, 0), new TimeOnly(15, 0), false),

        Window(MenuTab.Dinner, DayOfWeek.Monday, true, new TimeOnly(17, 0), new TimeOnly(20, 0), false),
        Window(MenuTab.Dinner, DayOfWeek.Tuesday, true, new TimeOnly(16, 0), new TimeOnly(21, 0), false),
        Window(MenuTab.Dinner, DayOfWeek.Wednesday, true, new TimeOnly(16, 0), new TimeOnly(21, 0), false),
        Window(MenuTab.Dinner, DayOfWeek.Thursday, true, new TimeOnly(16, 0), new TimeOnly(21, 0), false),
        Window(MenuTab.Dinner, DayOfWeek.Friday, true, new TimeOnly(16, 0), new TimeOnly(22, 0), false),
        Window(MenuTab.Dinner, DayOfWeek.Saturday, true, new TimeOnly(16, 0), new TimeOnly(22, 0), false),
        Window(MenuTab.Dinner, DayOfWeek.Sunday, true, new TimeOnly(15, 0), new TimeOnly(20, 0), false),

        Window(MenuTab.Drinks, DayOfWeek.Monday, true, new TimeOnly(16, 0), new TimeOnly(21, 0), false),
        Window(MenuTab.Drinks, DayOfWeek.Tuesday, true, new TimeOnly(11, 0), new TimeOnly(22, 0), false),
        Window(MenuTab.Drinks, DayOfWeek.Wednesday, true, new TimeOnly(11, 0), new TimeOnly(22, 0), false),
        Window(MenuTab.Drinks, DayOfWeek.Thursday, true, new TimeOnly(11, 0), new TimeOnly(22, 0), false),
        Window(MenuTab.Drinks, DayOfWeek.Friday, true, new TimeOnly(11, 0), new TimeOnly(0, 0), true),
        Window(MenuTab.Drinks, DayOfWeek.Saturday, true, new TimeOnly(10, 0), new TimeOnly(0, 0), true),
        Window(MenuTab.Drinks, DayOfWeek.Sunday, true, new TimeOnly(10, 0), new TimeOnly(21, 0), false)
    ];

    private static MenuServiceWindowEntity Window(MenuTab tab, DayOfWeek dayOfWeek, bool isAvailable, TimeOnly? opensAt, TimeOnly? closesAt, bool closesNextDay) =>
        new()
        {
            Tab = tab,
            DayOfWeek = dayOfWeek,
            IsAvailable = isAvailable,
            OpensAt = opensAt,
            ClosesAt = closesAt,
            ClosesNextDay = closesNextDay
        };
}
