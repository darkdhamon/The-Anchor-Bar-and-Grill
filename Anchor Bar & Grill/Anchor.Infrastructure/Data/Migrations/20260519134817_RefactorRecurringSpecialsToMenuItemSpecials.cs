using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Anchor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorRecurringSpecialsToMenuItemSpecials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MenuItemSpecials",
                columns: table => new
                {
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleKind = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StartsAt = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndsAt = table.Column<TimeOnly>(type: "time", nullable: true),
                    ClosesNextDay = table.Column<bool>(type: "bit", nullable: false),
                    Callout = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemSpecials", x => x.MenuItemId);
                    table.ForeignKey(
                        name: "FK_MenuItemSpecials_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "MenuItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO MenuItems
                (
                    MenuItemId,
                    MenuSectionId,
                    Name,
                    Description,
                    ImagePath,
                    SortOrder,
                    IsVisibleToGuests,
                    IsArchived,
                    OfferStartsOn,
                    OfferEndsOn,
                    IsSeasonal
                )
                SELECT
                    rs.RecurringSpecialId,
                    COALESCE(linkedItem.MenuSectionId, rs.MenuSectionId),
                    rs.Title,
                    rs.Description,
                    linkedItem.ImagePath,
                    rs.SortOrder,
                    rs.IsVisibleToGuests,
                    rs.IsArchived,
                    NULL,
                    NULL,
                    0
                FROM RecurringSpecials rs
                LEFT JOIN MenuItems linkedItem
                    ON linkedItem.MenuItemId = rs.LinkedMenuItemId
                WHERE rs.RecurringSpecialId NOT IN
                (
                    '33D64E7B-D5B7-481A-97FC-7F250A68C27E',
                    '5EE7BBEA-C2F4-4D5B-BCDB-BD0FD0A06704',
                    '7E8222C3-63EC-4B4B-B777-D1E3AA7C5A86',
                    '88BB945A-B7B4-4725-972B-60A042E524E9',
                    '6BAA63B3-55C9-4E47-8555-803573B9B38D'
                )
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM MenuItems existing
                    WHERE existing.MenuItemId = rs.RecurringSpecialId
                );

                INSERT INTO MenuItemPriceVariants
                (
                    MenuItemPriceVariantId,
                    MenuItemId,
                    Label,
                    Amount,
                    SortOrder
                )
                SELECT
                    NEWID(),
                    rs.RecurringSpecialId,
                    linkedVariant.Label,
                    linkedVariant.Amount,
                    linkedVariant.SortOrder
                FROM RecurringSpecials rs
                INNER JOIN MenuItemPriceVariants linkedVariant
                    ON linkedVariant.MenuItemId = rs.LinkedMenuItemId
                WHERE rs.RecurringSpecialId NOT IN
                (
                    '33D64E7B-D5B7-481A-97FC-7F250A68C27E',
                    '5EE7BBEA-C2F4-4D5B-BCDB-BD0FD0A06704',
                    '7E8222C3-63EC-4B4B-B777-D1E3AA7C5A86',
                    '88BB945A-B7B4-4725-972B-60A042E524E9',
                    '6BAA63B3-55C9-4E47-8555-803573B9B38D'
                )
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM MenuItemPriceVariants existing
                    WHERE existing.MenuItemId = rs.RecurringSpecialId
                );

                INSERT INTO MenuItemTabs
                (
                    MenuItemId,
                    Tab
                )
                SELECT
                    rs.RecurringSpecialId,
                    rs.Tab
                FROM RecurringSpecials rs
                WHERE rs.Tab <> 4
                AND rs.RecurringSpecialId NOT IN
                (
                    '33D64E7B-D5B7-481A-97FC-7F250A68C27E',
                    '5EE7BBEA-C2F4-4D5B-BCDB-BD0FD0A06704',
                    '7E8222C3-63EC-4B4B-B777-D1E3AA7C5A86',
                    '88BB945A-B7B4-4725-972B-60A042E524E9',
                    '6BAA63B3-55C9-4E47-8555-803573B9B38D'
                )
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM MenuItemTabs existing
                    WHERE existing.MenuItemId = rs.RecurringSpecialId
                      AND existing.Tab = rs.Tab
                );

                INSERT INTO MenuItemSpecials
                (
                    MenuItemId,
                    ScheduleKind,
                    DayOfWeek,
                    StartDate,
                    EndDate,
                    StartsAt,
                    EndsAt,
                    ClosesNextDay,
                    Callout
                )
                SELECT
                    rs.RecurringSpecialId,
                    1,
                    rs.DayOfWeek,
                    '2026-01-01',
                    NULL,
                    CASE
                        WHEN rs.TimeNote LIKE 'After %'
                            THEN TRY_CONVERT(time, LTRIM(RTRIM(SUBSTRING(rs.TimeNote, 7, LEN(rs.TimeNote)))))
                        WHEN CHARINDEX('-', cleaned.CleanTimeNote) > 0
                            THEN TRY_CONVERT(time, LTRIM(RTRIM(LEFT(cleaned.CleanTimeNote, CHARINDEX('-', cleaned.CleanTimeNote) - 1))))
                        ELSE NULL
                    END,
                    CASE
                        WHEN CHARINDEX('-', cleaned.CleanTimeNote) > 0
                            THEN TRY_CONVERT(time, LTRIM(RTRIM(SUBSTRING(cleaned.CleanTimeNote, CHARINDEX('-', cleaned.CleanTimeNote) + 1, LEN(cleaned.CleanTimeNote)))))
                        ELSE NULL
                    END,
                    CASE
                        WHEN rs.TimeNote LIKE '%next day%' THEN 1
                        ELSE 0
                    END,
                    rs.PriceNote
                FROM RecurringSpecials rs
                CROSS APPLY
                (
                    SELECT LTRIM(RTRIM(REPLACE(REPLACE(rs.TimeNote, ' next day', ''), 'next day', ''))) AS CleanTimeNote
                ) cleaned
                WHERE rs.RecurringSpecialId NOT IN
                (
                    '33D64E7B-D5B7-481A-97FC-7F250A68C27E',
                    '5EE7BBEA-C2F4-4D5B-BCDB-BD0FD0A06704',
                    '7E8222C3-63EC-4B4B-B777-D1E3AA7C5A86',
                    '88BB945A-B7B4-4725-972B-60A042E524E9',
                    '6BAA63B3-55C9-4E47-8555-803573B9B38D'
                )
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM MenuItemSpecials existing
                    WHERE existing.MenuItemId = rs.RecurringSpecialId
                );
                """);

            migrationBuilder.DropTable(
                name: "RecurringSpecials");

            migrationBuilder.DeleteData(
                table: "MenuItemPriceVariants",
                keyColumn: "MenuItemPriceVariantId",
                keyValue: new Guid("db1a72c1-6185-4f76-bf47-4a034a0daefe"));

            migrationBuilder.DeleteData(
                table: "MenuItemTabs",
                keyColumns: new[] { "MenuItemId", "Tab" },
                keyValues: new object[] { new Guid("9e7f7a6b-c8db-4e8d-b2ef-a60a40e91f70"), 3 });

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("9e7f7a6b-c8db-4e8d-b2ef-a60a40e91f70"));

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "MenuItemId", "Description", "ImagePath", "IsArchived", "IsSeasonal", "IsVisibleToGuests", "MenuSectionId", "Name", "OfferEndsOn", "OfferStartsOn", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("33d64e7b-d5b7-481a-97fc-7f250a68c27e"), "A dependable burger-night draw with fries and easy weeknight pricing.", "images/menu/burgers.svg", false, false, true, new Guid("198ccf8a-72fd-4278-a360-f36d5871e58b"), "Monday Night Burgers", null, null, 1 },
                    { new Guid("5ee7bbea-c2f4-4d5b-bcdb-bd0fd0a06704"), "A taco-night feature built for quick dinner traffic and casual bar seating.", "images/menu/appetizers.svg", false, false, true, new Guid("d67bd219-6d64-4a08-8ce4-d036a0c7b16d"), "Tuesday Taco Basket", null, null, 2 },
                    { new Guid("6baa63b3-55c9-4e47-8555-803573b9b38d"), "A hearty end-of-week dinner special that should read as a repeatable tradition.", null, false, false, true, new Guid("0e4de526-5921-4c3b-8985-d83344642a41"), "Sunday Pork Chop Dinner", null, null, 5 },
                    { new Guid("7e8222c3-63ec-4b4b-b777-d1e3aa7c5a86"), "Sauced wings with a strong shareable hook for midweek regulars.", "images/menu/wings.svg", false, false, true, new Guid("4a3a2d15-2af0-44a7-84c8-67b603a3ddb4"), "Wing Night", null, null, 3 },
                    { new Guid("88bb945a-b7b4-4725-972b-60a042e524e9"), "A Friday dinner anchor that deserves a permanent home in the guest menu flow.", null, false, false, true, new Guid("7f644c28-9275-4df8-8e4b-482f47568cfb"), "Friday Fish Fry", null, null, 4 }
                });

            migrationBuilder.InsertData(
                table: "MenuItemPriceVariants",
                columns: new[] { "MenuItemPriceVariantId", "Amount", "Label", "MenuItemId", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("055a076b-b1df-4960-b91a-d8de425d0b7f"), 10m, "Regular", new Guid("5ee7bbea-c2f4-4d5b-bcdb-bd0fd0a06704"), 1 },
                    { new Guid("7fd7a845-6d1a-4a4d-90ce-4d6e588d5fb9"), 14m, "Regular", new Guid("88bb945a-b7b4-4725-972b-60a042e524e9"), 1 },
                    { new Guid("a4d75c75-58fa-48ee-87ec-68c93ac6f7c8"), 16m, "Regular", new Guid("7e8222c3-63ec-4b4b-b777-d1e3aa7c5a86"), 1 },
                    { new Guid("bee6db9d-89d6-41e4-9356-eeb370c1afd8"), 17m, "Regular", new Guid("6baa63b3-55c9-4e47-8555-803573b9b38d"), 1 },
                    { new Guid("d4033f9f-d33d-4a8b-9e4f-c0cc750bd5c5"), 11m, "Regular", new Guid("33d64e7b-d5b7-481a-97fc-7f250a68c27e"), 1 }
                });

            migrationBuilder.InsertData(
                table: "MenuItemSpecials",
                columns: new[] { "MenuItemId", "Callout", "ClosesNextDay", "DayOfWeek", "EndDate", "EndsAt", "ScheduleKind", "StartDate", "StartsAt" },
                values: new object[,]
                {
                    { new Guid("33d64e7b-d5b7-481a-97fc-7f250a68c27e"), "$11 basket special", false, 1, null, null, 1, new DateOnly(2026, 1, 1), new TimeOnly(17, 0, 0) },
                    { new Guid("5ee7bbea-c2f4-4d5b-bcdb-bd0fd0a06704"), "$10 dinner feature", false, 2, null, null, 1, new DateOnly(2026, 1, 1), new TimeOnly(16, 0, 0) },
                    { new Guid("6baa63b3-55c9-4e47-8555-803573b9b38d"), "$17 dinner plate", false, 0, null, null, 1, new DateOnly(2026, 1, 1), new TimeOnly(15, 0, 0) },
                    { new Guid("7e8222c3-63ec-4b4b-b777-d1e3aa7c5a86"), "$16 dozen special", false, 3, null, null, 1, new DateOnly(2026, 1, 1), new TimeOnly(17, 0, 0) },
                    { new Guid("88bb945a-b7b4-4725-972b-60a042e524e9"), "$15 dinner plate", false, 5, null, null, 1, new DateOnly(2026, 1, 1), new TimeOnly(16, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "MenuItemTabs",
                columns: new[] { "MenuItemId", "Tab" },
                values: new object[,]
                {
                    { new Guid("33d64e7b-d5b7-481a-97fc-7f250a68c27e"), 3 },
                    { new Guid("5ee7bbea-c2f4-4d5b-bcdb-bd0fd0a06704"), 3 },
                    { new Guid("6baa63b3-55c9-4e47-8555-803573b9b38d"), 3 },
                    { new Guid("7e8222c3-63ec-4b4b-b777-d1e3aa7c5a86"), 3 },
                    { new Guid("88bb945a-b7b4-4725-972b-60a042e524e9"), 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MenuItemSpecials");

            migrationBuilder.DeleteData(
                table: "MenuItemPriceVariants",
                keyColumn: "MenuItemPriceVariantId",
                keyValue: new Guid("055a076b-b1df-4960-b91a-d8de425d0b7f"));

            migrationBuilder.DeleteData(
                table: "MenuItemPriceVariants",
                keyColumn: "MenuItemPriceVariantId",
                keyValue: new Guid("7fd7a845-6d1a-4a4d-90ce-4d6e588d5fb9"));

            migrationBuilder.DeleteData(
                table: "MenuItemPriceVariants",
                keyColumn: "MenuItemPriceVariantId",
                keyValue: new Guid("a4d75c75-58fa-48ee-87ec-68c93ac6f7c8"));

            migrationBuilder.DeleteData(
                table: "MenuItemPriceVariants",
                keyColumn: "MenuItemPriceVariantId",
                keyValue: new Guid("bee6db9d-89d6-41e4-9356-eeb370c1afd8"));

            migrationBuilder.DeleteData(
                table: "MenuItemPriceVariants",
                keyColumn: "MenuItemPriceVariantId",
                keyValue: new Guid("d4033f9f-d33d-4a8b-9e4f-c0cc750bd5c5"));

            migrationBuilder.DeleteData(
                table: "MenuItemTabs",
                keyColumns: new[] { "MenuItemId", "Tab" },
                keyValues: new object[] { new Guid("33d64e7b-d5b7-481a-97fc-7f250a68c27e"), 3 });

            migrationBuilder.DeleteData(
                table: "MenuItemTabs",
                keyColumns: new[] { "MenuItemId", "Tab" },
                keyValues: new object[] { new Guid("5ee7bbea-c2f4-4d5b-bcdb-bd0fd0a06704"), 3 });

            migrationBuilder.DeleteData(
                table: "MenuItemTabs",
                keyColumns: new[] { "MenuItemId", "Tab" },
                keyValues: new object[] { new Guid("6baa63b3-55c9-4e47-8555-803573b9b38d"), 3 });

            migrationBuilder.DeleteData(
                table: "MenuItemTabs",
                keyColumns: new[] { "MenuItemId", "Tab" },
                keyValues: new object[] { new Guid("7e8222c3-63ec-4b4b-b777-d1e3aa7c5a86"), 3 });

            migrationBuilder.DeleteData(
                table: "MenuItemTabs",
                keyColumns: new[] { "MenuItemId", "Tab" },
                keyValues: new object[] { new Guid("88bb945a-b7b4-4725-972b-60a042e524e9"), 3 });

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("33d64e7b-d5b7-481a-97fc-7f250a68c27e"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("5ee7bbea-c2f4-4d5b-bcdb-bd0fd0a06704"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("6baa63b3-55c9-4e47-8555-803573b9b38d"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("7e8222c3-63ec-4b4b-b777-d1e3aa7c5a86"));

            migrationBuilder.DeleteData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("88bb945a-b7b4-4725-972b-60a042e524e9"));

            migrationBuilder.CreateTable(
                name: "RecurringSpecials",
                columns: table => new
                {
                    RecurringSpecialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinkedMenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MenuSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    IsVisibleToGuests = table.Column<bool>(type: "bit", nullable: false),
                    PriceNote = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Tab = table.Column<int>(type: "int", nullable: false),
                    TimeNote = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
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
                table: "MenuItems",
                columns: new[] { "MenuItemId", "Description", "ImagePath", "IsArchived", "IsSeasonal", "IsVisibleToGuests", "MenuSectionId", "Name", "OfferEndsOn", "OfferStartsOn", "SortOrder" },
                values: new object[] { new Guid("9e7f7a6b-c8db-4e8d-b2ef-a60a40e91f70"), "A hearty end-of-week dinner special that should read as a repeatable tradition.", null, false, false, false, new Guid("0e4de526-5921-4c3b-8985-d83344642a41"), "Sunday Pork Chop Dinner", null, null, 1 });

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

            migrationBuilder.InsertData(
                table: "MenuItemPriceVariants",
                columns: new[] { "MenuItemPriceVariantId", "Amount", "Label", "MenuItemId", "SortOrder" },
                values: new object[] { new Guid("db1a72c1-6185-4f76-bf47-4a034a0daefe"), 17m, "Regular", new Guid("9e7f7a6b-c8db-4e8d-b2ef-a60a40e91f70"), 1 });

            migrationBuilder.InsertData(
                table: "MenuItemTabs",
                columns: new[] { "MenuItemId", "Tab" },
                values: new object[] { new Guid("9e7f7a6b-c8db-4e8d-b2ef-a60a40e91f70"), 3 });

            migrationBuilder.InsertData(
                table: "RecurringSpecials",
                columns: new[] { "RecurringSpecialId", "DayOfWeek", "Description", "IsArchived", "IsVisibleToGuests", "LinkedMenuItemId", "MenuSectionId", "PriceNote", "SortOrder", "Tab", "TimeNote", "Title" },
                values: new object[] { new Guid("6baa63b3-55c9-4e47-8555-803573b9b38d"), 0, "A hearty end-of-week dinner special that should read as a repeatable tradition.", false, true, new Guid("9e7f7a6b-c8db-4e8d-b2ef-a60a40e91f70"), new Guid("0e4de526-5921-4c3b-8985-d83344642a41"), "$17 dinner plate", 5, 3, "After 3:00 PM", "Sunday Pork Chop Dinner" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringSpecials_LinkedMenuItemId",
                table: "RecurringSpecials",
                column: "LinkedMenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringSpecials_MenuSectionId",
                table: "RecurringSpecials",
                column: "MenuSectionId");
        }
    }
}
