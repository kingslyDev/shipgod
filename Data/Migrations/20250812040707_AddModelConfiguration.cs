using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShipmentFinishGood.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModelConfiguration : Migration
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModelConfigurations");
        }
    }
}
