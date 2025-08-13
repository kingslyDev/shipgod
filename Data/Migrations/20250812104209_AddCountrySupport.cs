using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentFinishGood.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCountrySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Countries",
                table: "UploadSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "UploadSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentSessionId",
                table: "UploadSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "UploadSessionDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "POMasters",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadSessions_ParentSessionId",
                table: "UploadSessions",
                column: "ParentSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_UploadSessions_UploadSessions_ParentSessionId",
                table: "UploadSessions",
                column: "ParentSessionId",
                principalTable: "UploadSessions",
                principalColumn: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UploadSessions_UploadSessions_ParentSessionId",
                table: "UploadSessions");

            migrationBuilder.DropIndex(
                name: "IX_UploadSessions_ParentSessionId",
                table: "UploadSessions");

            migrationBuilder.DropColumn(
                name: "Countries",
                table: "UploadSessions");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "UploadSessions");

            migrationBuilder.DropColumn(
                name: "ParentSessionId",
                table: "UploadSessions");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "UploadSessionDetails");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "POMasters");
        }
    }
}
