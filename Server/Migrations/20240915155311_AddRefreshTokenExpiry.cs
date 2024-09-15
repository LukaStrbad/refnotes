using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UserRefreshTokens");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryTime",
                table: "UserRefreshTokens",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiryTime",
                table: "UserRefreshTokens");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UserRefreshTokens",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
