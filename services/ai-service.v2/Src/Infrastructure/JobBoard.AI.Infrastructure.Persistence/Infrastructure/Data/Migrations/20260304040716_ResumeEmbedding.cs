using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace JobBoard.AI.Infrastructure.Persistence.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class ResumeEmbedding : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "JobCandidate",
            columns: table => new
            {
                JobId = table.Column<Guid>(type: "uuid", nullable: false),
                Similarity = table.Column<double>(type: "double precision", nullable: false),
                Rank = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
            });

        migrationBuilder.CreateTable(
            name: "resume_embeddings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ResumeUId = table.Column<Guid>(type: "uuid", nullable: false),
                VectorData = table.Column<Vector>(type: "vector(1536)", nullable: false),
                Provider = table.Column<string>(type: "text", nullable: false),
                Model = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_resume_embeddings", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "JobCandidate");

        migrationBuilder.DropTable(
            name: "resume_embeddings");
    }
}
