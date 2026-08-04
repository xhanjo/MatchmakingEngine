using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchmakingEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchPlayerCaptain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCaptain",
                table: "MatchPlayers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCaptain",
                table: "MatchPlayers");
        }
    }
}
