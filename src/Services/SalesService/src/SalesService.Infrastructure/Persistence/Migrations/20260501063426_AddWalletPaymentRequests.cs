using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletPaymentRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParkingSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    StationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlotType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SlotNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingSlots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WalletPaymentRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    SaleTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "DECIMAL(12,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VehicleNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    FuelTypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    QuantityLitres = table.Column<decimal>(type: "DECIMAL(10,3)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletPaymentRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParkingBookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ParkingSlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SlotType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DurationHours = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Initiated"),
                    RazorpayOrderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RazorpayPaymentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BookedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingBookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingBookings_ParkingSlots_ParkingSlotId",
                        column: x => x.ParkingSlotId,
                        principalTable: "ParkingSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingBookings_CustomerId",
                table: "ParkingBookings",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingBookings_ParkingSlotId",
                table: "ParkingBookings",
                column: "ParkingSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingBookings_RazorpayOrderId",
                table: "ParkingBookings",
                column: "RazorpayOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSlots_StationId",
                table: "ParkingSlots",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletPaymentRequests_CustomerId",
                table: "WalletPaymentRequests",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletPaymentRequests_SaleTransactionId",
                table: "WalletPaymentRequests",
                column: "SaleTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingBookings");

            migrationBuilder.DropTable(
                name: "WalletPaymentRequests");

            migrationBuilder.DropTable(
                name: "ParkingSlots");
        }
    }
}
