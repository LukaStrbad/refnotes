using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigrationService.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyColumnsToEncryptedFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_encrypted_files_encrypted_directories_EncryptedDirectoryId",
                table: "encrypted_files");

            migrationBuilder.AlterColumn<int>(
                name: "EncryptedDirectoryId",
                table: "encrypted_files",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_encrypted_files_encrypted_directories_EncryptedDirectoryId",
                table: "encrypted_files",
                column: "EncryptedDirectoryId",
                principalTable: "encrypted_directories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_encrypted_files_encrypted_directories_EncryptedDirectoryId",
                table: "encrypted_files");

            migrationBuilder.AlterColumn<int>(
                name: "EncryptedDirectoryId",
                table: "encrypted_files",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_encrypted_files_encrypted_directories_EncryptedDirectoryId",
                table: "encrypted_files",
                column: "EncryptedDirectoryId",
                principalTable: "encrypted_directories",
                principalColumn: "Id");
        }
    }
}
