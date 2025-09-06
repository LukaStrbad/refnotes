using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSharedFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_shared_files_encrypted_files_EncryptedFileId",
                table: "shared_files");

            migrationBuilder.DropForeignKey(
                name: "FK_shared_files_users_SharedToId",
                table: "shared_files");

            migrationBuilder.RenameColumn(
                name: "SharedToId",
                table: "shared_files",
                newName: "SharedToDirectoryId");

            migrationBuilder.RenameColumn(
                name: "EncryptedFileId",
                table: "shared_files",
                newName: "SharedEncryptedFileId");

            migrationBuilder.RenameIndex(
                name: "IX_shared_files_SharedToId",
                table: "shared_files",
                newName: "IX_shared_files_SharedToDirectoryId");

            migrationBuilder.RenameIndex(
                name: "IX_shared_files_EncryptedFileId",
                table: "shared_files",
                newName: "IX_shared_files_SharedEncryptedFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_shared_files_encrypted_directories_SharedToDirectoryId",
                table: "shared_files",
                column: "SharedToDirectoryId",
                principalTable: "encrypted_directories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_shared_files_encrypted_files_SharedEncryptedFileId",
                table: "shared_files",
                column: "SharedEncryptedFileId",
                principalTable: "encrypted_files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_shared_files_encrypted_directories_SharedToDirectoryId",
                table: "shared_files");

            migrationBuilder.DropForeignKey(
                name: "FK_shared_files_encrypted_files_SharedEncryptedFileId",
                table: "shared_files");

            migrationBuilder.RenameColumn(
                name: "SharedToDirectoryId",
                table: "shared_files",
                newName: "SharedToId");

            migrationBuilder.RenameColumn(
                name: "SharedEncryptedFileId",
                table: "shared_files",
                newName: "EncryptedFileId");

            migrationBuilder.RenameIndex(
                name: "IX_shared_files_SharedToDirectoryId",
                table: "shared_files",
                newName: "IX_shared_files_SharedToId");

            migrationBuilder.RenameIndex(
                name: "IX_shared_files_SharedEncryptedFileId",
                table: "shared_files",
                newName: "IX_shared_files_EncryptedFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_shared_files_encrypted_files_EncryptedFileId",
                table: "shared_files",
                column: "EncryptedFileId",
                principalTable: "encrypted_files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_shared_files_users_SharedToId",
                table: "shared_files",
                column: "SharedToId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
