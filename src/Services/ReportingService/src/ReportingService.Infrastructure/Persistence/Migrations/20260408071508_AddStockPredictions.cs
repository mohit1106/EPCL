using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReportingService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockPredictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockPredictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FuelTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentStockLitres = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AvgDailyConsumptionL = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    PredictedEmptyAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DaysUntilEmpty = table.Column<decimal>(type: "decimal(6,1)", nullable: true),
                    AlertSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DataPointsUsed = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockPredictions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockPredictions_DaysUntilEmpty",
                table: "StockPredictions",
                column: "DaysUntilEmpty");

            migrationBuilder.CreateIndex(
                name: "IX_StockPredictions_StationId",
                table: "StockPredictions",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_StockPredictions_TankId",
                table: "StockPredictions",
                column: "TankId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockPredictions");
        }
    }
}
