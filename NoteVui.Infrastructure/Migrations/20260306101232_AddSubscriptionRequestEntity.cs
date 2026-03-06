using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoteVui.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionRequestEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subscription_requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    plan_type = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    admin_note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    processed_by_user_id = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    processed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_subscription_requests_Users_processed_by_user_id",
                        column: x => x.processed_by_user_id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subscription_requests_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_requests_processed_by_user_id",
                table: "subscription_requests",
                column: "processed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRequests_Status",
                table: "subscription_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRequests_User_Status",
                table: "subscription_requests",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionRequests_UserId",
                table: "subscription_requests",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subscription_requests");
        }
    }
}
