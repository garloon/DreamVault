using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamVault.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShelterIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShelterId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ShelterId",
                table: "Users",
                column: "ShelterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Shelters_ShelterId",
                table: "Users",
                column: "ShelterId",
                principalTable: "Shelters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Shelters_ShelterId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ShelterId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ShelterId",
                table: "Users");
        }
    }
}
