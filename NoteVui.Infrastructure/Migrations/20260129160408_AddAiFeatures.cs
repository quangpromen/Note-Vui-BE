using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoteVui.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_note_contents_notes_NoteId",
                table: "note_contents");

            migrationBuilder.RenameColumn(
                name: "NoteId",
                table: "note_contents",
                newName: "note_id");

            migrationBuilder.CreateTable(
                name: "ai_usage_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    InputTokens = table.Column<int>(type: "int", nullable: false),
                    OutputTokens = table.Column<int>(type: "int", nullable: false),
                    NoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_usage_logs", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "11111111-1111-1111-1111-111111111111",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "cf0a6f28-c85a-4456-8b2f-4e443d992850", "AQAAAAIAAYagAAAAECGNZxAVEP8uTRQVlGRTy41KvrmpZK82/TKa8t5P5wDly8gklE9QHN7ptvBVwF1SAQ==" });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_NoteId",
                table: "ai_usage_logs",
                column: "NoteId",
                filter: "[NoteId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_User_CreatedAt",
                table: "ai_usage_logs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_note_contents_notes_note_id",
                table: "note_contents",
                column: "note_id",
                principalTable: "notes",
                principalColumn: "note_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_note_contents_notes_note_id",
                table: "note_contents");

            migrationBuilder.DropTable(
                name: "ai_usage_logs");

            migrationBuilder.RenameColumn(
                name: "note_id",
                table: "note_contents",
                newName: "NoteId");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "11111111-1111-1111-1111-111111111111",
                columns: new[] { "ConcurrencyStamp", "PasswordHash" },
                values: new object[] { "90bcf484-899a-4238-9b07-a848d55d51fc", "AQAAAAIAAYagAAAAEAhaBD87ykivzVlg707EYeGrzGyHhF5wkeFz3/CdcyQ3Eb1KF9O1ETRXN6BICEj6CA==" });

            migrationBuilder.AddForeignKey(
                name: "FK_note_contents_notes_NoteId",
                table: "note_contents",
                column: "NoteId",
                principalTable: "notes",
                principalColumn: "note_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
