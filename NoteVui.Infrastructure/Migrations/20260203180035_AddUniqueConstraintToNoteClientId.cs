using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoteVui.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToNoteClientId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "11111111-1111-1111-1111-111111111111");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AccessFailedCount", "AvatarUrl", "ConcurrencyStamp", "Email", "EmailConfirmed", "FullName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "RefreshToken", "RefreshTokenExpiryTime", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "11111111-1111-1111-1111-111111111111", 0, null, "5d640505-0b2e-4dbc-af3e-554a93a68b3c", "test@notevui.com", true, "Test User", false, null, "TEST@NOTEVUI.COM", "TEST@NOTEVUI.COM", "AQAAAAIAAYagAAAAEO9GABNv5jEHmpQGt/F4feF8/mGkGVQqLGgRNk3pv0UoxZlK8P/KSL92hqyGy8YekQ==", null, false, null, null, "2E2B8BB1-8BE4-4E40-8C8E-8E8E8E8E8E8E", false, "test@notevui.com" });
        }
    }
}
