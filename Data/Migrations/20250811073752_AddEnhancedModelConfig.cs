using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentFinishGood.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEnhancedModelConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
