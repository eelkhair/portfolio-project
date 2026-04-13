using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobBoard.Infrastructure.Persistence.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class ApplicationSections : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Certifications",
            schema: "Application",
            table: "JobApplications",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Education",
            schema: "Application",
            table: "JobApplications",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PersonalInfo",
            schema: "Application",
            table: "JobApplications",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "{}");

        migrationBuilder.AddColumn<string>(
            name: "Skills",
            schema: "Application",
            table: "JobApplications",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "[]");

        migrationBuilder.AddColumn<string>(
            name: "WorkHistory",
            schema: "Application",
            table: "JobApplications",
            type: "nvarchar(max)",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Certifications",
            schema: "Application",
            table: "JobApplications");

        migrationBuilder.DropColumn(
            name: "Education",
            schema: "Application",
            table: "JobApplications");

        migrationBuilder.DropColumn(
            name: "PersonalInfo",
            schema: "Application",
            table: "JobApplications");

        migrationBuilder.DropColumn(
            name: "Skills",
            schema: "Application",
            table: "JobApplications");

        migrationBuilder.DropColumn(
            name: "WorkHistory",
            schema: "Application",
            table: "JobApplications");
    }
}
