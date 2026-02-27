using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace JobBoard.AI.Infrastructure.Persistence.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class JobEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "draft_embeddings");

            migrationBuilder.CreateTable(
                name: "job_embeddings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    VectorData = table.Column<Vector>(type: "vector(1536)", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_embeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_embeddings_drafts_DraftId",
                        column: x => x.DraftId,
                        principalTable: "drafts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_embeddings_DraftId",
                table: "job_embeddings",
                column: "DraftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_embeddings");

            migrationBuilder.CreateTable(
                name: "draft_embeddings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DraftId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    VectorData = table.Column<Vector>(type: "vector(1536)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_draft_embeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_draft_embeddings_drafts_DraftId",
                        column: x => x.DraftId,
                        principalTable: "drafts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_draft_embeddings_DraftId",
                table: "draft_embeddings",
                column: "DraftId");
        }
    }
}
