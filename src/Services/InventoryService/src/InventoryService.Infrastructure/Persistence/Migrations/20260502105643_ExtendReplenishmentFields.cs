using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendReplenishmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedDriverCode",
                table: "ReplenishmentRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedDriverId",
                table: "ReplenishmentRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedDriverName",
                table: "ReplenishmentRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedDriverPhone",
                table: "ReplenishmentRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DealerVerifiedAt",
                table: "ReplenishmentRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DealerVerifiedDriverCode",
                table: "ReplenishmentRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuelTypeName",
                table: "ReplenishmentRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "ReplenishmentRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "ReplenishmentRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestedWindow",
                table: "ReplenishmentRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetPumpName",
                table: "ReplenishmentRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedDriverCode",
                table: "ReplenishmentRequests");

            migrationBuilder.DropColumn(
                name: "AssignedDriverId",
                table: "ReplenishmentRequests");

            migrationBuilder.DropColumn(
                name: "AssignedDriverName",
                table: "ReplenishmentRequests");

            migrationBuilder.DropColumn(
                name: "AssignedDriverPhone",
                table: "ReplenishmentRequests");

            migrationBuilder.DropColumn(
                name: "DealerVerifiedAt",
                table: "ReplenishmentRequests");

            migrationBuilder.DropColumn(
                name: "DealerVerifiedDriverCode",
                table: "ReplenishmentRequests");

            migrationBuilder.DropColumn(
                name: "FuelTypeName",
                table: "ReplenishmentRequests");

            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "ReplenishmentRequests");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "ReplenishmentRequests");

            migrationBuilder.DropColumn(
                name: "RequestedWindow",
                table: "ReplenishmentRequests");

            migrationBuilder.DropColumn(
                name: "TargetPumpName",
                table: "ReplenishmentRequests");
        }
    }
}
