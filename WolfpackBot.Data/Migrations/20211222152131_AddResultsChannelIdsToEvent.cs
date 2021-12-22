using Microsoft.EntityFrameworkCore.Migrations;

namespace WolfpackBot.Data.Migrations
{
    public partial class AddResultsChannelIdsToEvent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "StandingsChannelId",
                table: "Event",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StandingsChannelId",
                table: "Event");
        }
    }
}
