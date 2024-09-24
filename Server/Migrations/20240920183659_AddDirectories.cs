using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Server.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Directories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    EncryptedDirectoryId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Directories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Directories_Directories_EncryptedDirectoryId",
                        column: x => x.EncryptedDirectoryId,
                        principalTable: "Directories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Directories_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EncryptedFile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    EncryptedDirectoryId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncryptedFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EncryptedFile_Directories_EncryptedDirectoryId",
                        column: x => x.EncryptedDirectoryId,
                        principalTable: "Directories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Directories_EncryptedDirectoryId",
                table: "Directories",
                column: "EncryptedDirectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Directories_OwnerId",
                table: "Directories",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_EncryptedFile_EncryptedDirectoryId",
                table: "EncryptedFile",
                column: "EncryptedDirectoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EncryptedFile");

            migrationBuilder.DropTable(
                name: "Directories");
        }
    }
}
