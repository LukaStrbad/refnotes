using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Data.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    ExpiryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_refresh_tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Password = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    Roles = table.Column<string[]>(type: "text[]", nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "email_confirm_tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_confirm_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_email_confirm_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "encrypted_directories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    PathHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: true),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    GroupId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_encrypted_directories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_encrypted_directories_encrypted_directories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "encrypted_directories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_encrypted_directories_user_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "user_groups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_encrypted_directories_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "file_tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    NameHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OwnerId = table.Column<int>(type: "integer", nullable: true),
                    GroupOwnerId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_file_tags_user_groups_GroupOwnerId",
                        column: x => x.GroupOwnerId,
                        principalTable: "user_groups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_file_tags_users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "group_access_codes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    SenderId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_group_access_codes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_group_access_codes_user_groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "user_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_access_codes_users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_password_reset_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_group_roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    UserGroupId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "directory_favorites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    EncryptedDirectoryId = table.Column<int>(type: "integer", nullable: false),
                    FavoriteDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_directory_favorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_directory_favorites_encrypted_directories_EncryptedDirector~",
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
                });

            migrationBuilder.CreateTable(
                name: "encrypted_files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilesystemName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NameHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EncryptedDirectoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_encrypted_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_encrypted_files_encrypted_directories_EncryptedDirectoryId",
                        column: x => x.EncryptedDirectoryId,
                        principalTable: "encrypted_directories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "encrypted_files_file_tags",
                columns: table => new
                {
                    FilesId = table.Column<int>(type: "integer", nullable: false),
                    TagsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_encrypted_files_file_tags", x => new { x.FilesId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_encrypted_files_file_tags_encrypted_files_FilesId",
                        column: x => x.FilesId,
                        principalTable: "encrypted_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_encrypted_files_file_tags_file_tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "file_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "file_favorites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    EncryptedFileId = table.Column<int>(type: "integer", nullable: false),
                    FavoriteDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "public_files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UrlHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EncryptedFileId = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_public_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_public_files_encrypted_files_EncryptedFileId",
                        column: x => x.EncryptedFileId,
                        principalTable: "encrypted_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shared_file_hashes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EncryptedFileId = table.Column<int>(type: "integer", nullable: false),
                    Hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shared_file_hashes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shared_file_hashes_encrypted_files_EncryptedFileId",
                        column: x => x.EncryptedFileId,
                        principalTable: "encrypted_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shared_files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SharedEncryptedFileId = table.Column<int>(type: "integer", nullable: false),
                    SharedToDirectoryId = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shared_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shared_files_encrypted_directories_SharedToDirectoryId",
                        column: x => x.SharedToDirectoryId,
                        principalTable: "encrypted_directories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_shared_files_encrypted_files_SharedEncryptedFileId",
                        column: x => x.SharedEncryptedFileId,
                        principalTable: "encrypted_files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "public_file_images",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicFileId = table.Column<int>(type: "integer", nullable: false),
                    EncryptedFileId = table.Column<int>(type: "integer", nullable: false)
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
                });

            migrationBuilder.CreateIndex(
                name: "IX_directory_favorites_EncryptedDirectoryId",
                table: "directory_favorites",
                column: "EncryptedDirectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_directory_favorites_UserId",
                table: "directory_favorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_email_confirm_tokens_UserId",
                table: "email_confirm_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_email_confirm_tokens_Value",
                table: "email_confirm_tokens",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_encrypted_directories_GroupId",
                table: "encrypted_directories",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_encrypted_directories_OwnerId",
                table: "encrypted_directories",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_encrypted_directories_ParentId",
                table: "encrypted_directories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_encrypted_files_EncryptedDirectoryId",
                table: "encrypted_files",
                column: "EncryptedDirectoryId");

            migrationBuilder.CreateIndex(
                name: "IX_encrypted_files_file_tags_TagsId",
                table: "encrypted_files_file_tags",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_file_favorites_EncryptedFileId",
                table: "file_favorites",
                column: "EncryptedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_file_favorites_UserId",
                table: "file_favorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_file_tags_GroupOwnerId",
                table: "file_tags",
                column: "GroupOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_file_tags_OwnerId",
                table: "file_tags",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_group_access_codes_ExpiryTime",
                table: "group_access_codes",
                column: "ExpiryTime");

            migrationBuilder.CreateIndex(
                name: "IX_group_access_codes_GroupId_Value",
                table: "group_access_codes",
                columns: new[] { "GroupId", "Value" });

            migrationBuilder.CreateIndex(
                name: "IX_group_access_codes_SenderId",
                table: "group_access_codes",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_UserId",
                table: "password_reset_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_Value",
                table: "password_reset_tokens",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_public_file_images_EncryptedFileId",
                table: "public_file_images",
                column: "EncryptedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_public_file_images_PublicFileId",
                table: "public_file_images",
                column: "PublicFileId");

            migrationBuilder.CreateIndex(
                name: "IX_public_files_EncryptedFileId",
                table: "public_files",
                column: "EncryptedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_public_files_UrlHash_State",
                table: "public_files",
                columns: new[] { "UrlHash", "State" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shared_file_hashes_EncryptedFileId",
                table: "shared_file_hashes",
                column: "EncryptedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_shared_file_hashes_Hash",
                table: "shared_file_hashes",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_shared_files_SharedEncryptedFileId",
                table: "shared_files",
                column: "SharedEncryptedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_shared_files_SharedToDirectoryId",
                table: "shared_files",
                column: "SharedToDirectoryId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "directory_favorites");

            migrationBuilder.DropTable(
                name: "email_confirm_tokens");

            migrationBuilder.DropTable(
                name: "encrypted_files_file_tags");

            migrationBuilder.DropTable(
                name: "file_favorites");

            migrationBuilder.DropTable(
                name: "group_access_codes");

            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            migrationBuilder.DropTable(
                name: "public_file_images");

            migrationBuilder.DropTable(
                name: "shared_file_hashes");

            migrationBuilder.DropTable(
                name: "shared_files");

            migrationBuilder.DropTable(
                name: "user_group_roles");

            migrationBuilder.DropTable(
                name: "user_refresh_tokens");

            migrationBuilder.DropTable(
                name: "file_tags");

            migrationBuilder.DropTable(
                name: "public_files");

            migrationBuilder.DropTable(
                name: "encrypted_files");

            migrationBuilder.DropTable(
                name: "encrypted_directories");

            migrationBuilder.DropTable(
                name: "user_groups");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
