using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AboutCompany",
                schema: "Jobs",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "EEO",
                schema: "Jobs",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PostedAt",
                schema: "Jobs",
                table: "Jobs");

            migrationBuilder.AddColumn<string>(
                name: "About",
                schema: "Jobs",
                table: "Companies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EEO",
                schema: "Jobs",
                table: "Companies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "About",
                schema: "Jobs",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "EEO",
                schema: "Jobs",
                table: "Companies");

            migrationBuilder.AddColumn<string>(
                name: "AboutCompany",
                schema: "Jobs",
                table: "Jobs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EEO",
                schema: "Jobs",
                table: "Jobs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostedAt",
                schema: "Jobs",
                table: "Jobs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
