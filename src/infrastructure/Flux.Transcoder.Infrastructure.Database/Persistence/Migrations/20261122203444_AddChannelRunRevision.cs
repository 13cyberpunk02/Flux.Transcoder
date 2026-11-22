using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flux.Transcoder.Infrastructure.Database.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelRunRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "run_revision",
                table: "channels",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "run_revision",
                table: "channels");
        }
    }
}
