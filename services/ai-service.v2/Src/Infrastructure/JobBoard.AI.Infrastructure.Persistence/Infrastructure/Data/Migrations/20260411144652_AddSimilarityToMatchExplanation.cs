using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobBoard.AI.Infrastructure.Persistence.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSimilarityToMatchExplanation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Similarity",
                table: "match_explanations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Similarity",
                table: "match_explanations");
        }
    }
}
