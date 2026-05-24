using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Anchor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuSectionVisibilityAndMultiSectionAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UsesSectionVisibility",
                table: "MenuItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MenuItemSectionAssignments",
                columns: table => new
                {
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemSectionAssignments", x => new { x.MenuItemId, x.MenuSectionId });
                    table.ForeignKey(
                        name: "FK_MenuItemSectionAssignments_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "MenuItemId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MenuItemSectionAssignments_MenuSections_MenuSectionId",
                        column: x => x.MenuSectionId,
                        principalTable: "MenuSections",
                        principalColumn: "MenuSectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MenuSectionTabs",
                columns: table => new
                {
                    MenuSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tab = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuSectionTabs", x => new { x.MenuSectionId, x.Tab });
                    table.ForeignKey(
                        name: "FK_MenuSectionTabs_MenuSections_MenuSectionId",
                        column: x => x.MenuSectionId,
                        principalTable: "MenuSections",
                        principalColumn: "MenuSectionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemSectionAssignments_MenuSectionId",
                table: "MenuItemSectionAssignments",
                column: "MenuSectionId");

            migrationBuilder.Sql(
                """
                INSERT INTO MenuItemSectionAssignments
                (
                    MenuItemId,
                    MenuSectionId,
                    SortOrder
                )
                SELECT
                    item.MenuItemId,
                    item.MenuSectionId,
                    CASE
                        WHEN item.SortOrder > 0 THEN item.SortOrder
                        ELSE 1
                    END
                FROM MenuItems item
                WHERE item.MenuSectionId <> '00000000-0000-0000-0000-000000000000'
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM MenuItemSectionAssignments existing
                    WHERE existing.MenuItemId = item.MenuItemId
                      AND existing.MenuSectionId = item.MenuSectionId
                );

                INSERT INTO MenuSectionTabs
                (
                    MenuSectionId,
                    Tab
                )
                SELECT DISTINCT
                    item.MenuSectionId,
                    itemTab.Tab
                FROM MenuItems item
                INNER JOIN MenuItemTabs itemTab ON itemTab.MenuItemId = item.MenuItemId
                WHERE item.MenuSectionId <> '00000000-0000-0000-0000-000000000000'
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM MenuSectionTabs existing
                    WHERE existing.MenuSectionId = item.MenuSectionId
                      AND existing.Tab = itemTab.Tab
                );

                INSERT INTO MenuSectionTabs
                (
                    MenuSectionId,
                    Tab
                )
                SELECT
                    section.MenuSectionId,
                    4
                FROM MenuSections section
                WHERE section.Family = 2
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM MenuSectionTabs existing
                    WHERE existing.MenuSectionId = section.MenuSectionId
                );

                INSERT INTO MenuSectionTabs
                (
                    MenuSectionId,
                    Tab
                )
                SELECT
                    section.MenuSectionId,
                    CASE
                        WHEN UPPER(section.Name) LIKE '%BREAKFAST%' THEN 1
                        WHEN UPPER(section.Name) LIKE '%LUNCH%' AND UPPER(section.Name) NOT LIKE '%DINNER%' THEN 2
                        WHEN UPPER(section.Name) LIKE '%DINNER%' AND UPPER(section.Name) NOT LIKE '%LUNCH%' THEN 3
                        ELSE 2
                    END
                FROM MenuSections section
                WHERE section.Family = 1
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM MenuSectionTabs existing
                    WHERE existing.MenuSectionId = section.MenuSectionId
                );

                INSERT INTO MenuSectionTabs
                (
                    MenuSectionId,
                    Tab
                )
                SELECT
                    section.MenuSectionId,
                    3
                FROM MenuSections section
                WHERE section.Family = 1
                AND EXISTS
                (
                    SELECT 1
                    FROM MenuSectionTabs existing
                    WHERE existing.MenuSectionId = section.MenuSectionId
                      AND existing.Tab = 2
                )
                AND UPPER(section.Name) NOT LIKE '%BREAKFAST%'
                AND UPPER(section.Name) NOT LIKE '%LUNCH%'
                AND UPPER(section.Name) NOT LIKE '%DINNER%'
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM MenuSectionTabs existing
                    WHERE existing.MenuSectionId = section.MenuSectionId
                      AND existing.Tab = 3
                );
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_MenuSections_MenuSectionId",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_MenuSectionId",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "MenuSectionId",
                table: "MenuItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MenuSectionId",
                table: "MenuItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                """
                UPDATE item
                SET MenuSectionId = COALESCE(source.MenuSectionId, fallback.MenuSectionId)
                FROM MenuItems item
                OUTER APPLY
                (
                    SELECT TOP 1 assignment.MenuSectionId
                    FROM MenuItemSectionAssignments assignment
                    WHERE assignment.MenuItemId = item.MenuItemId
                    ORDER BY assignment.SortOrder, assignment.MenuSectionId
                ) source
                CROSS APPLY
                (
                    SELECT TOP 1 section.MenuSectionId
                    FROM MenuSections section
                    ORDER BY section.SortOrder, section.MenuSectionId
                ) fallback;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuSectionId",
                table: "MenuItems",
                column: "MenuSectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_MenuSections_MenuSectionId",
                table: "MenuItems",
                column: "MenuSectionId",
                principalTable: "MenuSections",
                principalColumn: "MenuSectionId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropTable(
                name: "MenuItemSectionAssignments");

            migrationBuilder.DropTable(
                name: "MenuSectionTabs");

            migrationBuilder.DropColumn(
                name: "UsesSectionVisibility",
                table: "MenuItems");
        }
    }
}
