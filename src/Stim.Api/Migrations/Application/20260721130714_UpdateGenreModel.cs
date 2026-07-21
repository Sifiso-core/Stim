using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stim.Api.Migrations.Application
{
    /// <inheritdoc />
    public partial class UpdateGenreModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                schema: "stim",
                table: "Genres",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "stim",
                table: "Genres",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                schema: "stim",
                table: "Genres",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAtUtc",
                schema: "stim",
                table: "Genres",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                schema: "stim",
                table: "Genres");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "stim",
                table: "Genres");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                schema: "stim",
                table: "Genres");

            migrationBuilder.DropColumn(
                name: "LastUpdatedAtUtc",
                schema: "stim",
                table: "Genres");
        }
    }
}
