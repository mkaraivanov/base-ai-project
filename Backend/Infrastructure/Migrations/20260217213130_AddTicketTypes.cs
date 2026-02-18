using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PriceModifier = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookingTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TicketTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingTickets_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BookingTickets_TicketTypes_TicketTypeId",
                        column: x => x.TicketTypeId,
                        principalTable: "TicketTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReservationTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TicketTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeatPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationTickets_TicketTypes_TicketTypeId",
                        column: x => x.TicketTypeId,
                        principalTable: "TicketTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "TicketTypes",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Name", "PriceModifier", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Standard adult ticket", true, "Adult", 1.0m, 1 },
                    { new Guid("b2c3d4e5-f6a7-8901-bcde-f12345678901"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Children ticket (up to 12 years) — 50% discount", true, "Children", 0.5m, 2 },
                    { new Guid("c3d4e5f6-a7b8-9012-cdef-123456789012"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Senior ticket (65+) — 25% discount", true, "Senior", 0.75m, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingTickets_BookingId",
                table: "BookingTickets",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BookingTickets_TicketTypeId",
                table: "BookingTickets",
                column: "TicketTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationTickets_ReservationId",
                table: "ReservationTickets",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationTickets_TicketTypeId",
                table: "ReservationTickets",
                column: "TicketTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTypes_IsActive",
                table: "TicketTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTypes_SortOrder",
                table: "TicketTypes",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingTickets");

            migrationBuilder.DropTable(
                name: "ReservationTickets");

            migrationBuilder.DropTable(
                name: "TicketTypes");
        }
    }
}
