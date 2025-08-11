using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentFinishGood.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModelConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PcsPerPallet = table.Column<int>(type: "int", nullable: false),
                    PcsPerBox = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "POs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NoPO = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ModelCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MixedPO = table.Column<bool>(type: "bit", nullable: false),
                    DisplayNoPO = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    GroupSignature = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BarcodeUnits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentGroupId = table.Column<int>(type: "int", nullable: false),
                    BarcodeType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SequenceNo = table.Column<int>(type: "int", nullable: false),
                    Qty = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsScanned = table.Column<bool>(type: "bit", nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScannedBy = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BarcodeUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BarcodeUnits_ShipmentGroups_ShipmentGroupId",
                        column: x => x.ShipmentGroupId,
                        principalTable: "ShipmentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupBreakdowns",
                columns: table => new
                {
                    ShipmentGroupId = table.Column<int>(type: "int", nullable: false),
                    QtyTotal = table.Column<int>(type: "int", nullable: false),
                    QtyPallet = table.Column<int>(type: "int", nullable: false),
                    QtyBox = table.Column<int>(type: "int", nullable: false),
                    QtyPcs = table.Column<int>(type: "int", nullable: false),
                    PcsPerPallet = table.Column<int>(type: "int", nullable: false),
                    PcsPerBox = table.Column<int>(type: "int", nullable: false),
                    IntegrityOk = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupBreakdowns", x => x.ShipmentGroupId);
                    table.ForeignKey(
                        name: "FK_GroupBreakdowns_ShipmentGroups_ShipmentGroupId",
                        column: x => x.ShipmentGroupId,
                        principalTable: "ShipmentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentGroupPOs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShipmentGroupId = table.Column<int>(type: "int", nullable: false),
                    POId = table.Column<int>(type: "int", nullable: false),
                    QtyContribution = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentGroupPOs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentGroupPOs_POs_POId",
                        column: x => x.POId,
                        principalTable: "POs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentGroupPOs_ShipmentGroups_ShipmentGroupId",
                        column: x => x.ShipmentGroupId,
                        principalTable: "ShipmentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeUnits_Code",
                table: "BarcodeUnits",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeUnits_ShipmentGroupId",
                table: "BarcodeUnits",
                column: "ShipmentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelConfigs_ModelCode_Method_IsActive",
                table: "ModelConfigs",
                columns: new[] { "ModelCode", "Method", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentGroupPOs_POId",
                table: "ShipmentGroupPOs",
                column: "POId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentGroupPOs_ShipmentGroupId",
                table: "ShipmentGroupPOs",
                column: "ShipmentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentGroups_GroupSignature",
                table: "ShipmentGroups",
                column: "GroupSignature",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BarcodeUnits");

            migrationBuilder.DropTable(
                name: "GroupBreakdowns");

            migrationBuilder.DropTable(
                name: "ModelConfigs");

            migrationBuilder.DropTable(
                name: "ShipmentGroupPOs");

            migrationBuilder.DropTable(
                name: "POs");

            migrationBuilder.DropTable(
                name: "ShipmentGroups");
        }
    }
}
