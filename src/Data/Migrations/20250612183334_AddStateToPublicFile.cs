using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigrationService.Migrations
{
    /// <inheritdoc />
    public partial class AddStateToPublicFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_public_files_UrlHash",
                table: "public_files");

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "public_files",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_public_files_UrlHash_State",
                table: "public_files",
                columns: new[] { "UrlHash", "State" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_public_files_UrlHash_State",
                table: "public_files");

            migrationBuilder.DropColumn(
                name: "State",
                table: "public_files");

            migrationBuilder.CreateIndex(
                name: "IX_public_files_UrlHash",
                table: "public_files",
                column: "UrlHash",
                unique: true);
        }
    }
}
