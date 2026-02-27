using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobBoard.AI.Infrastructure.Persistence.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DraftId",
                table: "job_embeddings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DraftId",
                table: "job_embeddings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_embeddings_DraftId",
                table: "job_embeddings",
                column: "DraftId");

            migrationBuilder.AddForeignKey(
                name: "FK_job_embeddings_drafts_DraftId",
                table: "job_embeddings",
                column: "DraftId",
                principalTable: "drafts",
                principalColumn: "Id");
        }
    }
}
