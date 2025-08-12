using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentFinishGood.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPOTablesAndBarcode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScanningActivities",
                columns: table => new
                {
                    ActivityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarcodeValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedArea = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanningActivities", x => x.ActivityId);
                });

            migrationBuilder.CreateTable(
                name: "UploadSessions",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SheetName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShipmentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdentityQRCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SheetIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MasterBarcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalBoxes = table.Column<int>(type: "int", nullable: false),
                    ShipmentDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadSessions", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "BarcodeRegistries",
                columns: table => new
                {
                    BarcodeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BarcodeValue = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BarcodeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    GeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarcodeRegistries", x => x.BarcodeId);
                    table.ForeignKey(
                        name: "FK_BarcodeRegistries_UploadSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "UploadSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "POMasters",
                columns: table => new
                {
                    POId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NoPO = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelProduk = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QtyTotal = table.Column<int>(type: "int", nullable: false),
                    QtyPallet = table.Column<int>(type: "int", nullable: false),
                    QtyBox = table.Column<int>(type: "int", nullable: false),
                    QtyPcs = table.Column<int>(type: "int", nullable: false),
                    Container = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NoInvoice = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShipmentDetail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceSessionId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShipmentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POMasters", x => x.POId);
                    table.ForeignKey(
                        name: "FK_POMasters_UploadSessions_SourceSessionId",
                        column: x => x.SourceSessionId,
                        principalTable: "UploadSessions",
                        principalColumn: "SessionId");
                });

            migrationBuilder.CreateTable(
                name: "UploadSessionDetails",
                columns: table => new
                {
                    DetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    OriginalPO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalQty = table.Column<int>(type: "int", nullable: false),
                    RowIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadSessionDetails", x => x.DetailId);
                    table.ForeignKey(
                        name: "FK_UploadSessionDetails_UploadSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "UploadSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PODetails",
                columns: table => new
                {
                    DetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    POId = table.Column<int>(type: "int", nullable: false),
                    UnitType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Qty = table.Column<int>(type: "int", nullable: false),
                    ScannedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScannedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PODetails", x => x.DetailId);
                    table.ForeignKey(
                        name: "FK_PODetails_POMasters_POId",
                        column: x => x.POId,
                        principalTable: "POMasters",
                        principalColumn: "POId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeRegistries_BarcodeValue",
                table: "BarcodeRegistries",
                column: "BarcodeValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeRegistries_SessionId",
                table: "BarcodeRegistries",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PODetails_POId",
                table: "PODetails",
                column: "POId");

            migrationBuilder.CreateIndex(
                name: "IX_POMasters_SourceSessionId",
                table: "POMasters",
                column: "SourceSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessionDetails_SessionId",
                table: "UploadSessionDetails",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BarcodeRegistries");

            migrationBuilder.DropTable(
                name: "PODetails");

            migrationBuilder.DropTable(
                name: "ScanningActivities");

            migrationBuilder.DropTable(
                name: "UploadSessionDetails");

            migrationBuilder.DropTable(
                name: "POMasters");

            migrationBuilder.DropTable(
                name: "UploadSessions");
        }
    }
}
