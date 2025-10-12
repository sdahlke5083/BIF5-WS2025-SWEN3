using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Paperless.REST.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable pgcrypto for gen_random_uuid()
            migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS "pgcrypto";""");

            migrationBuilder.CreateTable(
                name: "documentLanguages",
                columns: table => new
                {
                    iSO639Code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_documentLanguages", x => x.iSO639Code);
                });

            migrationBuilder.CreateTable(
                name: "fileTypes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    displayName = table.Column<string>(type: "text", nullable: false),
                    mimeType = table.Column<string>(type: "text", nullable: false),
                    fileExtension = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_fileTypes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "summaryPresets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    prompt = table.Column<string>(type: "text", nullable: false),
                    maxTokens = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_summaryPresets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    displayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password = table.Column<string>(type: "text", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workspaceRoles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_workspaceRoles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workspaces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_workspaces", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "userRoles",
                columns: table => new
                {
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    roleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_userRoles", x => new { x.userId, x.roleId });
                    table.ForeignKey(
                        name: "fK_userRoles_roles_roleId",
                        column: x => x.roleId,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fK_userRoles_users_userId",
                        column: x => x.userId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    currentMetadataVersion = table.Column<int>(type: "integer", nullable: false),
                    currentFileVersion = table.Column<int>(type: "integer", nullable: false),
                    deletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    workspaceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_documents", x => x.id);
                    table.ForeignKey(
                        name: "fK_documents_users_deletedByUserId",
                        column: x => x.deletedByUserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fK_documents_workspaces_workspaceId",
                        column: x => x.workspaceId,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "workspaceMembers",
                columns: table => new
                {
                    workspaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    userId = table.Column<Guid>(type: "uuid", nullable: false),
                    workspaceRoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_workspaceMembers", x => new { x.workspaceId, x.userId });
                    table.ForeignKey(
                        name: "fK_workspaceMembers_users_userId",
                        column: x => x.userId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fK_workspaceMembers_workspaceRoles_workspaceRoleId",
                        column: x => x.workspaceRoleId,
                        principalTable: "workspaceRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fK_workspaceMembers_workspaces_workspaceId",
                        column: x => x.workspaceId,
                        principalTable: "workspaces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "documentMetadatas",
                columns: table => new
                {
                    documentId = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    createdByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    languageCode = table.Column<string>(type: "text", nullable: true),
                    documentLanguageISO639Code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_documentMetadatas", x => new { x.documentId, x.version });
                    table.ForeignKey(
                        name: "fK_documentMetadatas_documentLanguages_documentLanguageISO639C~",
                        column: x => x.documentLanguageISO639Code,
                        principalTable: "documentLanguages",
                        principalColumn: "iSO639Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fK_documentMetadatas_documents_documentId",
                        column: x => x.documentId,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fK_documentMetadatas_users_createdByUserId",
                        column: x => x.createdByUserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "documentSummaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    documentId = table.Column<Guid>(type: "uuid", nullable: false),
                    createdAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    model = table.Column<string>(type: "text", nullable: true),
                    lengthPresetId = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_documentSummaries", x => x.id);
                    table.ForeignKey(
                        name: "fK_documentSummaries_documents_documentId",
                        column: x => x.documentId,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fK_documentSummaries_summaryPresets_lengthPresetId",
                        column: x => x.lengthPresetId,
                        principalTable: "summaryPresets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documentTags",
                columns: table => new
                {
                    documentId = table.Column<Guid>(type: "uuid", nullable: false),
                    tagId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_documentTags", x => new { x.documentId, x.tagId });
                    table.ForeignKey(
                        name: "fK_documentTags_documents_documentId",
                        column: x => x.documentId,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fK_documentTags_tags_tagId",
                        column: x => x.tagId,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fileVersions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    documentId = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    originalFileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    storedName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    sizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    fileTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    uploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    uploadedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_fileVersions", x => x.id);
                    table.ForeignKey(
                        name: "fK_fileVersions_documents_documentId",
                        column: x => x.documentId,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fK_fileVersions_fileTypes_fileTypeId",
                        column: x => x.fileTypeId,
                        principalTable: "fileTypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fK_fileVersions_users_uploadedByUserId",
                        column: x => x.uploadedByUserId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "processingStatuses",
                columns: table => new
                {
                    documentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ocr = table.Column<int>(type: "integer", nullable: false),
                    summary = table.Column<int>(type: "integer", nullable: false),
                    index = table.Column<int>(type: "integer", nullable: false),
                    lastError = table.Column<string>(type: "text", nullable: true, defaultValueSql: "NULL"),
                    updatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pK_processingStatuses", x => x.documentId);
                    table.ForeignKey(
                        name: "fK_processingStatuses_documents_documentId",
                        column: x => x.documentId,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "iX_documentMetadatas_createdByUserId",
                table: "documentMetadatas",
                column: "createdByUserId");

            migrationBuilder.CreateIndex(
                name: "iX_documentMetadatas_documentId_createdAt",
                table: "documentMetadatas",
                columns: new[] { "documentId", "createdAt" });

            migrationBuilder.CreateIndex(
                name: "iX_documentMetadatas_documentLanguageISO639Code",
                table: "documentMetadatas",
                column: "documentLanguageISO639Code");

            migrationBuilder.CreateIndex(
                name: "iX_documentMetadatas_title",
                table: "documentMetadatas",
                column: "title");

            migrationBuilder.CreateIndex(
                name: "iX_documents_deletedAt",
                table: "documents",
                column: "deletedAt");

            migrationBuilder.CreateIndex(
                name: "iX_documents_deletedByUserId",
                table: "documents",
                column: "deletedByUserId");

            migrationBuilder.CreateIndex(
                name: "iX_documents_workspaceId",
                table: "documents",
                column: "workspaceId");

            migrationBuilder.CreateIndex(
                name: "iX_documentSummaries_documentId",
                table: "documentSummaries",
                column: "documentId");

            migrationBuilder.CreateIndex(
                name: "iX_documentSummaries_lengthPresetId",
                table: "documentSummaries",
                column: "lengthPresetId");

            migrationBuilder.CreateIndex(
                name: "iX_documentTags_tagId",
                table: "documentTags",
                column: "tagId");

            migrationBuilder.CreateIndex(
                name: "iX_fileVersions_documentId_version",
                table: "fileVersions",
                columns: new[] { "documentId", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "iX_fileVersions_fileTypeId",
                table: "fileVersions",
                column: "fileTypeId");

            migrationBuilder.CreateIndex(
                name: "iX_fileVersions_originalFileName_uploadedAt",
                table: "fileVersions",
                columns: new[] { "originalFileName", "uploadedAt" });

            migrationBuilder.CreateIndex(
                name: "iX_fileVersions_uploadedByUserId",
                table: "fileVersions",
                column: "uploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "iX_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "iX_tags_name",
                table: "tags",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "iX_userRoles_roleId",
                table: "userRoles",
                column: "roleId");

            migrationBuilder.CreateIndex(
                name: "iX_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "iX_workspaceMembers_userId",
                table: "workspaceMembers",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "iX_workspaceMembers_workspaceRoleId",
                table: "workspaceMembers",
                column: "workspaceRoleId");


            // Seed initial data
            migrationBuilder.InsertData(
                table: "documentLanguages",
                columns: new[] { "iSO639Code", "name" },
                values: new object[,]
                {
                    { "en", "English" },
                    { "es", "Spanish" },
                    { "fr", "French" },
                    { "de", "German" },
                    { "zh", "Chinese" },
                    { "ja", "Japanese" },
                    { "ru", "Russian" },
                    { "ar", "Arabic" },
                    { "pt", "Portuguese" },
                    { "hi", "Hindi" }
                });
            
            migrationBuilder.InsertData(
                table: "fileTypes",
                columns: new[] { "id", "displayName", "mimeType", "fileExtension" },
                values: new object[,]
                {
                    { Guid.NewGuid(), "PDF Document", "application/pdf", ".pdf" },
                    { Guid.NewGuid(), "Word Document", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx" },
                    { Guid.NewGuid(), "Text File", "text/plain", ".txt" },
                    { Guid.NewGuid(), "Rich Text Format", "application/rtf", ".rtf" },
                    { Guid.NewGuid(), "Markdown File", "text/markdown", ".md" },
                    { Guid.NewGuid(), "PNG Image", "image/png", ".png" },
                    { Guid.NewGuid(), "JPEG Image", "image/jpeg", ".jpg" },
                    { Guid.NewGuid(), "TIFF Image", "image/tiff", ".tiff" },
                    { Guid.NewGuid(), "HTML Document", "text/html", ".html" },
                    { Guid.NewGuid(), "Word Document (legacy)", "application/msword", ".doc" },
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { Guid.NewGuid(), "Admin" },
                    { Guid.NewGuid(), "User" },
                    { Guid.NewGuid(), "Guest" }
                });

            migrationBuilder.InsertData(
                table: "workspaceRoles",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { Guid.NewGuid(), "Owner" },
                    { Guid.NewGuid(), "Editor" },
                    { Guid.NewGuid(), "Viewer" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documentMetadatas");

            migrationBuilder.DropTable(
                name: "documentSummaries");

            migrationBuilder.DropTable(
                name: "documentTags");

            migrationBuilder.DropTable(
                name: "fileVersions");

            migrationBuilder.DropTable(
                name: "processingStatuses");

            migrationBuilder.DropTable(
                name: "userRoles");

            migrationBuilder.DropTable(
                name: "workspaceMembers");

            migrationBuilder.DropTable(
                name: "documentLanguages");

            migrationBuilder.DropTable(
                name: "summaryPresets");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "fileTypes");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "workspaceRoles");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "workspaces");
        }
    }
}
