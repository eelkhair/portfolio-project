using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobBoard.Infrastructure.Persistence.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParsedSectionsToResume : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailedSections",
                schema: "User",
                table: "Resumes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParsedSections",
                schema: "User",
                table: "Resumes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedSections",
                schema: "User",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "ParsedSections",
                schema: "User",
                table: "Resumes");
        }
    }
}
