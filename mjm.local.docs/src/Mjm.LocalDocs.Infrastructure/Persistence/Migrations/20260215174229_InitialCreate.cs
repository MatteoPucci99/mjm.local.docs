using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mjm.LocalDocs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiTokens",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 36, nullable: false),
                    Name = table.Column<string>(maxLength: 200, nullable: false),
                    TokenHash = table.Column<string>(maxLength: 64, nullable: false),
                    TokenPrefix = table.Column<string>(maxLength: 10, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(nullable: true),
                    IsRevoked = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 36, nullable: false),
                    Name = table.Column<string>(maxLength: 200, nullable: false),
                    Description = table.Column<string>(maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 36, nullable: false),
                    ProjectId = table.Column<string>(maxLength: 36, nullable: false),
                    FileName = table.Column<string>(maxLength: 500, nullable: false),
                    FileExtension = table.Column<string>(maxLength: 20, nullable: false),
                    FileContent = table.Column<byte[]>(nullable: true),
                    FileStorageLocation = table.Column<string>(maxLength: 1000, nullable: true),
                    FileSizeBytes = table.Column<long>(nullable: false),
                    ExtractedText = table.Column<string>(nullable: false),
                    ContentHash = table.Column<string>(maxLength: 64, nullable: true),
                    MetadataJson = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentChunks",
                columns: table => new
                {
                    Id = table.Column<string>(maxLength: 100, nullable: false),
                    DocumentId = table.Column<string>(maxLength: 36, nullable: false),
                    Content = table.Column<string>(nullable: false),
                    FileName = table.Column<string>(maxLength: 500, nullable: true),
                    ChunkIndex = table.Column<int>(nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentChunks_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_Name",
                table: "ApiTokens",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ApiTokens_TokenHash",
                table: "ApiTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_DocumentId",
                table: "DocumentChunks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_DocumentId_ChunkIndex",
                table: "DocumentChunks",
                columns: new[] { "DocumentId", "ChunkIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ContentHash",
                table: "Documents",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ProjectId",
                table: "Documents",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ProjectId_FileName",
                table: "Documents",
                columns: new[] { "ProjectId", "FileName" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Name",
                table: "Projects",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiTokens");

            migrationBuilder.DropTable(
                name: "DocumentChunks");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
