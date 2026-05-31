using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Anchor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkSeededRecurringSpecialsToMenuItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "MenuItemId", "Description", "ImagePath", "IsArchived", "IsSeasonal", "IsVisibleToGuests", "MenuSectionId", "Name", "OfferEndsOn", "OfferStartsOn", "SortOrder" },
                values: new object[] { new Guid("9e7f7a6b-c8db-4e8d-b2ef-a60a40e91f70"), "A hearty end-of-week dinner special that should read as a repeatable tradition.", null, false, false, false, new Guid("0e4de526-5921-4c3b-8985-d83344642a41"), "Sunday Pork Chop Dinner", null, null, 1 });

            migrationBuilder.UpdateData(
                table: "RecurringSpecials",
                keyColumn: "RecurringSpecialId",
                keyValue: new Guid("6baa63b3-55c9-4e47-8555-803573b9b38d"),
                column: "LinkedMenuItemId",
                value: new Guid("9e7f7a6b-c8db-4e8d-b2ef-a60a40e91f70"));

            migrationBuilder.InsertData(
                table: "MenuItemPriceVariants",
                columns: new[] { "MenuItemPriceVariantId", "Amount", "Label", "MenuItemId", "SortOrder" },
                values: new object[] { new Guid("db1a72c1-6185-4f76-bf47-4a034a0daefe"), 17m, "Regular", new Guid("9e7f7a6b-c8db-4e8d-b2ef-a60a40e91f70"), 1 });

            migrationBuilder.InsertData(
                table: "MenuItemTabs",
                columns: new[] { "MenuItemId", "Tab" },
                values: new object[] { new Guid("9e7f7a6b-c8db-4e8d-b2ef-a60a40e91f70"), 3 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.UpdateData(
                table: "RecurringSpecials",
                keyColumn: "RecurringSpecialId",
                keyValue: new Guid("6baa63b3-55c9-4e47-8555-803573b9b38d"),
                column: "LinkedMenuItemId",
                value: null);
        }
    }
}
