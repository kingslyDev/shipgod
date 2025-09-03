using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShipmentFinishGood.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModelConfigurations",
                columns: table => new
                {
                    ConfigId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PcsPerPallet = table.Column<int>(type: "int", nullable: false),
                    PcsPerBox = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelConfigurations", x => x.ConfigId);
                });

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
                    ShipmentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FileHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Countries = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentSessionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadSessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_UploadSessions_UploadSessions_ParentSessionId",
                        column: x => x.ParentSessionId,
                        principalTable: "UploadSessions",
                        principalColumn: "SessionId");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
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
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                    RowIndex = table.Column<int>(type: "int", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
                name: "UserSessionLocks",
                columns: table => new
                {
                    LockId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    MasterQRCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessionLocks", x => x.LockId);
                    table.ForeignKey(
                        name: "FK_UserSessionLocks_UploadSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "UploadSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
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
                    POId = table.Column<int>(type: "int", nullable: true),
                    ModelProduct = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BoxNumber = table.Column<int>(type: "int", nullable: true),
                    GeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ScannedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScannedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarcodeRegistries", x => x.BarcodeId);
                    table.ForeignKey(
                        name: "FK_BarcodeRegistries_POMasters_POId",
                        column: x => x.POId,
                        principalTable: "POMasters",
                        principalColumn: "POId");
                    table.ForeignKey(
                        name: "FK_BarcodeRegistries_UploadSessions_SessionId",
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

            migrationBuilder.CreateTable(
                name: "POItemRegistries",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    POId = table.Column<int>(type: "int", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BarcodeValue = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemSequence = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScannedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ItemDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpectedQuantity = table.Column<int>(type: "int", nullable: true),
                    Container = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POItemRegistries", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_POItemRegistries_POMasters_POId",
                        column: x => x.POId,
                        principalTable: "POMasters",
                        principalColumn: "POId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPOLocks",
                columns: table => new
                {
                    POLockId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SessionLockId = table.Column<int>(type: "int", nullable: false),
                    POId = table.Column<int>(type: "int", nullable: false),
                    NoPO = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPOLocks", x => x.POLockId);
                    table.ForeignKey(
                        name: "FK_UserPOLocks_POMasters_POId",
                        column: x => x.POId,
                        principalTable: "POMasters",
                        principalColumn: "POId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPOLocks_UserSessionLocks_SessionLockId",
                        column: x => x.SessionLockId,
                        principalTable: "UserSessionLocks",
                        principalColumn: "LockId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ModelConfigurations",
                columns: new[] { "ConfigId", "Description", "ModelName", "PcsPerBox", "PcsPerPallet", "Type" },
                values: new object[,]
                {
                    { 1, "LOOSE (Pengiriman tanpa palet)", "RF-D10EB-K", 3, 192, "LOOSE" },
                    { 2, "LOOSE (Pengiriman tanpa palet)", "RF-D10EG-K", 3, 192, "LOOSE" },
                    { 3, "LOOSE (Pengiriman tanpa palet)", "RF-D10EG-W", 3, 192, "LOOSE" },
                    { 4, "LOOSE (Pengiriman tanpa palet)", "RF-D10GN-K", 3, 192, "LOOSE" },
                    { 5, "LOOSE (Pengiriman tanpa palet)", "R-2255-S", 3, 240, "LOOSE" },
                    { 6, "LOOSE (Pengiriman tanpa palet)", "RF-2450-S", 3, 240, "LOOSE" },
                    { 7, "LOOSE (Pengiriman tanpa palet)", "RF-2400DEB-K", 3, 240, "LOOSE" },
                    { 8, "LOOSE (Pengiriman tanpa palet)", "RF-2400DEE-K", 3, 240, "LOOSE" },
                    { 9, "LOOSE (Pengiriman tanpa palet)", "RF-2400DEG-K", 3, 240, "LOOSE" },
                    { 10, "LOOSE (Pengiriman tanpa palet)", "RF-2400DGN-S", 3, 240, "LOOSE" },
                    { 11, "LOOSE (Pengiriman tanpa palet)", "RF-2400DGT-S", 3, 240, "LOOSE" },
                    { 12, "LOOSE (Pengiriman tanpa palet)", "RF-2400DPC-S", 3, 240, "LOOSE" },
                    { 13, "LOOSE (Pengiriman tanpa palet)", "RF-2400DP-S", 3, 240, "LOOSE" },
                    { 14, "LOOSE (Pengiriman tanpa palet)", "RF-2400DP-K", 3, 240, "LOOSE" },
                    { 15, "LOOSE (Pengiriman tanpa palet)", "RF-2400DLJ-K", 3, 240, "LOOSE" },
                    { 16, "LOOSE (Pengiriman tanpa palet)", "RF-P155-S", 20, 800, "LOOSE" },
                    { 17, "LOOSE (Pengiriman tanpa palet)", "RF-P55-S", 20, 800, "LOOSE" },
                    { 18, "LOOSE (Pengiriman tanpa palet)", "RF-P150DEG-S", 20, 800, "LOOSE" },
                    { 19, "LOOSE (Pengiriman tanpa palet)", "RF-P150DGC-S", 20, 800, "LOOSE" },
                    { 20, "LOOSE (Pengiriman tanpa palet)", "RF-P150DGT-S", 20, 800, "LOOSE" },
                    { 21, "LOOSE (Pengiriman tanpa palet)", "RF-P150DBAGA", 20, 800, "LOOSE" },
                    { 22, "LOOSE (Pengiriman tanpa palet)", "RF-P50DGC-S", 20, 1000, "LOOSE" },
                    { 23, "LOOSE (Pengiriman tanpa palet)", "RF-P50DGC-R", 20, 1000, "LOOSE" },
                    { 24, "LOOSE (Pengiriman tanpa palet)", "RF-P50DEG-S", 20, 1000, "LOOSE" },
                    { 25, "LOOSE (Pengiriman tanpa palet)", "RF-P50DLJ-S", 20, 1000, "LOOSE" },
                    { 26, "LOOSE (Pengiriman tanpa palet)", "RF-P50DPR-S", 20, 1000, "LOOSE" },
                    { 27, "LOOSE (Pengiriman tanpa palet)", "RF-P50DPP-S", 20, 1000, "LOOSE" },
                    { 28, "LOOSE (Pengiriman tanpa palet)", "RF-562DDGC-K", 5, 350, "LOOSE" },
                    { 29, "LOOSE (Pengiriman tanpa palet)", "RF-U156-S", 6, 450, "LOOSE" },
                    { 30, "LOOSE (Pengiriman tanpa palet)", "RF-NA35R-S", 20, 1000, "LOOSE" },
                    { 31, "LOOSE (Pengiriman tanpa palet)", "RF-NA35R", 20, 1000, "LOOSE" },
                    { 32, "LOOSE (Pengiriman tanpa palet)", "RF-5270LJ-K", 20, 600, "LOOSE" },
                    { 33, "PALLET (Pengiriman dengan palet)", "RF-562DDGC-K", 5, 210, "PALLET" },
                    { 34, "PALLET (Pengiriman dengan palet)", "RF-P150DGC-S", 20, 1600, "PALLET" },
                    { 35, "PALLET (Pengiriman dengan palet)", "RF-P150DEG-S", 20, 1600, "PALLET" },
                    { 36, "PALLET (Pengiriman dengan palet)", "RF-P50DGC-R", 20, 1600, "PALLET" },
                    { 37, "PALLET (Pengiriman dengan palet)", "RF-P50DEG-S", 20, 1200, "PALLET" },
                    { 38, "PALLET (Pengiriman dengan palet)", "RF-D10EG-K", 3, 144, "PALLET" },
                    { 39, "PALLET (Pengiriman dengan palet)", "RF-D10GN-K", 3, 126, "PALLET" },
                    { 40, "PALLET (Pengiriman dengan palet)", "RF-2450-S", 3, 216, "PALLET" },
                    { 41, "PALLET (Pengiriman dengan palet)", "R-2255-S", 3, 216, "PALLET" },
                    { 42, "PALLET (Pengiriman dengan palet)", "RF-2400DPC-S", 3, 216, "PALLET" },
                    { 43, "PALLET (Pengiriman dengan palet)", "RF-2400DP-S", 3, 216, "PALLET" },
                    { 44, "PALLET (Pengiriman dengan palet)", "RF-2400DP-K", 3, 216, "PALLET" },
                    { 45, "PALLET (Pengiriman dengan palet)", "RF-2400DEE-K", 3, 216, "PALLET" },
                    { 46, "PALLET (Pengiriman dengan palet)", "RF-2400DEG-K", 3, 216, "PALLET" },
                    { 47, "PALLET (Pengiriman dengan palet)", "RF-2400DGN-S", 3, 216, "PALLET" },
                    { 48, "PALLET (Pengiriman dengan palet)", "RF-2400DEB-K", 3, 216, "PALLET" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeRegistries_BarcodeValue",
                table: "BarcodeRegistries",
                column: "BarcodeValue",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeRegistries_POId_BoxNumber",
                table: "BarcodeRegistries",
                columns: new[] { "POId", "BoxNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeRegistries_SessionId_Status_IsActive",
                table: "BarcodeRegistries",
                columns: new[] { "SessionId", "Status", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PODetails_POId",
                table: "PODetails",
                column: "POId");

            migrationBuilder.CreateIndex(
                name: "IX_POItemRegistries_BarcodeValue",
                table: "POItemRegistries",
                column: "BarcodeValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_POItemRegistries_POId_ItemType_Status",
                table: "POItemRegistries",
                columns: new[] { "POId", "ItemType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_POMasters_SourceSessionId",
                table: "POMasters",
                column: "SourceSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessionDetails_SessionId",
                table: "UploadSessionDetails",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_ParentSessionId",
                table: "UploadSessions",
                column: "ParentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPOLocks_POId",
                table: "UserPOLocks",
                column: "POId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPOLocks_SessionLockId",
                table: "UserPOLocks",
                column: "SessionLockId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPOLocks_UserId_IsActive",
                table: "UserPOLocks",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSessionLocks_SessionId",
                table: "UserSessionLocks",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessionLocks_UserId_IsActive",
                table: "UserSessionLocks",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BarcodeRegistries");

            migrationBuilder.DropTable(
                name: "ModelConfigurations");

            migrationBuilder.DropTable(
                name: "PODetails");

            migrationBuilder.DropTable(
                name: "POItemRegistries");

            migrationBuilder.DropTable(
                name: "ScanningActivities");

            migrationBuilder.DropTable(
                name: "UploadSessionDetails");

            migrationBuilder.DropTable(
                name: "UserPOLocks");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "POMasters");

            migrationBuilder.DropTable(
                name: "UserSessionLocks");

            migrationBuilder.DropTable(
                name: "UploadSessions");
        }
    }
}
