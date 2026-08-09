using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchmakingEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedMapToMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedMap",
                table: "Matches",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedMap",
                table: "Matches");
        }
    }
}
