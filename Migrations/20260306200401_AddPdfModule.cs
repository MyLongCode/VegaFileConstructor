using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VegaFileConstructor.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PdfEditOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OriginalFilePath = table.Column<string>(type: "text", nullable: false),
                    OutputFilePath = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TotalRequestedReplacements = table.Column<int>(type: "integer", nullable: false),
                    TotalFoundOccurrences = table.Column<int>(type: "integer", nullable: false),
                    TotalAppliedReplacements = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PdfEditOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PdfEditReplacements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    OldValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NewValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FoundCount = table.Column<int>(type: "integer", nullable: false),
                    AppliedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PdfEditReplacements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PdfEditReplacements_PdfEditOperations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "PdfEditOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PdfEditOperations_UserId_CreatedAt",
                table: "PdfEditOperations",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PdfEditReplacements_OperationId_Order",
                table: "PdfEditReplacements",
                columns: new[] { "OperationId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PdfEditReplacements");

            migrationBuilder.DropTable(
                name: "PdfEditOperations");
        }
    }
}
