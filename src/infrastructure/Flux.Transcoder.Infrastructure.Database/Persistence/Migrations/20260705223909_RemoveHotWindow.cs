using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flux.Transcoder.Infrastructure.Database.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHotWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hot_window",
                table: "channels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "hot_window",
                table: "channels",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }
    }
}
