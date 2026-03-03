using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobBoard.Infrastructure.Persistence.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ResumeParseStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParseRetryCount",
                schema: "User",
                table: "Resumes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ParseStatus",
                schema: "User",
                table: "Resumes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.CreateIndex(
                name: "IX_Resumes_ParseStatus",
                schema: "User",
                table: "Resumes",
                column: "ParseStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Resumes_ParseStatus",
                schema: "User",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "ParseRetryCount",
                schema: "User",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "ParseStatus",
                schema: "User",
                table: "Resumes");
        }
    }
}
