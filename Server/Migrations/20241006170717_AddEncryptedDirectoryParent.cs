using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddEncryptedDirectoryParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Directories_Directories_EncryptedDirectoryId",
                table: "Directories");

            migrationBuilder.DropColumn(
                name: "Directories",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Files",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "EncryptedDirectoryId",
                table: "Directories",
                newName: "ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_Directories_EncryptedDirectoryId",
                table: "Directories",
                newName: "IX_Directories_ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Directories_Directories_ParentId",
                table: "Directories",
                column: "ParentId",
                principalTable: "Directories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Directories_Directories_ParentId",
                table: "Directories");

            migrationBuilder.RenameColumn(
                name: "ParentId",
                table: "Directories",
                newName: "EncryptedDirectoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Directories_ParentId",
                table: "Directories",
                newName: "IX_Directories_EncryptedDirectoryId");

            migrationBuilder.AddColumn<string>(
                name: "Directories",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Files",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Directories_Directories_EncryptedDirectoryId",
                table: "Directories",
                column: "EncryptedDirectoryId",
                principalTable: "Directories",
                principalColumn: "Id");
        }
    }
}
