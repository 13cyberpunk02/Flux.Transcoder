using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flux.Transcoder.Infrastructure.Database.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RetationAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "archive_retention",
                table: "channels",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "hot_window",
                table: "channels",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "archive_retention",
                table: "channels");

            migrationBuilder.DropColumn(
                name: "hot_window",
                table: "channels");
        }
    }
}
