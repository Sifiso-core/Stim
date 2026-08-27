using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stim.Api.Migrations.Identity
{
    /// <inheritdoc />
    public partial class Update_RefreshTokenWithRevocationBool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRevoked",
                schema: "identity",
                table: "RefreshTokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRevoked",
                schema: "identity",
                table: "RefreshTokens");
        }
    }
}
