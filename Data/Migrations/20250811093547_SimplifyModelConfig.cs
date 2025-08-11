using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentFinishGood.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyModelConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "ModelConfigs");

            migrationBuilder.DropColumn(
                name: "LoosePcsPerBox",
                table: "ModelConfigs");

            migrationBuilder.DropColumn(
                name: "LoosePcsPerPallet",
                table: "ModelConfigs");

            migrationBuilder.DropColumn(
                name: "PalletPcsPerBox",
                table: "ModelConfigs");

            migrationBuilder.DropColumn(
                name: "PalletPcsPerPallet",
                table: "ModelConfigs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "ModelConfigs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoosePcsPerBox",
                table: "ModelConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LoosePcsPerPallet",
                table: "ModelConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PalletPcsPerBox",
                table: "ModelConfigs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PalletPcsPerPallet",
                table: "ModelConfigs",
                type: "int",
                nullable: true);
        }
    }
}
