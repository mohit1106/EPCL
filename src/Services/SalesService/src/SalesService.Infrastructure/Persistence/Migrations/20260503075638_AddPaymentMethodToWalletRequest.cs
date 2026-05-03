using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodToWalletRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "WalletPaymentRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "Wallet");

            migrationBuilder.AddColumn<string>(
                name: "RazorpayOrderId",
                table: "WalletPaymentRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RazorpayPaymentId",
                table: "WalletPaymentRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "WalletPaymentRequests");

            migrationBuilder.DropColumn(
                name: "RazorpayOrderId",
                table: "WalletPaymentRequests");

            migrationBuilder.DropColumn(
                name: "RazorpayPaymentId",
                table: "WalletPaymentRequests");
        }
    }
}
