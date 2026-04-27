using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverPreAuthorizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FuelPreAuthorizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    DriverUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FleetAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorizedAmountINR = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false),
                    AuthorizedLitres = table.Column<decimal>(type: "DECIMAL(10,3)", nullable: true),
                    FuelTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedByTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelPreAuthorizations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FuelPreAuthorizations_AuthCode",
                table: "FuelPreAuthorizations",
                column: "AuthCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FuelPreAuthorizations");
        }
    }
}
