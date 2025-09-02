using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentFinishGood.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPOLockTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "POLocks",
                columns: table => new
                {
                    POLockId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    POId = table.Column<int>(type: "int", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LockedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POLocks", x => x.POLockId);
                    table.ForeignKey(
                        name: "FK_POLocks_POMasters_POId",
                        column: x => x.POId,
                        principalTable: "POMasters",
                        principalColumn: "POId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_POLocks_UploadSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "UploadSessions",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_POLocks_POId",
                table: "POLocks",
                column: "POId");

            migrationBuilder.CreateIndex(
                name: "IX_POLocks_SessionId",
                table: "POLocks",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_POLocks_UserId_SessionId_IsActive",
                table: "POLocks",
                columns: new[] { "UserId", "SessionId", "IsActive" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "POLocks");
        }
    }
}
