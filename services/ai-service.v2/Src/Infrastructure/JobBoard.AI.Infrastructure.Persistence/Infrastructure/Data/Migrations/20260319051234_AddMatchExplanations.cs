using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobBoard.AI.Infrastructure.Persistence.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddMatchExplanations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS drafts;");

        migrationBuilder.CreateTable(
            name: "match_explanations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ResumeUId = table.Column<Guid>(type: "uuid", nullable: false),
                JobId = table.Column<Guid>(type: "uuid", nullable: false),
                Summary = table.Column<string>(type: "text", nullable: false),
                DetailsJson = table.Column<string>(type: "text", nullable: false),
                GapsJson = table.Column<string>(type: "text", nullable: false),
                Provider = table.Column<string>(type: "text", nullable: false),
                Model = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_match_explanations", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_match_explanations_JobId",
            table: "match_explanations",
            column: "JobId");

        migrationBuilder.CreateIndex(
            name: "IX_match_explanations_ResumeUId",
            table: "match_explanations",
            column: "ResumeUId");

        migrationBuilder.CreateIndex(
            name: "IX_match_explanations_ResumeUId_JobId",
            table: "match_explanations",
            columns: new[] { "ResumeUId", "JobId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "match_explanations");

        migrationBuilder.CreateTable(
            name: "drafts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                ContentJson = table.Column<string>(type: "jsonb", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                Type = table.Column<string>(type: "text", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_drafts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "JobCandidate",
            columns: table => new
            {
                JobId = table.Column<Guid>(type: "uuid", nullable: false),
                Rank = table.Column<int>(type: "integer", nullable: false),
                Similarity = table.Column<double>(type: "double precision", nullable: false)
            },
            constraints: table =>
            {
            });
    }
}
