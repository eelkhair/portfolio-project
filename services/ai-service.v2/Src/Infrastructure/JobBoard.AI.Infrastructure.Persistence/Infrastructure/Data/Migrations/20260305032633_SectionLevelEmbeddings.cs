using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace JobBoard.AI.Infrastructure.Persistence.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class SectionLevelEmbeddings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Vector>(
            name: "certifications_vector",
            table: "resume_embeddings",
            type: "vector(1536)",
            nullable: true);

        migrationBuilder.AddColumn<Vector>(
            name: "education_vector",
            table: "resume_embeddings",
            type: "vector(1536)",
            nullable: true);

        migrationBuilder.AddColumn<Vector>(
            name: "experience_vector",
            table: "resume_embeddings",
            type: "vector(1536)",
            nullable: true);

        migrationBuilder.AddColumn<Vector>(
            name: "projects_vector",
            table: "resume_embeddings",
            type: "vector(1536)",
            nullable: true);

        migrationBuilder.AddColumn<Vector>(
            name: "skills_vector",
            table: "resume_embeddings",
            type: "vector(1536)",
            nullable: true);

        migrationBuilder.AddColumn<Vector>(
            name: "summary_vector",
            table: "resume_embeddings",
            type: "vector(1536)",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "certifications_vector",
            table: "resume_embeddings");

        migrationBuilder.DropColumn(
            name: "education_vector",
            table: "resume_embeddings");

        migrationBuilder.DropColumn(
            name: "experience_vector",
            table: "resume_embeddings");

        migrationBuilder.DropColumn(
            name: "projects_vector",
            table: "resume_embeddings");

        migrationBuilder.DropColumn(
            name: "skills_vector",
            table: "resume_embeddings");

        migrationBuilder.DropColumn(
            name: "summary_vector",
            table: "resume_embeddings");
    }
}
