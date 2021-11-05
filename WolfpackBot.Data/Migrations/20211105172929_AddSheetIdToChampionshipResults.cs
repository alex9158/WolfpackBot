using Microsoft.EntityFrameworkCore.Migrations;

namespace WolfpackBot.Data.Migrations
{
    public partial class AddSheetIdToChampionshipResults : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExcelSheetEventMappingId",
                table: "ChampionshipResults",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExcelSheetId",
                table: "ChampionshipResults",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChampionshipResults_ExcelSheetId",
                table: "ChampionshipResults",
                column: "ExcelSheetId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChampionshipResults_ExcelSheetEventMapping_ExcelSheetId",
                table: "ChampionshipResults",
                column: "ExcelSheetId",
                principalTable: "ExcelSheetEventMapping",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChampionshipResults_ExcelSheetEventMapping_ExcelSheetId",
                table: "ChampionshipResults");

            migrationBuilder.DropIndex(
                name: "IX_ChampionshipResults_ExcelSheetId",
                table: "ChampionshipResults");

            migrationBuilder.DropColumn(
                name: "ExcelSheetEventMappingId",
                table: "ChampionshipResults");

            migrationBuilder.DropColumn(
                name: "ExcelSheetId",
                table: "ChampionshipResults");
        }
    }
}
