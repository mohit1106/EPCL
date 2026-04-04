using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReportingService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailySalesSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    StationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FuelTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalTransactions = table.Column<int>(type: "int", nullable: false),
                    TotalLitresSold = table.Column<decimal>(type: "decimal(12,3)", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailySalesSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ReportType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    DateTo = table.Column<DateOnly>(type: "date", nullable: true),
                    StationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Format = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyStationReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    StationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    TotalTransactions = table.Column<int>(type: "int", nullable: false),
                    TotalLitresSold = table.Column<decimal>(type: "decimal(14,3)", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "decimal(16,2)", nullable: false),
                    PetrolLitres = table.Column<decimal>(type: "decimal(14,3)", nullable: false),
                    DieselLitres = table.Column<decimal>(type: "decimal(14,3)", nullable: false),
                    CngLitres = table.Column<decimal>(type: "decimal(14,3)", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyStationReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CronExpression = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Format = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailySalesSummaries_StationId",
                table: "DailySalesSummaries",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_DailySalesSummaries_StationId_FuelTypeId_Date",
                table: "DailySalesSummaries",
                columns: new[] { "StationId", "FuelTypeId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStationReports_StationId",
                table: "MonthlyStationReports",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedEvents_EventId",
                table: "ProcessedEvents",
                column: "EventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailySalesSummaries");

            migrationBuilder.DropTable(
                name: "GeneratedReports");

            migrationBuilder.DropTable(
                name: "MonthlyStationReports");

            migrationBuilder.DropTable(
                name: "ProcessedEvents");

            migrationBuilder.DropTable(
                name: "ScheduledReports");
        }
    }
}
