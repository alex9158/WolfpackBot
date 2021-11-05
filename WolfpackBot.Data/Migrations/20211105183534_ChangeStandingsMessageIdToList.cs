using Microsoft.EntityFrameworkCore.Migrations;

namespace WolfpackBot.Data.Migrations
{
    public partial class ChangeStandingsMessageIdToList : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StandingsMessageId",
                table: "Event");

            migrationBuilder.AddColumn<string>(
                name: "StandingsMessageIds",
                table: "Event",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StandingsMessageIds",
                table: "Event");

            migrationBuilder.AddColumn<ulong>(
                name: "StandingsMessageId",
                table: "Event",
                type: "INTEGER",
                nullable: true);
        }
    }
}
