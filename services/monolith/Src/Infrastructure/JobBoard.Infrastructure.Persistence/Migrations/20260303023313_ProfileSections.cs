using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobBoard.Infrastructure.Persistence.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProfileSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Experience",
                schema: "User",
                table: "UserProfiles");

            migrationBuilder.AddColumn<string>(
                name: "Certifications",
                schema: "User",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Education",
                schema: "User",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkHistory",
                schema: "User",
                table: "UserProfiles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Certifications",
                schema: "User",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Education",
                schema: "User",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "WorkHistory",
                schema: "User",
                table: "UserProfiles");

            migrationBuilder.AddColumn<string>(
                name: "Experience",
                schema: "User",
                table: "UserProfiles",
                type: "nvarchar(3000)",
                maxLength: 3000,
                nullable: true);
        }
    }
}
