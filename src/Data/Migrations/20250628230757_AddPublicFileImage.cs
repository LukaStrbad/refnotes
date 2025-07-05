using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicFileImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "public_file_images",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PublicFileId = table.Column<int>(type: "int", nullable: false),
                    EncryptedFileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_public_file_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_public_file_images_encrypted_files_EncryptedFileId",
                        column: x => x.EncryptedFileId,
                        principalTable: "encrypted_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_public_file_images_public_files_PublicFileId",
                        column: x => x.PublicFileId,
                        principalTable: "public_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_public_file_images_EncryptedFileId",
                table: "public_file_images",
                column: "EncryptedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_public_file_images_PublicFileId",
                table: "public_file_images",
                column: "PublicFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "public_file_images");
        }
    }
}
