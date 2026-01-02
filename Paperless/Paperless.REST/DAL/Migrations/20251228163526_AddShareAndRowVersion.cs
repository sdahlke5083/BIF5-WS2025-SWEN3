using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paperless.REST.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddShareAndRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "rowVersion",
                table: "documents",
                type: "bytea",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "shares",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    documentId = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    passwordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    expiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_shares", x => x.id);
                    table.ForeignKey(
                        name: "fK_shares_documents_documentId",
                        column: x => x.documentId,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "iX_shares_documentId",
                table: "shares",
                column: "documentId");

            migrationBuilder.CreateIndex(
                name: "iX_shares_token",
                table: "shares",
                column: "token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shares");

            migrationBuilder.DropColumn(
                name: "rowVersion",
                table: "documents");
        }
    }
}
