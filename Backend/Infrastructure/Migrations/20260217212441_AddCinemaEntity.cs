using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCinemaEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create Cinemas table first
            migrationBuilder.CreateTable(
                name: "Cinemas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cinemas", x => x.Id);
                });

            // 2. Seed the default cinema so existing halls can reference it
            var defaultCinemaId = new Guid("10000000-0000-0000-0000-000000000001");
            migrationBuilder.InsertData(
                table: "Cinemas",
                columns: new[] { "Id", "Name", "Address", "City", "Country", "PhoneNumber", "Email", "LogoUrl", "OpenTime", "CloseTime", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { defaultCinemaId, "Default Cinema", "123 Main Street", "Cityville", "Countryland", "+1-000-000-0000", "info@defaultcinema.local", null, new TimeOnly(9, 0), new TimeOnly(23, 0), true, DateTime.UtcNow, DateTime.UtcNow });

            // 3. Add CinemaId to CinemaHalls defaulting to the default cinema
            migrationBuilder.AddColumn<Guid>(
                name: "CinemaId",
                table: "CinemaHalls",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: defaultCinemaId);

            // 4. Update any existing halls that still have Guid.Empty to the default cinema
            migrationBuilder.Sql($"UPDATE CinemaHalls SET CinemaId = '{defaultCinemaId}' WHERE CinemaId = '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.CreateIndex(
                name: "IX_CinemaHalls_CinemaId",
                table: "CinemaHalls",
                column: "CinemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Cinemas_IsActive",
                table: "Cinemas",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Cinemas_Name_City",
                table: "Cinemas",
                columns: new[] { "Name", "City" });

            migrationBuilder.AddForeignKey(
                name: "FK_CinemaHalls_Cinemas_CinemaId",
                table: "CinemaHalls",
                column: "CinemaId",
                principalTable: "Cinemas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CinemaHalls_Cinemas_CinemaId",
                table: "CinemaHalls");

            migrationBuilder.DropTable(
                name: "Cinemas");

            migrationBuilder.DropIndex(
                name: "IX_CinemaHalls_CinemaId",
                table: "CinemaHalls");

            migrationBuilder.DropColumn(
                name: "CinemaId",
                table: "CinemaHalls");
        }
    }
}
