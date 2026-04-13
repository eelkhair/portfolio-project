using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobApi.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AddDraftsTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Drafts",
            schema: "Jobs",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyId = table.Column<int>(type: "int", nullable: false),
                DraftType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                DraftStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                    .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                    .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                UId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                RecordStatus = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false, defaultValue: "Active")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Drafts", x => x.Id);
                table.ForeignKey(
                    name: "FK_Drafts_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalSchema: "Jobs",
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("SqlServer:IsTemporal", true)
            .Annotation("SqlServer:TemporalHistoryTableName", "DraftsHistory")
            .Annotation("SqlServer:TemporalHistoryTableSchema", "Jobs")
            .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
            .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

        migrationBuilder.CreateIndex(
            name: "IX_Drafts_CompanyId",
            schema: "Jobs",
            table: "Drafts",
            column: "CompanyId");

        migrationBuilder.CreateIndex(
            name: "IX_Drafts_CompanyId_DraftStatus",
            schema: "Jobs",
            table: "Drafts",
            columns: new[] { "CompanyId", "DraftStatus" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Drafts",
            schema: "Jobs")
            .Annotation("SqlServer:IsTemporal", true)
            .Annotation("SqlServer:TemporalHistoryTableName", "DraftsHistory")
            .Annotation("SqlServer:TemporalHistoryTableSchema", "Jobs")
            .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
            .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
    }
}
