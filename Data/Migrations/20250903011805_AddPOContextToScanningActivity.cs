using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentFinishGood.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPOContextToScanningActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "POContext",
                table: "ScanningActivities",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "POContext",
                table: "ScanningActivities");
        }
    }
}
