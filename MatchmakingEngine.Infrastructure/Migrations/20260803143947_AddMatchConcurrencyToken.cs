using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MatchmakingEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "AvailableMaps",
                table: "Matches",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<List<string>>(
                name: "BannedMaps",
                table: "Matches",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentVetoTurnPlayerId",
                table: "Matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VetoDeadLine",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableMaps",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "BannedMaps",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "CurrentVetoTurnPlayerId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "VetoDeadLine",
                table: "Matches");
        }
    }
}
