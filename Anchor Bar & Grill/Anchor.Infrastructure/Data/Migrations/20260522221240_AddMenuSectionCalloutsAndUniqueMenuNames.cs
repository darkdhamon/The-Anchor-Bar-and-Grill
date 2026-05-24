using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Anchor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuSectionCalloutsAndUniqueMenuNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Callout",
                table: "MenuSections",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "MenuSections",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "MenuItems",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("06f858a2-f226-4b2f-a912-a6330bbf4ec1"),
                column: "NormalizedName",
                value: "CHICKEN STRIPS");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("0d440a2b-06a3-47f9-b129-1544f2f391a8"),
                column: "NormalizedName",
                value: "CHICKEN WRAP");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("1af4708e-e741-4621-95e3-6c8f24af2be6"),
                column: "NormalizedName",
                value: "RANCH MELT");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("1c4d4f34-5260-4f7d-abcb-1c6875b7ebf8"),
                column: "NormalizedName",
                value: "TRADITIONAL OR BONELESS (12)");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("33d64e7b-d5b7-481a-97fc-7f250a68c27e"),
                column: "NormalizedName",
                value: "MONDAY NIGHT BURGERS");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("3b7745b6-66d4-4db7-8ee3-b018834f58f7"),
                column: "NormalizedName",
                value: "STEAK SANDWICH");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("44472c07-5f31-482a-8506-8a3c11cf1f26"),
                column: "NormalizedName",
                value: "MINI DONUTS");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("590cc0e4-8be8-48e8-97b8-908ea7a1fc9a"),
                column: "NormalizedName",
                value: "GRILLED CHICKEN SANDWICH");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("5b1c6127-f7f0-497a-88b9-537e9110176f"),
                column: "NormalizedName",
                value: "MINI CORN DOGS");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("5c3a9530-0f24-4d62-883b-f01b0a4286c2"),
                column: "NormalizedName",
                value: "MINI TACOS");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("5ee7bbea-c2f4-4d5b-bcdb-bd0fd0a06704"),
                column: "NormalizedName",
                value: "TUESDAY TACO BASKET");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("6baa63b3-55c9-4e47-8555-803573b9b38d"),
                column: "NormalizedName",
                value: "SUNDAY PORK CHOP DINNER");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("6e97a8ee-16b1-4feb-b6e0-2ab4e56658a0"),
                column: "NormalizedName",
                value: "WESTERN BURGER");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("6f2a75a4-c1e2-458f-bde4-d825f987cc3d"),
                column: "NormalizedName",
                value: "BUFFALO CHICKEN WRAP");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("73ea7283-893f-4d14-8081-39f63bd54d13"),
                column: "NormalizedName",
                value: "THE ANCHOR SALAD");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("7626d0df-9f8a-4fe8-9062-3596165e148a"),
                column: "NormalizedName",
                value: "CLASSIC HAMBURGER");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("79663ef8-29ff-4d24-8b1c-cfa8dad8ba72"),
                column: "NormalizedName",
                value: "TRADITIONAL OR BONELESS (6)");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("7e8222c3-63ec-4b4b-b777-d1e3aa7c5a86"),
                column: "NormalizedName",
                value: "WING NIGHT");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("88bb945a-b7b4-4725-972b-60a042e524e9"),
                column: "NormalizedName",
                value: "FRIDAY FISH FRY");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("8c5bde4d-3fb2-4a02-8ab5-40d3e0b49387"),
                column: "NormalizedName",
                value: "SMOKED SALMON SALAD");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("8fcaa555-d618-4ad8-ae73-abf51854a329"),
                column: "NormalizedName",
                value: "CHOCOLATE LAVA CAKE");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("90dce7e3-9cc6-4732-b7d2-f4d43056fbb8"),
                column: "NormalizedName",
                value: "SUNRISE BURGER");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("95f39c20-e1ba-4fd2-992d-8d9e19600d64"),
                column: "NormalizedName",
                value: "STEAK WRAP");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("b7ab3351-1b6b-45d0-b7b4-9782d79cfc65"),
                column: "NormalizedName",
                value: "MAC & CHEESE");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("c88652a0-c9f2-4a7d-b4ac-8ddbfc9ff4e5"),
                column: "NormalizedName",
                value: "CHEESE CURDS");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("ca5cd1b7-8c73-4b21-b3e4-8e98fea44ee9"),
                column: "NormalizedName",
                value: "ADD FRIES");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("db2a7b2f-d9e9-4433-80a3-baeb5e5b5728"),
                column: "NormalizedName",
                value: "SEASONAL SOUP");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("e1fd2b7f-d7e0-47cc-9e3e-4bc3a30aa4b8"),
                column: "NormalizedName",
                value: "WALLEYE SANDWICH");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("e75d8a92-f1d2-4d58-9cd0-9b7e80ce9d80"),
                column: "NormalizedName",
                value: "FISH TACOS");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("e9d5a6c9-9a4c-4e98-8c72-2ae28bfcba97"),
                column: "NormalizedName",
                value: "BLT SALAD");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("ecfc8bfa-6c51-4607-b7ff-fe9f59db8fbc"),
                column: "NormalizedName",
                value: "BACON CHEESEBURGER");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("ff4ee65c-89e7-49f7-9023-8579ccb8307b"),
                column: "NormalizedName",
                value: "QUESADILLAS");

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("0e4de526-5921-4c3b-8985-d83344642a41"),
                columns: new[] { "Callout", "NormalizedName" },
                values: new object[] { null, "DINNER SPECIALS" });

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("198ccf8a-72fd-4278-a360-f36d5871e58b"),
                columns: new[] { "Callout", "NormalizedName" },
                values: new object[] { null, "BURGERS" });

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("2ea5e671-e8ac-4c8a-b3d9-4c136a32a71b"),
                columns: new[] { "Callout", "NormalizedName" },
                values: new object[] { null, "KIDS MENU" });

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("31e9cb99-5fca-4a4a-a04b-89b97c926a52"),
                columns: new[] { "Callout", "NormalizedName" },
                values: new object[] { null, "SOUPS & SALADS" });

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("4a3a2d15-2af0-44a7-84c8-67b603a3ddb4"),
                columns: new[] { "Callout", "NormalizedName" },
                values: new object[] { null, "WINGS" });

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("7f644c28-9275-4df8-8e4b-482f47568cfb"),
                columns: new[] { "Callout", "NormalizedName" },
                values: new object[] { null, "SANDWICHES" });

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("a8f0b603-e02d-49f5-873d-1bb6bfc16c0f"),
                columns: new[] { "Callout", "NormalizedName" },
                values: new object[] { null, "DESSERTS" });

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("d67bd219-6d64-4a08-8ce4-d036a0c7b16d"),
                columns: new[] { "Callout", "NormalizedName" },
                values: new object[] { null, "APPETIZERS" });

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("fa5da0f9-7e81-4b9d-9e11-fa5b1f828c72"),
                columns: new[] { "Callout", "NormalizedName" },
                values: new object[] { null, "WRAPS" });

            migrationBuilder.Sql(
                """
                UPDATE MenuSections
                SET
                    Name = LTRIM(RTRIM(Name)),
                    Callout = NULLIF(LTRIM(RTRIM(Callout)), '');

                ;WITH RankedSections AS
                (
                    SELECT
                        MenuSectionId,
                        LTRIM(RTRIM(Name)) AS TrimmedName,
                        ROW_NUMBER() OVER (
                            PARTITION BY UPPER(LTRIM(RTRIM(Name)))
                            ORDER BY MenuSectionId) AS DuplicateRank
                    FROM MenuSections
                ),
                RenamedSections AS
                (
                    SELECT
                        MenuSectionId,
                        CASE
                            WHEN DuplicateRank = 1 THEN TrimmedName
                            WHEN DuplicateRank = 2 THEN CONCAT(TrimmedName, ' (Duplicate)')
                            ELSE CONCAT(TrimmedName, ' (Duplicate ', DuplicateRank - 1, ')')
                        END AS ResolvedName
                    FROM RankedSections
                )
                UPDATE sections
                SET
                    Name = renamed.ResolvedName,
                    NormalizedName = UPPER(renamed.ResolvedName)
                FROM MenuSections sections
                INNER JOIN RenamedSections renamed ON renamed.MenuSectionId = sections.MenuSectionId;

                UPDATE MenuItems
                SET Name = LTRIM(RTRIM(Name));

                ;WITH RankedItems AS
                (
                    SELECT
                        MenuItemId,
                        LTRIM(RTRIM(Name)) AS TrimmedName,
                        ROW_NUMBER() OVER (
                            PARTITION BY UPPER(LTRIM(RTRIM(Name)))
                            ORDER BY MenuItemId) AS DuplicateRank
                    FROM MenuItems
                ),
                RenamedItems AS
                (
                    SELECT
                        MenuItemId,
                        CASE
                            WHEN DuplicateRank = 1 THEN TrimmedName
                            WHEN DuplicateRank = 2 THEN CONCAT(TrimmedName, ' (Duplicate)')
                            ELSE CONCAT(TrimmedName, ' (Duplicate ', DuplicateRank - 1, ')')
                        END AS ResolvedName
                    FROM RankedItems
                )
                UPDATE items
                SET
                    Name = renamed.ResolvedName,
                    NormalizedName = UPPER(renamed.ResolvedName)
                FROM MenuItems items
                INNER JOIN RenamedItems renamed ON renamed.MenuItemId = items.MenuItemId;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MenuSections_NormalizedName",
                table: "MenuSections",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_NormalizedName",
                table: "MenuItems",
                column: "NormalizedName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MenuSections_NormalizedName",
                table: "MenuSections");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_NormalizedName",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "Callout",
                table: "MenuSections");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "MenuSections");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "MenuItems");
        }
    }
}
