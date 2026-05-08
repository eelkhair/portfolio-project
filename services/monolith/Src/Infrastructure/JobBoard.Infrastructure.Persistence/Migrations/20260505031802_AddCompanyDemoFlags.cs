using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyDemoFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DemoExpiresAt",
                schema: "Company",
                table: "Companies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDemo",
                schema: "Company",
                table: "Companies",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DemoExpiresAt",
                schema: "Company",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "IsDemo",
                schema: "Company",
                table: "Companies");
        }
    }
}
