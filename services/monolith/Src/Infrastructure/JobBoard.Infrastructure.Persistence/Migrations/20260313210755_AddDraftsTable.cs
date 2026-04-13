using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobBoard.Infrastructure.Persistence.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddDraftsTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "Draft");

        migrationBuilder.CreateSequence<int>(
            name: "Drafts_Sequence");

        migrationBuilder.CreateTable(
            name: "Drafts",
            schema: "Draft",
            columns: table => new
            {
                InternalId = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR Drafts_Sequence"),
                CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DraftType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                DraftStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                    .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                    .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Drafts", x => x.InternalId);
            })
            .Annotation("SqlServer:IsTemporal", true)
            .Annotation("SqlServer:TemporalHistoryTableName", "DraftsHistory")
            .Annotation("SqlServer:TemporalHistoryTableSchema", "Draft")
            .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
            .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

        migrationBuilder.CreateIndex(
            name: "IX_Drafts_CompanyId",
            schema: "Draft",
            table: "Drafts",
            column: "CompanyId");

        migrationBuilder.CreateIndex(
            name: "IX_Drafts_CompanyId_DraftStatus",
            schema: "Draft",
            table: "Drafts",
            columns: new[] { "CompanyId", "DraftStatus" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Drafts",
            schema: "Draft")
            .Annotation("SqlServer:IsTemporal", true)
            .Annotation("SqlServer:TemporalHistoryTableName", "DraftsHistory")
            .Annotation("SqlServer:TemporalHistoryTableSchema", "Draft")
            .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
            .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

        migrationBuilder.DropSequence(
            name: "Drafts_Sequence");
    }
}
