using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHelpRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HelpRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DealerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DealerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TargetAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetAdminName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HelpRequestReplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HelpRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FromName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FromUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelpRequestReplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HelpRequestReplies_HelpRequests_HelpRequestId",
                        column: x => x.HelpRequestId,
                        principalTable: "HelpRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequestReplies_HelpRequestId",
                table: "HelpRequestReplies",
                column: "HelpRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequests_DealerUserId",
                table: "HelpRequests",
                column: "DealerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequests_Status",
                table: "HelpRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_HelpRequests_TargetAdminId",
                table: "HelpRequests",
                column: "TargetAdminId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HelpRequestReplies");

            migrationBuilder.DropTable(
                name: "HelpRequests");
        }
    }
}
