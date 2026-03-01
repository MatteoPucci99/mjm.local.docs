using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mjm.LocalDocs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddTradingSystems : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TradingSystems",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                SourceUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                Status = table.Column<int>(type: "INTEGER", nullable: false),
                ProjectId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                CodeDocumentId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: true),
                AttachmentDocumentIdsJson = table.Column<string>(type: "TEXT", nullable: true),
                TagsJson = table.Column<string>(type: "TEXT", nullable: true),
                Notes = table.Column<string>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TradingSystems", x => x.Id);
                table.ForeignKey(
                    name: "FK_TradingSystems_Documents_CodeDocumentId",
                    column: x => x.CodeDocumentId,
                    principalTable: "Documents",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_TradingSystems_Projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TradingSystems_Name",
            table: "TradingSystems",
            column: "Name");

        migrationBuilder.CreateIndex(
            name: "IX_TradingSystems_ProjectId",
            table: "TradingSystems",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_TradingSystems_Status",
            table: "TradingSystems",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_TradingSystems_CodeDocumentId",
            table: "TradingSystems",
            column: "CodeDocumentId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TradingSystems");
    }
}
