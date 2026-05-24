using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Anchor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MenuSections",
                columns: table => new
                {
                    MenuSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Family = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisibleToGuests = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuSections", x => x.MenuSectionId);
                });

            migrationBuilder.CreateTable(
                name: "MenuServiceWindows",
                columns: table => new
                {
                    Tab = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    OpensAt = table.Column<TimeOnly>(type: "time", nullable: true),
                    ClosesAt = table.Column<TimeOnly>(type: "time", nullable: true),
                    ClosesNextDay = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuServiceWindows", x => new { x.Tab, x.DayOfWeek });
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisibleToGuests = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    OfferStartsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    OfferEndsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    IsSeasonal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.MenuItemId);
                    table.ForeignKey(
                        name: "FK_MenuItems_MenuSections_MenuSectionId",
                        column: x => x.MenuSectionId,
                        principalTable: "MenuSections",
                        principalColumn: "MenuSectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemPriceVariants",
                columns: table => new
                {
                    MenuItemPriceVariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemPriceVariants", x => x.MenuItemPriceVariantId);
                    table.ForeignKey(
                        name: "FK_MenuItemPriceVariants_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "MenuItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemTabs",
                columns: table => new
                {
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tab = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemTabs", x => new { x.MenuItemId, x.Tab });
                    table.ForeignKey(
                        name: "FK_MenuItemTabs_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "MenuItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecurringSpecials",
                columns: table => new
                {
                    RecurringSpecialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tab = table.Column<int>(type: "int", nullable: false),
                    MenuSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TimeNote = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PriceNote = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LinkedMenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisibleToGuests = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringSpecials", x => x.RecurringSpecialId);
                    table.ForeignKey(
                        name: "FK_RecurringSpecials_MenuItems_LinkedMenuItemId",
                        column: x => x.LinkedMenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "MenuItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringSpecials_MenuSections_MenuSectionId",
                        column: x => x.MenuSectionId,
                        principalTable: "MenuSections",
                        principalColumn: "MenuSectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "MenuSections",
                columns: new[] { "MenuSectionId", "Family", "IsArchived", "IsVisibleToGuests", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("0e4de526-5921-4c3b-8985-d83344642a41"), 1, false, true, "Dinner Specials", 0 },
                    { new Guid("198ccf8a-72fd-4278-a360-f36d5871e58b"), 1, false, true, "Burgers", 5 },
                    { new Guid("2ea5e671-e8ac-4c8a-b3d9-4c136a32a71b"), 1, false, true, "Kids Menu", 7 },
                    { new Guid("31e9cb99-5fca-4a4a-a04b-89b97c926a52"), 1, false, true, "Soups & Salads", 3 },
                    { new Guid("4a3a2d15-2af0-44a7-84c8-67b603a3ddb4"), 1, false, true, "Wings", 2 },
                    { new Guid("7f644c28-9275-4df8-8e4b-482f47568cfb"), 1, false, true, "Sandwiches", 4 },
                    { new Guid("a8f0b603-e02d-49f5-873d-1bb6bfc16c0f"), 1, false, true, "Desserts", 8 },
                    { new Guid("d67bd219-6d64-4a08-8ce4-d036a0c7b16d"), 1, false, true, "Appetizers", 1 },
                    { new Guid("fa5da0f9-7e81-4b9d-9e11-fa5b1f828c72"), 1, false, true, "Wraps", 6 }
                });

            migrationBuilder.InsertData(
                table: "MenuServiceWindows",
                columns: new[] { "DayOfWeek", "Tab", "ClosesAt", "ClosesNextDay", "IsAvailable", "OpensAt" },
                values: new object[,]
                {
                    { 0, 1, new TimeOnly(13, 0, 0), false, true, new TimeOnly(10, 0, 0) },
                    { 1, 1, null, false, false, null },
                    { 2, 1, null, false, false, null },
                    { 3, 1, null, false, false, null },
                    { 4, 1, null, false, false, null },
                    { 5, 1, null, false, false, null },
                    { 6, 1, new TimeOnly(13, 0, 0), false, true, new TimeOnly(10, 0, 0) },
                    { 0, 2, new TimeOnly(15, 0, 0), false, true, new TimeOnly(11, 0, 0) },
                    { 1, 2, null, false, false, null },
                    { 2, 2, new TimeOnly(16, 0, 0), false, true, new TimeOnly(11, 0, 0) },
                    { 3, 2, new TimeOnly(16, 0, 0), false, true, new TimeOnly(11, 0, 0) },
                    { 4, 2, new TimeOnly(16, 0, 0), false, true, new TimeOnly(11, 0, 0) },
                    { 5, 2, new TimeOnly(16, 0, 0), false, true, new TimeOnly(11, 0, 0) },
                    { 6, 2, new TimeOnly(16, 0, 0), false, true, new TimeOnly(11, 0, 0) },
                    { 0, 3, new TimeOnly(20, 0, 0), false, true, new TimeOnly(15, 0, 0) },
                    { 1, 3, new TimeOnly(20, 0, 0), false, true, new TimeOnly(17, 0, 0) },
                    { 2, 3, new TimeOnly(21, 0, 0), false, true, new TimeOnly(16, 0, 0) },
                    { 3, 3, new TimeOnly(21, 0, 0), false, true, new TimeOnly(16, 0, 0) },
                    { 4, 3, new TimeOnly(21, 0, 0), false, true, new TimeOnly(16, 0, 0) },
                    { 5, 3, new TimeOnly(22, 0, 0), false, true, new TimeOnly(16, 0, 0) },
                    { 6, 3, new TimeOnly(22, 0, 0), false, true, new TimeOnly(16, 0, 0) },
                    { 0, 4, new TimeOnly(21, 0, 0), false, true, new TimeOnly(10, 0, 0) },
                    { 1, 4, new TimeOnly(21, 0, 0), false, true, new TimeOnly(16, 0, 0) },
                    { 2, 4, new TimeOnly(22, 0, 0), false, true, new TimeOnly(11, 0, 0) },
                    { 3, 4, new TimeOnly(22, 0, 0), false, true, new TimeOnly(11, 0, 0) },
                    { 4, 4, new TimeOnly(22, 0, 0), false, true, new TimeOnly(11, 0, 0) },
                    { 5, 4, new TimeOnly(0, 0, 0), true, true, new TimeOnly(11, 0, 0) },
                    { 6, 4, new TimeOnly(0, 0, 0), true, true, new TimeOnly(10, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "MenuItemId", "Description", "ImagePath", "IsArchived", "IsSeasonal", "IsVisibleToGuests", "MenuSectionId", "Name", "OfferEndsOn", "OfferStartsOn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("06f858a2-f226-4b2f-a912-a6330bbf4ec1"), "Served with sauce.", null, false, false, true, new Guid("2ea5e671-e8ac-4c8a-b3d9-4c136a32a71b"), "Chicken Strips", null, null, 3 },
                    { new Guid("0d440a2b-06a3-47f9-b129-1544f2f391a8"), "Grilled or crispy chicken, tomatoes, onions, lettuce, and dressing.", "images/menu/wraps.svg", false, false, true, new Guid("fa5da0f9-7e81-4b9d-9e11-fa5b1f828c72"), "Chicken Wrap", null, null, 1 },
                    { new Guid("1af4708e-e741-4621-95e3-6c8f24af2be6"), "Swiss cheese, grilled ham, smoked bacon, and classic ranch.", null, false, false, true, new Guid("7f644c28-9275-4df8-8e4b-482f47568cfb"), "Ranch Melt", null, null, 3 },
                    { new Guid("1c4d4f34-5260-4f7d-abcb-1c6875b7ebf8"), "Choice of two sauces.", "images/menu/wings.svg", false, false, true, new Guid("4a3a2d15-2af0-44a7-84c8-67b603a3ddb4"), "Traditional or Boneless (12)", null, null, 2 },
                    { new Guid("3b7745b6-66d4-4db7-8ee3-b018834f58f7"), "Grilled sirloin, smoked gouda, peppers, and onions.", "images/menu/sandwiches.svg", false, false, true, new Guid("7f644c28-9275-4df8-8e4b-482f47568cfb"), "Steak Sandwich", null, null, 2 },
                    { new Guid("44472c07-5f31-482a-8506-8a3c11cf1f26"), "Fair-style donuts for a casual sweet finish.", "images/menu/desserts.svg", false, false, true, new Guid("a8f0b603-e02d-49f5-873d-1bb6bfc16c0f"), "Mini Donuts", null, null, 2 },
                    { new Guid("590cc0e4-8be8-48e8-97b8-908ea7a1fc9a"), "Lettuce, tomato, and mayo on a toasted bun.", "images/menu/sandwiches.svg", false, false, true, new Guid("7f644c28-9275-4df8-8e4b-482f47568cfb"), "Grilled Chicken Sandwich", null, null, 1 },
                    { new Guid("5b1c6127-f7f0-497a-88b9-537e9110176f"), "The classic choice for a quick family meal.", "images/menu/kids.svg", false, false, true, new Guid("2ea5e671-e8ac-4c8a-b3d9-4c136a32a71b"), "Mini Corn Dogs", null, null, 2 },
                    { new Guid("5c3a9530-0f24-4d62-883b-f01b0a4286c2"), "Served with salsa and sour cream.", null, false, false, true, new Guid("d67bd219-6d64-4a08-8ce4-d036a0c7b16d"), "Mini Tacos", null, new DateOnly(2026, 5, 31), 2 },
                    { new Guid("6e97a8ee-16b1-4feb-b6e0-2ab4e56658a0"), "Bacon, BBQ sauce, and a crisp onion ring.", null, false, false, true, new Guid("198ccf8a-72fd-4278-a360-f36d5871e58b"), "Western Burger", null, null, 3 },
                    { new Guid("6f2a75a4-c1e2-458f-bde4-d825f987cc3d"), "Crispy chicken tossed in buffalo with ranch-style cooling balance.", null, false, false, true, new Guid("fa5da0f9-7e81-4b9d-9e11-fa5b1f828c72"), "Buffalo Chicken Wrap", null, null, 3 },
                    { new Guid("73ea7283-893f-4d14-8081-39f63bd54d13"), "Crisp greens, tomatoes, peppers, shaved red onions, and your choice of dressing.", "images/menu/salads.svg", false, false, true, new Guid("31e9cb99-5fca-4a4a-a04b-89b97c926a52"), "The Anchor Salad", null, null, 1 },
                    { new Guid("7626d0df-9f8a-4fe8-9062-3596165e148a"), "Fresh hand-pattied burger; add cheese if desired.", "images/menu/burgers.svg", false, false, true, new Guid("198ccf8a-72fd-4278-a360-f36d5871e58b"), "Classic Hamburger", null, null, 1 },
                    { new Guid("79663ef8-29ff-4d24-8b1c-cfa8dad8ba72"), "Choice of one sauce.", "images/menu/wings.svg", false, false, true, new Guid("4a3a2d15-2af0-44a7-84c8-67b603a3ddb4"), "Traditional or Boneless (6)", null, null, 1 },
                    { new Guid("8c5bde4d-3fb2-4a02-8ab5-40d3e0b49387"), "Finished with crumbled smoked salmon and poppyseed dressing.", "images/menu/salads.svg", false, true, true, new Guid("31e9cb99-5fca-4a4a-a04b-89b97c926a52"), "Smoked Salmon Salad", new DateOnly(2026, 7, 10), new DateOnly(2026, 5, 26), 2 },
                    { new Guid("8fcaa555-d618-4ad8-ae73-abf51854a329"), "Served warm with ice cream.", "images/menu/desserts.svg", false, false, true, new Guid("a8f0b603-e02d-49f5-873d-1bb6bfc16c0f"), "Chocolate Lava Cake", new DateOnly(2026, 6, 2), new DateOnly(2026, 5, 15), 1 },
                    { new Guid("90dce7e3-9cc6-4732-b7d2-f4d43056fbb8"), "Bacon, American cheese, and egg.", null, false, false, true, new Guid("198ccf8a-72fd-4278-a360-f36d5871e58b"), "Sunrise Burger", null, null, 4 },
                    { new Guid("95f39c20-e1ba-4fd2-992d-8d9e19600d64"), "Steak, peppers, onions, cheese, lettuce, and dressing.", "images/menu/wraps.svg", false, false, true, new Guid("fa5da0f9-7e81-4b9d-9e11-fa5b1f828c72"), "Steak Wrap", null, null, 2 },
                    { new Guid("b7ab3351-1b6b-45d0-b7b4-9782d79cfc65"), "Served with one side and a kid drink option.", null, false, false, true, new Guid("2ea5e671-e8ac-4c8a-b3d9-4c136a32a71b"), "Mac & Cheese", null, null, 1 },
                    { new Guid("c88652a0-c9f2-4a7d-b4ac-8ddbfc9ff4e5"), "Crisp white cheddar curds with your choice of dipping sauce.", "images/menu/appetizers.svg", false, false, true, new Guid("d67bd219-6d64-4a08-8ce4-d036a0c7b16d"), "Cheese Curds", null, null, 1 },
                    { new Guid("ca5cd1b7-8c73-4b21-b3e4-8e98fea44ee9"), "Upgrade any wing order with a side of fries.", null, false, false, true, new Guid("4a3a2d15-2af0-44a7-84c8-67b603a3ddb4"), "Add Fries", null, null, 3 },
                    { new Guid("db2a7b2f-d9e9-4433-80a3-baeb5e5b5728"), "Cup or bowl, updated as the kitchen rotates specials.", null, false, false, true, new Guid("31e9cb99-5fca-4a4a-a04b-89b97c926a52"), "Seasonal Soup", null, null, 4 },
                    { new Guid("e1fd2b7f-d7e0-47cc-9e3e-4bc3a30aa4b8"), "Breaded walleye on a toasted bun.", null, false, false, true, new Guid("7f644c28-9275-4df8-8e4b-482f47568cfb"), "Walleye Sandwich", null, null, 4 },
                    { new Guid("e75d8a92-f1d2-4d58-9cd0-9b7e80ce9d80"), "Finished with Boom Boom sauce for a bold bar-food favorite.", "images/menu/appetizers.svg", false, false, true, new Guid("d67bd219-6d64-4a08-8ce4-d036a0c7b16d"), "Fish Tacos", new DateOnly(2026, 6, 3), new DateOnly(2026, 5, 12), 4 },
                    { new Guid("e9d5a6c9-9a4c-4e98-8c72-2ae28bfcba97"), "Bacon, tomatoes, greens, and your choice of dressing.", null, false, false, true, new Guid("31e9cb99-5fca-4a4a-a04b-89b97c926a52"), "BLT Salad", null, null, 3 },
                    { new Guid("ecfc8bfa-6c51-4607-b7ff-fe9f59db8fbc"), "A familiar favorite with bacon and melty cheese.", "images/menu/burgers.svg", false, false, true, new Guid("198ccf8a-72fd-4278-a360-f36d5871e58b"), "Bacon Cheeseburger", null, null, 2 },
                    { new Guid("ff4ee65c-89e7-49f7-9023-8579ccb8307b"), "Loaded with cheese and served with salsa and sour cream.", null, false, true, true, new Guid("d67bd219-6d64-4a08-8ce4-d036a0c7b16d"), "Quesadillas", new DateOnly(2026, 7, 10), new DateOnly(2026, 5, 9), 3 }
                });

            migrationBuilder.InsertData(
                table: "RecurringSpecials",
                columns: new[] { "RecurringSpecialId", "DayOfWeek", "Description", "IsArchived", "IsVisibleToGuests", "LinkedMenuItemId", "MenuSectionId", "PriceNote", "SortOrder", "Tab", "TimeNote", "Title" },
                values: new object[] { new Guid("6baa63b3-55c9-4e47-8555-803573b9b38d"), 0, "A hearty end-of-week dinner special that should read as a repeatable tradition.", false, true, null, new Guid("0e4de526-5921-4c3b-8985-d83344642a41"), "$17 dinner plate", 5, 3, "After 3:00 PM", "Sunday Pork Chop Dinner" });

            migrationBuilder.InsertData(
                table: "MenuItemPriceVariants",
                columns: new[] { "MenuItemPriceVariantId", "Amount", "Label", "MenuItemId", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("0dc7e9f1-2c5a-490a-8919-80d2983cd1e1"), 6m, "Regular", new Guid("44472c07-5f31-482a-8506-8a3c11cf1f26"), 1 },
                    { new Guid("1d14709d-c8b9-46b9-91cb-f997ac37bce0"), 7m, "Regular", new Guid("b7ab3351-1b6b-45d0-b7b4-9782d79cfc65"), 1 },
                    { new Guid("2d12eaf9-ebca-4ea9-9ecc-663ff531d9ed"), 9m, "Regular", new Guid("5c3a9530-0f24-4d62-883b-f01b0a4286c2"), 1 },
                    { new Guid("305be09a-e819-48de-b6bb-7771aabfd65a"), 16m, "Regular", new Guid("1c4d4f34-5260-4f7d-abcb-1c6875b7ebf8"), 1 },
                    { new Guid("388c7811-3724-4937-86f7-4e5de1836535"), 10m, "Regular", new Guid("73ea7283-893f-4d14-8081-39f63bd54d13"), 1 },
                    { new Guid("3aee6764-1cf7-492e-b597-e0a511e17978"), 14m, "Regular", new Guid("90dce7e3-9cc6-4732-b7d2-f4d43056fbb8"), 1 },
                    { new Guid("3f6a665b-3334-4315-b208-57cf9cc4b234"), 13m, "Regular", new Guid("590cc0e4-8be8-48e8-97b8-908ea7a1fc9a"), 1 },
                    { new Guid("40e09332-7d77-44fa-b03a-b24e9a65d1a5"), 7m, "Regular", new Guid("06f858a2-f226-4b2f-a912-a6330bbf4ec1"), 1 },
                    { new Guid("4db158e0-ee22-4b83-846a-f62446fd7fe5"), 4m, "Cup", new Guid("db2a7b2f-d9e9-4433-80a3-baeb5e5b5728"), 1 },
                    { new Guid("5fa1bba0-2afa-45ef-be7b-6cc2c1354b9b"), 9m, "Regular", new Guid("79663ef8-29ff-4d24-8b1c-cfa8dad8ba72"), 1 },
                    { new Guid("65e43e63-0a48-4b0c-b3b1-3956101a4f56"), 11m, "Regular", new Guid("ff4ee65c-89e7-49f7-9023-8579ccb8307b"), 1 },
                    { new Guid("724eb0d3-0208-43bc-9da4-eae38cb79f45"), 14m, "Regular", new Guid("6e97a8ee-16b1-4feb-b6e0-2ab4e56658a0"), 1 },
                    { new Guid("7a5b50d6-a069-41d1-af29-4d308a93e9da"), 13m, "Regular", new Guid("1af4708e-e741-4621-95e3-6c8f24af2be6"), 1 },
                    { new Guid("7b70feb6-0dba-4aa0-8b6e-1a791b196d31"), 6m, "Bowl", new Guid("db2a7b2f-d9e9-4433-80a3-baeb5e5b5728"), 2 },
                    { new Guid("8e59cf80-d0fc-43a2-9442-9262f77d9b8a"), 3m, "Regular", new Guid("ca5cd1b7-8c73-4b21-b3e4-8e98fea44ee9"), 1 },
                    { new Guid("90ccf394-072e-47f2-80b5-85a00e58b6d5"), 10m, "Regular", new Guid("e75d8a92-f1d2-4d58-9cd0-9b7e80ce9d80"), 1 },
                    { new Guid("916e3d70-2cb3-4273-a283-3241fb7fa0d0"), 9m, "Regular", new Guid("c88652a0-c9f2-4a7d-b4ac-8ddbfc9ff4e5"), 1 },
                    { new Guid("957a3058-ad38-4da8-ae9a-d2670e408831"), 12m, "Regular", new Guid("e9d5a6c9-9a4c-4e98-8c72-2ae28bfcba97"), 1 },
                    { new Guid("a57aa41f-25a7-46a0-b346-2b557de1d9c1"), 13m, "Regular", new Guid("ecfc8bfa-6c51-4607-b7ff-fe9f59db8fbc"), 1 },
                    { new Guid("abaa4242-5fa4-463f-af31-83bbd4a97bea"), 11m, "Regular", new Guid("7626d0df-9f8a-4fe8-9062-3596165e148a"), 1 },
                    { new Guid("b6cc31de-9405-4ac0-b5e9-1505fbe9a83a"), 13m, "Regular", new Guid("0d440a2b-06a3-47f9-b129-1544f2f391a8"), 1 },
                    { new Guid("c3ed0da2-245d-44c1-854e-3428f91f2e2b"), 13m, "Regular", new Guid("6f2a75a4-c1e2-458f-bde4-d825f987cc3d"), 1 },
                    { new Guid("cf5a1b06-f3ad-47f7-8c3f-d5d0b6823b24"), 6m, "Regular", new Guid("8fcaa555-d618-4ad8-ae73-abf51854a329"), 1 },
                    { new Guid("d1a821d1-d919-45d4-a11d-13ede02f145d"), 7m, "Regular", new Guid("5b1c6127-f7f0-497a-88b9-537e9110176f"), 1 },
                    { new Guid("d1d39b7a-30a0-4a9a-b9b0-c8dd1c8c4c74"), 14m, "Regular", new Guid("e1fd2b7f-d7e0-47cc-9e3e-4bc3a30aa4b8"), 1 },
                    { new Guid("d37f81b0-fb88-4daf-9fb8-dbd62afa8392"), 13m, "Regular", new Guid("8c5bde4d-3fb2-4a02-8ab5-40d3e0b49387"), 1 },
                    { new Guid("d3f87dde-7690-4820-87a4-b065dd5b81d6"), 13m, "Regular", new Guid("95f39c20-e1ba-4fd2-992d-8d9e19600d64"), 1 },
                    { new Guid("d8e8c71b-0234-4742-811a-71ad0e65a094"), 14m, "Regular", new Guid("3b7745b6-66d4-4db7-8ee3-b018834f58f7"), 1 }
                });

            migrationBuilder.InsertData(
                table: "MenuItemTabs",
                columns: new[] { "MenuItemId", "Tab" },
                values: new object[,]
                {
                    { new Guid("06f858a2-f226-4b2f-a912-a6330bbf4ec1"), 2 },
                    { new Guid("06f858a2-f226-4b2f-a912-a6330bbf4ec1"), 3 },
                    { new Guid("0d440a2b-06a3-47f9-b129-1544f2f391a8"), 2 },
                    { new Guid("0d440a2b-06a3-47f9-b129-1544f2f391a8"), 3 },
                    { new Guid("1af4708e-e741-4621-95e3-6c8f24af2be6"), 2 },
                    { new Guid("1af4708e-e741-4621-95e3-6c8f24af2be6"), 3 },
                    { new Guid("1c4d4f34-5260-4f7d-abcb-1c6875b7ebf8"), 2 },
                    { new Guid("1c4d4f34-5260-4f7d-abcb-1c6875b7ebf8"), 3 },
                    { new Guid("3b7745b6-66d4-4db7-8ee3-b018834f58f7"), 2 },
                    { new Guid("3b7745b6-66d4-4db7-8ee3-b018834f58f7"), 3 },
                    { new Guid("44472c07-5f31-482a-8506-8a3c11cf1f26"), 2 },
                    { new Guid("44472c07-5f31-482a-8506-8a3c11cf1f26"), 3 },
                    { new Guid("590cc0e4-8be8-48e8-97b8-908ea7a1fc9a"), 2 },
                    { new Guid("590cc0e4-8be8-48e8-97b8-908ea7a1fc9a"), 3 },
                    { new Guid("5b1c6127-f7f0-497a-88b9-537e9110176f"), 2 },
                    { new Guid("5b1c6127-f7f0-497a-88b9-537e9110176f"), 3 },
                    { new Guid("5c3a9530-0f24-4d62-883b-f01b0a4286c2"), 2 },
                    { new Guid("5c3a9530-0f24-4d62-883b-f01b0a4286c2"), 3 },
                    { new Guid("6e97a8ee-16b1-4feb-b6e0-2ab4e56658a0"), 2 },
                    { new Guid("6e97a8ee-16b1-4feb-b6e0-2ab4e56658a0"), 3 },
                    { new Guid("6f2a75a4-c1e2-458f-bde4-d825f987cc3d"), 2 },
                    { new Guid("6f2a75a4-c1e2-458f-bde4-d825f987cc3d"), 3 },
                    { new Guid("73ea7283-893f-4d14-8081-39f63bd54d13"), 2 },
                    { new Guid("73ea7283-893f-4d14-8081-39f63bd54d13"), 3 },
                    { new Guid("7626d0df-9f8a-4fe8-9062-3596165e148a"), 2 },
                    { new Guid("7626d0df-9f8a-4fe8-9062-3596165e148a"), 3 },
                    { new Guid("79663ef8-29ff-4d24-8b1c-cfa8dad8ba72"), 2 },
                    { new Guid("79663ef8-29ff-4d24-8b1c-cfa8dad8ba72"), 3 },
                    { new Guid("8c5bde4d-3fb2-4a02-8ab5-40d3e0b49387"), 2 },
                    { new Guid("8c5bde4d-3fb2-4a02-8ab5-40d3e0b49387"), 3 },
                    { new Guid("8fcaa555-d618-4ad8-ae73-abf51854a329"), 2 },
                    { new Guid("8fcaa555-d618-4ad8-ae73-abf51854a329"), 3 },
                    { new Guid("90dce7e3-9cc6-4732-b7d2-f4d43056fbb8"), 2 },
                    { new Guid("90dce7e3-9cc6-4732-b7d2-f4d43056fbb8"), 3 },
                    { new Guid("95f39c20-e1ba-4fd2-992d-8d9e19600d64"), 2 },
                    { new Guid("95f39c20-e1ba-4fd2-992d-8d9e19600d64"), 3 },
                    { new Guid("b7ab3351-1b6b-45d0-b7b4-9782d79cfc65"), 2 },
                    { new Guid("b7ab3351-1b6b-45d0-b7b4-9782d79cfc65"), 3 },
                    { new Guid("c88652a0-c9f2-4a7d-b4ac-8ddbfc9ff4e5"), 2 },
                    { new Guid("c88652a0-c9f2-4a7d-b4ac-8ddbfc9ff4e5"), 3 },
                    { new Guid("ca5cd1b7-8c73-4b21-b3e4-8e98fea44ee9"), 2 },
                    { new Guid("ca5cd1b7-8c73-4b21-b3e4-8e98fea44ee9"), 3 },
                    { new Guid("db2a7b2f-d9e9-4433-80a3-baeb5e5b5728"), 2 },
                    { new Guid("db2a7b2f-d9e9-4433-80a3-baeb5e5b5728"), 3 },
                    { new Guid("e1fd2b7f-d7e0-47cc-9e3e-4bc3a30aa4b8"), 2 },
                    { new Guid("e1fd2b7f-d7e0-47cc-9e3e-4bc3a30aa4b8"), 3 },
                    { new Guid("e75d8a92-f1d2-4d58-9cd0-9b7e80ce9d80"), 2 },
                    { new Guid("e75d8a92-f1d2-4d58-9cd0-9b7e80ce9d80"), 3 },
                    { new Guid("e9d5a6c9-9a4c-4e98-8c72-2ae28bfcba97"), 2 },
                    { new Guid("e9d5a6c9-9a4c-4e98-8c72-2ae28bfcba97"), 3 },
                    { new Guid("ecfc8bfa-6c51-4607-b7ff-fe9f59db8fbc"), 2 },
                    { new Guid("ecfc8bfa-6c51-4607-b7ff-fe9f59db8fbc"), 3 },
                    { new Guid("ff4ee65c-89e7-49f7-9023-8579ccb8307b"), 2 },
                    { new Guid("ff4ee65c-89e7-49f7-9023-8579ccb8307b"), 3 }
                });

            migrationBuilder.InsertData(
                table: "RecurringSpecials",
                columns: new[] { "RecurringSpecialId", "DayOfWeek", "Description", "IsArchived", "IsVisibleToGuests", "LinkedMenuItemId", "MenuSectionId", "PriceNote", "SortOrder", "Tab", "TimeNote", "Title" },
                values: new object[,]
                {
                    { new Guid("33d64e7b-d5b7-481a-97fc-7f250a68c27e"), 1, "A dependable burger-night draw with fries and easy weeknight pricing.", false, true, new Guid("7626d0df-9f8a-4fe8-9062-3596165e148a"), new Guid("198ccf8a-72fd-4278-a360-f36d5871e58b"), "$11 basket special", 1, 3, "After 5:00 PM", "Monday Night Burgers" },
                    { new Guid("5ee7bbea-c2f4-4d5b-bcdb-bd0fd0a06704"), 2, "A taco-night feature built for quick dinner traffic and casual bar seating.", false, true, new Guid("e75d8a92-f1d2-4d58-9cd0-9b7e80ce9d80"), new Guid("d67bd219-6d64-4a08-8ce4-d036a0c7b16d"), "$10 dinner feature", 2, 3, "After 4:00 PM", "Tuesday Taco Basket" },
                    { new Guid("7e8222c3-63ec-4b4b-b777-d1e3aa7c5a86"), 3, "Sauced wings with a strong shareable hook for midweek regulars.", false, true, new Guid("1c4d4f34-5260-4f7d-abcb-1c6875b7ebf8"), new Guid("4a3a2d15-2af0-44a7-84c8-67b603a3ddb4"), "$16 dozen special", 3, 3, "After 5:00 PM", "Wing Night" },
                    { new Guid("88bb945a-b7b4-4725-972b-60a042e524e9"), 5, "A Friday dinner anchor that deserves a permanent home in the guest menu flow.", false, true, new Guid("e1fd2b7f-d7e0-47cc-9e3e-4bc3a30aa4b8"), new Guid("7f644c28-9275-4df8-8e4b-482f47568cfb"), "$15 dinner plate", 4, 3, "After 4:00 PM", "Friday Fish Fry" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemPriceVariants_MenuItemId",
                table: "MenuItemPriceVariants",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuSectionId",
                table: "MenuItems",
                column: "MenuSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringSpecials_LinkedMenuItemId",
                table: "RecurringSpecials",
                column: "LinkedMenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringSpecials_MenuSectionId",
                table: "RecurringSpecials",
                column: "MenuSectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MenuItemPriceVariants");

            migrationBuilder.DropTable(
                name: "MenuItemTabs");

            migrationBuilder.DropTable(
                name: "MenuServiceWindows");

            migrationBuilder.DropTable(
                name: "RecurringSpecials");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "MenuSections");
        }
    }
}
