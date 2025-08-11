using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentFinishGood.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExcelUploadSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExcelUploadSessions",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalItems = table.Column<int>(type: "int", nullable: false),
                    ProcessedItems = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcelUploadSessions", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "ExcelUploadItems",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    NoPO = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModelProduk = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    QtyTotal = table.Column<int>(type: "int", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedPOId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcelUploadItems", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_ExcelUploadItems_ExcelUploadSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "ExcelUploadSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExcelUploadItems_SessionId",
                table: "ExcelUploadItems",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExcelUploadItems");

            migrationBuilder.DropTable(
                name: "ExcelUploadSessions");
        }
    }
}
