using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigrationService.Migrations
{
    /// <inheritdoc />
    public partial class RenameEncryptedFileTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EncryptedFile_Directories_EncryptedDirectoryId",
                table: "EncryptedFile");

            migrationBuilder.DropForeignKey(
                name: "FK_EncryptedFileFileTag_EncryptedFile_FilesId",
                table: "EncryptedFileFileTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EncryptedFile",
                table: "EncryptedFile");

            migrationBuilder.RenameTable(
                name: "EncryptedFile",
                newName: "Files");

            migrationBuilder.RenameColumn(
                name: "EncryptedName",
                table: "FileTags",
                newName: "Name");

            migrationBuilder.RenameIndex(
                name: "IX_EncryptedFile_EncryptedDirectoryId",
                table: "Files",
                newName: "IX_Files_EncryptedDirectoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Files",
                table: "Files",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EncryptedFileFileTag_Files_FilesId",
                table: "EncryptedFileFileTag",
                column: "FilesId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Directories_EncryptedDirectoryId",
                table: "Files",
                column: "EncryptedDirectoryId",
                principalTable: "Directories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EncryptedFileFileTag_Files_FilesId",
                table: "EncryptedFileFileTag");

            migrationBuilder.DropForeignKey(
                name: "FK_Files_Directories_EncryptedDirectoryId",
                table: "Files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Files",
                table: "Files");

            migrationBuilder.RenameTable(
                name: "Files",
                newName: "EncryptedFile");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "FileTags",
                newName: "EncryptedName");

            migrationBuilder.RenameIndex(
                name: "IX_Files_EncryptedDirectoryId",
                table: "EncryptedFile",
                newName: "IX_EncryptedFile_EncryptedDirectoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EncryptedFile",
                table: "EncryptedFile",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EncryptedFile_Directories_EncryptedDirectoryId",
                table: "EncryptedFile",
                column: "EncryptedDirectoryId",
                principalTable: "Directories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EncryptedFileFileTag_EncryptedFile_FilesId",
                table: "EncryptedFileFileTag",
                column: "FilesId",
                principalTable: "EncryptedFile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
