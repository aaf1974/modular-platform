using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Demo.Api.PostgresMigration.Migrations
{
    /// <inheritdoc />
    public partial class AuthRestorePass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_auth_users_NormalizedEmail",
                table: "auth_users",
                newName: "ux_auth_users_normalized_email");

            migrationBuilder.CreateTable(
                name: "auth_password_reset_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_password_reset_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_auth_password_reset_tokens_auth_users_UserId",
                        column: x => x.UserId,
                        principalTable: "auth_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auth_password_reset_tokens_UserId_ExpiresAtUtc",
                table: "auth_password_reset_tokens",
                columns: new[] { "UserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "ux_auth_password_reset_tokens_hash",
                table: "auth_password_reset_tokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_password_reset_tokens");

            migrationBuilder.RenameIndex(
                name: "ux_auth_users_normalized_email",
                table: "auth_users",
                newName: "IX_auth_users_NormalizedEmail");
        }
    }
}
