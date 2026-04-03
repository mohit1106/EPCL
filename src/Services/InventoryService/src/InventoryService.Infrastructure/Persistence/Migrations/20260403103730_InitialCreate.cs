using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tanks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    StationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FuelTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TankSerialNumber = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: false),
                    CapacityLitres = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false),
                    CurrentStockLitres = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false),
                    ReservedLitres = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false, defaultValue: 0m),
                    MinThresholdLitres = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Available"),
                    LastReplenishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastDipReadingAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tanks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DipReadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DipValueLitres = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false),
                    SystemStockLitres = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false),
                    VarianceLitres = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false),
                    VariancePercent = table.Column<decimal>(type: "DECIMAL(5,2)", nullable: false),
                    IsFraudFlagged = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DipReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DipReadings_Tanks_TankId",
                        column: x => x.TankId,
                        principalTable: "Tanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReplenishmentRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    StationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedQuantityLitres = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false),
                    UrgencyLevel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "Normal"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Submitted"),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplenishmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReplenishmentRequests_Tanks_TankId",
                        column: x => x.TankId,
                        principalTable: "Tanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockLoadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityLoadedLitres = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false),
                    LoadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TankerNumber = table.Column<string>(type: "VARCHAR(30)", maxLength: 30, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "VARCHAR(50)", maxLength: 50, nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    StockBefore = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false),
                    StockAfter = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLoadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockLoadings_Tanks_TankId",
                        column: x => x.TankId,
                        principalTable: "Tanks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DipReadings_TankId",
                table: "DipReadings",
                column: "TankId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedEvents_EventId",
                table: "ProcessedEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentRequests_StationId",
                table: "ReplenishmentRequests",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentRequests_TankId",
                table: "ReplenishmentRequests",
                column: "TankId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLoadings_TankId",
                table: "StockLoadings",
                column: "TankId");

            migrationBuilder.CreateIndex(
                name: "IX_Tanks_StationId",
                table: "Tanks",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_Tanks_TankSerialNumber",
                table: "Tanks",
                column: "TankSerialNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DipReadings");

            migrationBuilder.DropTable(
                name: "ProcessedEvents");

            migrationBuilder.DropTable(
                name: "ReplenishmentRequests");

            migrationBuilder.DropTable(
                name: "StockLoadings");

            migrationBuilder.DropTable(
                name: "Tanks");
        }
    }
}
