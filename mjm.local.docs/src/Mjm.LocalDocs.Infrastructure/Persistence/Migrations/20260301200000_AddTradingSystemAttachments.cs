using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mjm.LocalDocs.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddTradingSystemAttachments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TradingSystemAttachments",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                TradingSystemId = table.Column<string>(type: "TEXT", maxLength: 36, nullable: false),
                FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                FileExtension = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: false),
                FileContent = table.Column<byte[]>(type: "BLOB", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TradingSystemAttachments", x => x.Id);
                table.ForeignKey(
                    name: "FK_TradingSystemAttachments_TradingSystems_TradingSystemId",
                    column: x => x.TradingSystemId,
                    principalTable: "TradingSystems",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TradingSystemAttachments_TradingSystemId",
            table: "TradingSystemAttachments",
            column: "TradingSystemId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TradingSystemAttachments");
    }
}
