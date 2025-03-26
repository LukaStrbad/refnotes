using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Directories_Directories_ParentId",
                table: "Directories");

            migrationBuilder.DropForeignKey(
                name: "FK_Directories_Users_OwnerId",
                table: "Directories");

            migrationBuilder.DropForeignKey(
                name: "FK_EncryptedFileFileTag_FileTags_TagsId",
                table: "EncryptedFileFileTag");

            migrationBuilder.DropForeignKey(
                name: "FK_EncryptedFileFileTag_Files_FilesId",
                table: "EncryptedFileFileTag");

            migrationBuilder.DropForeignKey(
                name: "FK_FileTags_Users_OwnerId",
                table: "FileTags");

            migrationBuilder.DropForeignKey(
                name: "FK_Files_Directories_EncryptedDirectoryId",
                table: "Files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRefreshTokens",
                table: "UserRefreshTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Files",
                table: "Files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FileTags",
                table: "FileTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EncryptedFileFileTag",
                table: "EncryptedFileFileTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Directories",
                table: "Directories");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "UserRefreshTokens",
                newName: "user_refresh_tokens");

            migrationBuilder.RenameTable(
                name: "Files",
                newName: "encrypted_files");

            migrationBuilder.RenameTable(
                name: "FileTags",
                newName: "file_tags");

            migrationBuilder.RenameTable(
                name: "EncryptedFileFileTag",
                newName: "encrypted_files_file_tags");

            migrationBuilder.RenameTable(
                name: "Directories",
                newName: "encrypted_directories");

            migrationBuilder.RenameIndex(
                name: "IX_Files_EncryptedDirectoryId",
                table: "encrypted_files",
                newName: "IX_encrypted_files_EncryptedDirectoryId");

            migrationBuilder.RenameIndex(
                name: "IX_FileTags_OwnerId",
                table: "file_tags",
                newName: "IX_file_tags_OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_EncryptedFileFileTag_TagsId",
                table: "encrypted_files_file_tags",
                newName: "IX_encrypted_files_file_tags_TagsId");

            migrationBuilder.RenameIndex(
                name: "IX_Directories_ParentId",
                table: "encrypted_directories",
                newName: "IX_encrypted_directories_ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_Directories_OwnerId",
                table: "encrypted_directories",
                newName: "IX_encrypted_directories_OwnerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_refresh_tokens",
                table: "user_refresh_tokens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_encrypted_files",
                table: "encrypted_files",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_file_tags",
                table: "file_tags",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_encrypted_files_file_tags",
                table: "encrypted_files_file_tags",
                columns: new[] { "FilesId", "TagsId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_encrypted_directories",
                table: "encrypted_directories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_encrypted_directories_encrypted_directories_ParentId",
                table: "encrypted_directories",
                column: "ParentId",
                principalTable: "encrypted_directories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_encrypted_directories_users_OwnerId",
                table: "encrypted_directories",
                column: "OwnerId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_encrypted_files_encrypted_directories_EncryptedDirectoryId",
                table: "encrypted_files",
                column: "EncryptedDirectoryId",
                principalTable: "encrypted_directories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_encrypted_files_file_tags_encrypted_files_FilesId",
                table: "encrypted_files_file_tags",
                column: "FilesId",
                principalTable: "encrypted_files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_encrypted_files_file_tags_file_tags_TagsId",
                table: "encrypted_files_file_tags",
                column: "TagsId",
                principalTable: "file_tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_file_tags_users_OwnerId",
                table: "file_tags",
                column: "OwnerId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_encrypted_directories_encrypted_directories_ParentId",
                table: "encrypted_directories");

            migrationBuilder.DropForeignKey(
                name: "FK_encrypted_directories_users_OwnerId",
                table: "encrypted_directories");

            migrationBuilder.DropForeignKey(
                name: "FK_encrypted_files_encrypted_directories_EncryptedDirectoryId",
                table: "encrypted_files");

            migrationBuilder.DropForeignKey(
                name: "FK_encrypted_files_file_tags_encrypted_files_FilesId",
                table: "encrypted_files_file_tags");

            migrationBuilder.DropForeignKey(
                name: "FK_encrypted_files_file_tags_file_tags_TagsId",
                table: "encrypted_files_file_tags");

            migrationBuilder.DropForeignKey(
                name: "FK_file_tags_users_OwnerId",
                table: "file_tags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_refresh_tokens",
                table: "user_refresh_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_file_tags",
                table: "file_tags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_encrypted_files_file_tags",
                table: "encrypted_files_file_tags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_encrypted_files",
                table: "encrypted_files");

            migrationBuilder.DropPrimaryKey(
                name: "PK_encrypted_directories",
                table: "encrypted_directories");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "user_refresh_tokens",
                newName: "UserRefreshTokens");

            migrationBuilder.RenameTable(
                name: "file_tags",
                newName: "FileTags");

            migrationBuilder.RenameTable(
                name: "encrypted_files_file_tags",
                newName: "EncryptedFileFileTag");

            migrationBuilder.RenameTable(
                name: "encrypted_files",
                newName: "Files");

            migrationBuilder.RenameTable(
                name: "encrypted_directories",
                newName: "Directories");

            migrationBuilder.RenameIndex(
                name: "IX_file_tags_OwnerId",
                table: "FileTags",
                newName: "IX_FileTags_OwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_encrypted_files_file_tags_TagsId",
                table: "EncryptedFileFileTag",
                newName: "IX_EncryptedFileFileTag_TagsId");

            migrationBuilder.RenameIndex(
                name: "IX_encrypted_files_EncryptedDirectoryId",
                table: "Files",
                newName: "IX_Files_EncryptedDirectoryId");

            migrationBuilder.RenameIndex(
                name: "IX_encrypted_directories_ParentId",
                table: "Directories",
                newName: "IX_Directories_ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_encrypted_directories_OwnerId",
                table: "Directories",
                newName: "IX_Directories_OwnerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRefreshTokens",
                table: "UserRefreshTokens",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FileTags",
                table: "FileTags",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EncryptedFileFileTag",
                table: "EncryptedFileFileTag",
                columns: new[] { "FilesId", "TagsId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Files",
                table: "Files",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Directories",
                table: "Directories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Directories_Directories_ParentId",
                table: "Directories",
                column: "ParentId",
                principalTable: "Directories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Directories_Users_OwnerId",
                table: "Directories",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EncryptedFileFileTag_FileTags_TagsId",
                table: "EncryptedFileFileTag",
                column: "TagsId",
                principalTable: "FileTags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EncryptedFileFileTag_Files_FilesId",
                table: "EncryptedFileFileTag",
                column: "FilesId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FileTags_Users_OwnerId",
                table: "FileTags",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Directories_EncryptedDirectoryId",
                table: "Files",
                column: "EncryptedDirectoryId",
                principalTable: "Directories",
                principalColumn: "Id");
        }
    }
}
