using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigrationService.Migrations
{
    /// <inheritdoc />
    public partial class AddFileTagGroupOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_file_tags_users_OwnerId",
                table: "file_tags");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "file_tags",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "GroupOwnerId",
                table: "file_tags",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_file_tags_GroupOwnerId",
                table: "file_tags",
                column: "GroupOwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_file_tags_user_groups_GroupOwnerId",
                table: "file_tags",
                column: "GroupOwnerId",
                principalTable: "user_groups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_file_tags_users_OwnerId",
                table: "file_tags",
                column: "OwnerId",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_file_tags_user_groups_GroupOwnerId",
                table: "file_tags");

            migrationBuilder.DropForeignKey(
                name: "FK_file_tags_users_OwnerId",
                table: "file_tags");

            migrationBuilder.DropIndex(
                name: "IX_file_tags_GroupOwnerId",
                table: "file_tags");

            migrationBuilder.DropColumn(
                name: "GroupOwnerId",
                table: "file_tags");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "file_tags",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_file_tags_users_OwnerId",
                table: "file_tags",
                column: "OwnerId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
