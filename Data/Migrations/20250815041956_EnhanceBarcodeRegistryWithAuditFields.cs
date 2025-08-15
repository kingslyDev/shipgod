using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentFinishGood.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceBarcodeRegistryWithAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BarcodeRegistries_BarcodeValue",
                table: "BarcodeRegistries");

            migrationBuilder.DropIndex(
                name: "IX_BarcodeRegistries_SessionId",
                table: "BarcodeRegistries");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "BarcodeRegistries",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "BoxNumber",
                table: "BarcodeRegistries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneratedBy",
                table: "BarcodeRegistries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "BarcodeRegistries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModelProduct",
                table: "BarcodeRegistries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "POId",
                table: "BarcodeRegistries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScannedBy",
                table: "BarcodeRegistries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScannedDate",
                table: "BarcodeRegistries",
                type: "datetime2",
                nullable: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_BarcodeRegistries_POMasters_POId",
                table: "BarcodeRegistries",
                column: "POId",
                principalTable: "POMasters",
                principalColumn: "POId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BarcodeRegistries_POMasters_POId",
                table: "BarcodeRegistries");

            migrationBuilder.DropIndex(
                name: "IX_BarcodeRegistries_BarcodeValue",
                table: "BarcodeRegistries");

            migrationBuilder.DropIndex(
                name: "IX_BarcodeRegistries_POId_BoxNumber",
                table: "BarcodeRegistries");

            migrationBuilder.DropIndex(
                name: "IX_BarcodeRegistries_SessionId_Status_IsActive",
                table: "BarcodeRegistries");

            migrationBuilder.DropColumn(
                name: "BoxNumber",
                table: "BarcodeRegistries");

            migrationBuilder.DropColumn(
                name: "GeneratedBy",
                table: "BarcodeRegistries");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "BarcodeRegistries");

            migrationBuilder.DropColumn(
                name: "ModelProduct",
                table: "BarcodeRegistries");

            migrationBuilder.DropColumn(
                name: "POId",
                table: "BarcodeRegistries");

            migrationBuilder.DropColumn(
                name: "ScannedBy",
                table: "BarcodeRegistries");

            migrationBuilder.DropColumn(
                name: "ScannedDate",
                table: "BarcodeRegistries");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "BarcodeRegistries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeRegistries_BarcodeValue",
                table: "BarcodeRegistries",
                column: "BarcodeValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BarcodeRegistries_SessionId",
                table: "BarcodeRegistries",
                column: "SessionId");
        }
    }
}
