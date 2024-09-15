﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Server.Db;

#nullable disable

namespace Server.Migrations
{
    [DbContext(typeof(RefNotesContext))]
    [Migration("20240912195238_AddRefreshTokens")]
    partial class AddRefreshTokens
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.6");

            modelBuilder.Entity("Server.Model.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasAnnotation("Relational:JsonPropertyName", "id");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(1024)
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "email");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "name");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasMaxLength(4096)
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "password");

                    b.Property<string>("Roles")
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "roles");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "username");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Server.Model.UserRefreshToken", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsActive")
                        .HasColumnType("INTEGER");

                    b.Property<string>("RefreshToken")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("TEXT")
                        .HasAnnotation("Relational:JsonPropertyName", "username");

                    b.HasKey("Id");

                    b.ToTable("UserRefreshTokens");
                });
#pragma warning restore 612, 618
        }
    }
}
