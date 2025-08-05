using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNameAndPathHashFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NameHash",
                table: "file_tags",
                type: "char(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NameHash",
                table: "encrypted_files",
                type: "char(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PathHash",
                table: "encrypted_directories",
                type: "char(64)",
                fixedLength: true,
                maxLength: 64,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameHash",
                table: "file_tags");

            migrationBuilder.DropColumn(
                name: "NameHash",
                table: "encrypted_files");

            migrationBuilder.DropColumn(
                name: "PathHash",
                table: "encrypted_directories");
        }
    }
}
