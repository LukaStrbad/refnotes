using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class FileAndDirectoryUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Directories",
                newName: "Path");

            migrationBuilder.AddColumn<string>(
                name: "FilesystemName",
                table: "EncryptedFile",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FilesystemName",
                table: "Directories",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FilesystemName",
                table: "EncryptedFile");

            migrationBuilder.DropColumn(
                name: "FilesystemName",
                table: "Directories");

            migrationBuilder.RenameColumn(
                name: "Path",
                table: "Directories",
                newName: "Name");
        }
    }
}
