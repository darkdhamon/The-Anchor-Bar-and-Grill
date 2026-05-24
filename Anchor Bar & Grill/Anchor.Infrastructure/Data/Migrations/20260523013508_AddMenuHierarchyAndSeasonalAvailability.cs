using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Anchor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuHierarchyAndSeasonalAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentSectionId",
                table: "MenuSections",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "StartDate",
                table: "MenuItemSpecials",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddColumn<int>(
                name: "SeasonEndDay",
                table: "MenuItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeasonEndMonth",
                table: "MenuItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeasonStartDay",
                table: "MenuItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeasonStartMonth",
                table: "MenuItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MenuItemSpecialDays",
                columns: table => new
                {
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemSpecialDays", x => new { x.MenuItemId, x.DayOfWeek });
                    table.ForeignKey(
                        name: "FK_MenuItemSpecialDays_MenuItemSpecials_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItemSpecials",
                        principalColumn: "MenuItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO MenuItemSpecialDays
                (
                    MenuItemId,
                    DayOfWeek
                )
                SELECT
                    special.MenuItemId,
                    special.DayOfWeek
                FROM MenuItemSpecials special
                WHERE special.DayOfWeek IS NOT NULL
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM MenuItemSpecialDays existing
                    WHERE existing.MenuItemId = special.MenuItemId
                      AND existing.DayOfWeek = special.DayOfWeek
                );

                UPDATE MenuItemSpecials
                SET StartDate = NULL
                WHERE ScheduleKind = 1
                  AND StartDate = '2026-01-01';
                """);

            migrationBuilder.DropColumn(
                name: "DayOfWeek",
                table: "MenuItemSpecials");

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("06f858a2-f226-4b2f-a912-a6330bbf4ec1"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("0d440a2b-06a3-47f9-b129-1544f2f391a8"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("1af4708e-e741-4621-95e3-6c8f24af2be6"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("1c4d4f34-5260-4f7d-abcb-1c6875b7ebf8"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("33d64e7b-d5b7-481a-97fc-7f250a68c27e"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("3b7745b6-66d4-4db7-8ee3-b018834f58f7"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("44472c07-5f31-482a-8506-8a3c11cf1f26"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("590cc0e4-8be8-48e8-97b8-908ea7a1fc9a"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("5b1c6127-f7f0-497a-88b9-537e9110176f"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("5c3a9530-0f24-4d62-883b-f01b0a4286c2"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("5ee7bbea-c2f4-4d5b-bcdb-bd0fd0a06704"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("6baa63b3-55c9-4e47-8555-803573b9b38d"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("6e97a8ee-16b1-4feb-b6e0-2ab4e56658a0"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("6f2a75a4-c1e2-458f-bde4-d825f987cc3d"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("73ea7283-893f-4d14-8081-39f63bd54d13"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("7626d0df-9f8a-4fe8-9062-3596165e148a"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("79663ef8-29ff-4d24-8b1c-cfa8dad8ba72"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("7e8222c3-63ec-4b4b-b777-d1e3aa7c5a86"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("88bb945a-b7b4-4725-972b-60a042e524e9"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("8c5bde4d-3fb2-4a02-8ab5-40d3e0b49387"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("8fcaa555-d618-4ad8-ae73-abf51854a329"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("90dce7e3-9cc6-4732-b7d2-f4d43056fbb8"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("95f39c20-e1ba-4fd2-992d-8d9e19600d64"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("b7ab3351-1b6b-45d0-b7b4-9782d79cfc65"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("c88652a0-c9f2-4a7d-b4ac-8ddbfc9ff4e5"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("ca5cd1b7-8c73-4b21-b3e4-8e98fea44ee9"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("db2a7b2f-d9e9-4433-80a3-baeb5e5b5728"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("e1fd2b7f-d7e0-47cc-9e3e-4bc3a30aa4b8"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("e75d8a92-f1d2-4d58-9cd0-9b7e80ce9d80"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("e9d5a6c9-9a4c-4e98-8c72-2ae28bfcba97"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("ecfc8bfa-6c51-4607-b7ff-fe9f59db8fbc"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuItems",
                keyColumn: "MenuItemId",
                keyValue: new Guid("ff4ee65c-89e7-49f7-9023-8579ccb8307b"),
                columns: new[] { "SeasonEndDay", "SeasonEndMonth", "SeasonStartDay", "SeasonStartMonth" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("0e4de526-5921-4c3b-8985-d83344642a41"),
                column: "ParentSectionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("198ccf8a-72fd-4278-a360-f36d5871e58b"),
                column: "ParentSectionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("2ea5e671-e8ac-4c8a-b3d9-4c136a32a71b"),
                column: "ParentSectionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("31e9cb99-5fca-4a4a-a04b-89b97c926a52"),
                column: "ParentSectionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("4a3a2d15-2af0-44a7-84c8-67b603a3ddb4"),
                column: "ParentSectionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("7f644c28-9275-4df8-8e4b-482f47568cfb"),
                column: "ParentSectionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("a8f0b603-e02d-49f5-873d-1bb6bfc16c0f"),
                column: "ParentSectionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("d67bd219-6d64-4a08-8ce4-d036a0c7b16d"),
                column: "ParentSectionId",
                value: null);

            migrationBuilder.UpdateData(
                table: "MenuSections",
                keyColumn: "MenuSectionId",
                keyValue: new Guid("fa5da0f9-7e81-4b9d-9e11-fa5b1f828c72"),
                column: "ParentSectionId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_MenuSections_ParentSectionId",
                table: "MenuSections",
                column: "ParentSectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuSections_MenuSections_ParentSectionId",
                table: "MenuSections",
                column: "ParentSectionId",
                principalTable: "MenuSections",
                principalColumn: "MenuSectionId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuSections_MenuSections_ParentSectionId",
                table: "MenuSections");

            migrationBuilder.DropIndex(
                name: "IX_MenuSections_ParentSectionId",
                table: "MenuSections");

            migrationBuilder.DropColumn(
                name: "ParentSectionId",
                table: "MenuSections");

            migrationBuilder.DropColumn(
                name: "SeasonEndDay",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "SeasonEndMonth",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "SeasonStartDay",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "SeasonStartMonth",
                table: "MenuItems");

            migrationBuilder.Sql(
                """
                UPDATE MenuItemSpecials
                SET StartDate = '2026-01-01'
                WHERE ScheduleKind = 1
                  AND StartDate IS NULL;
                """);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "StartDate",
                table: "MenuItemSpecials",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DayOfWeek",
                table: "MenuItemSpecials",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE special
                SET special.DayOfWeek = source.DayOfWeek
                FROM MenuItemSpecials special
                OUTER APPLY
                (
                    SELECT TOP 1 day.DayOfWeek
                    FROM MenuItemSpecialDays day
                    WHERE day.MenuItemId = special.MenuItemId
                    ORDER BY day.DayOfWeek
                ) source;
                """);

            migrationBuilder.DropTable(
                name: "MenuItemSpecialDays");
        }
    }
}
