using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Anchor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PromoBadge = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    StartsAt = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndsAt = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndsNextDay = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    PublicationState = table.Column<int>(type: "int", nullable: false),
                    RecurrencePattern = table.Column<int>(type: "int", nullable: false),
                    RecurrenceInterval = table.Column<int>(type: "int", nullable: false),
                    RecursOnDayOfWeek = table.Column<int>(type: "int", nullable: true),
                    RecursOnWeekOfMonth = table.Column<int>(type: "int", nullable: true),
                    RecursUntil = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_PublicationState_StartsOn",
                table: "Events",
                columns: new[] { "PublicationState", "StartsOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}
