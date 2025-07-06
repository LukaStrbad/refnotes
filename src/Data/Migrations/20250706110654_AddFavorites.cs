using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "directory_favorites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EncryptedDirectoryId = table.Column<int>(type: "int", nullable: false),
                    FavoriteDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_directory_favorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_directory_favorites_encrypted_directories_EncryptedDirectory~",
                        column: x => x.EncryptedDirectoryId,
                        principalTable: "encrypted_directories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_directory_favorites_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "file_favorites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    EncryptedFileId = table.Column<int>(type: "int", nullable: false),
                    FavoriteDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_favorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_file_favorites_encrypted_files_EncryptedFileId",
                        column: x => x.EncryptedFileId,
                        principalTable: "encrypted_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_file_favorites_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_directory_favorites_EncryptedDirectoryId",
                table: "directory_favorites",
                column: "EncryptedDirectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_directory_favorites_UserId",
                table: "directory_favorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_file_favorites_EncryptedFileId",
                table: "file_favorites",
                column: "EncryptedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_file_favorites_UserId",
                table: "file_favorites",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "directory_favorites");

            migrationBuilder.DropTable(
                name: "file_favorites");
        }
    }
}
