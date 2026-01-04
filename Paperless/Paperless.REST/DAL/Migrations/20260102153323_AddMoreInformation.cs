using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paperless.REST.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "mustChangePassword",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mustChangePassword",
                table: "users");
        }
    }
}
