using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DreamVault.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsOnlineToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "Users");
        }
    }
}
