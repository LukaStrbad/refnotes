using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigrationService.Migrations
{
    /// <inheritdoc />
    public partial class AddUserGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_encrypted_directories_users_OwnerId",
                table: "encrypted_directories");

            migrationBuilder.AlterColumn<string>(
                name: "Path",
                table: "encrypted_directories",
                type: "varchar(1024)",
                maxLength: 1024,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "encrypted_directories",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "encrypted_directories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "user_groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_groups", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_group_roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    UserGroupId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_group_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_group_roles_user_groups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "user_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_group_roles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_encrypted_directories_GroupId",
                table: "encrypted_directories",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_user_group_roles_UserGroupId",
                table: "user_group_roles",
                column: "UserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_user_group_roles_UserId_Role",
                table: "user_group_roles",
                columns: new[] { "UserId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_user_group_roles_UserId_UserGroupId",
                table: "user_group_roles",
                columns: new[] { "UserId", "UserGroupId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_encrypted_directories_user_groups_GroupId",
                table: "encrypted_directories",
                column: "GroupId",
                principalTable: "user_groups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_encrypted_directories_users_OwnerId",
                table: "encrypted_directories",
                column: "OwnerId",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_encrypted_directories_user_groups_GroupId",
                table: "encrypted_directories");

            migrationBuilder.DropForeignKey(
                name: "FK_encrypted_directories_users_OwnerId",
                table: "encrypted_directories");

            migrationBuilder.DropTable(
                name: "user_group_roles");

            migrationBuilder.DropTable(
                name: "user_groups");

            migrationBuilder.DropIndex(
                name: "IX_encrypted_directories_GroupId",
                table: "encrypted_directories");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "encrypted_directories");

            migrationBuilder.AlterColumn<string>(
                name: "Path",
                table: "encrypted_directories",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(1024)",
                oldMaxLength: 1024)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "encrypted_directories",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_encrypted_directories_users_OwnerId",
                table: "encrypted_directories",
                column: "OwnerId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
