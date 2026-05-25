using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Anchor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHomepagePublicity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HomepagePublicity",
                columns: table => new
                {
                    HomepagePublicityId = table.Column<int>(type: "int", nullable: false),
                    DraftEyebrow = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DraftHeadline = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    DraftSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DraftUpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PublishedEyebrow = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PublishedHeadline = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PublishedSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PublishedUpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomepagePublicity", x => x.HomepagePublicityId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomepagePublicity");
        }
    }
}
