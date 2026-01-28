using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoteVui.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientIdToNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "client_id",
                table: "notes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "11111111-1111-1111-1111-111111111111",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "90bcf484-899a-4238-9b07-a848d55d51fc", "AQAAAAIAAYagAAAAEAhaBD87ykivzVlg707EYeGrzGyHhF5wkeFz3/CdcyQ3Eb1KF9O1ETRXN6BICEj6CA==" });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_User_ClientId",
                table: "notes",
                columns: new[] { "user_id", "client_id" });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_User_UpdatedAt",
                table: "notes",
                columns: new[] { "user_id", "updated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notes_User_ClientId",
                table: "notes");

            migrationBuilder.DropIndex(
                name: "IX_Notes_User_UpdatedAt",
                table: "notes");

            migrationBuilder.DropColumn(
                name: "client_id",
                table: "notes");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "11111111-1111-1111-1111-111111111111",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "4dba5f71-3c32-4d29-880b-7877e570d2c0", "AQAAAAIAAYagAAAAEK9l3c5s/Ll1Y0DwrBU7Rah+N2WbN9v+5RYaQXJtK3qIr5tqsymfHMoT5wNxPvQCgQ==" });
        }
    }
}
