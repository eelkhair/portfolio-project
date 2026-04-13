using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobBoard.Infrastructure.Persistence.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class ProfileApplicationResume : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "Application");

        migrationBuilder.CreateSequence<int>(
            name: "JobApplications_Sequence");

        migrationBuilder.CreateSequence<int>(
            name: "Resumes_Sequence");

        migrationBuilder.CreateSequence<int>(
            name: "UserProfiles_Sequence");

        migrationBuilder.CreateTable(
            name: "Resumes",
            schema: "User",
            columns: table => new
            {
                InternalId = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR Resumes_Sequence"),
                UserId = table.Column<int>(type: "int", nullable: false),
                FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                OriginalFileName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                FileSize = table.Column<long>(type: "bigint", nullable: true),
                ParsedContent = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
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
                table.PrimaryKey("PK_Resumes", x => x.InternalId);
                table.ForeignKey(
                    name: "FK_Resumes_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "User",
                    principalTable: "Users",
                    principalColumn: "InternalId",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("SqlServer:IsTemporal", true)
            .Annotation("SqlServer:TemporalHistoryTableName", "ResumesHistory")
            .Annotation("SqlServer:TemporalHistoryTableSchema", "User")
            .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
            .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

        migrationBuilder.CreateTable(
            name: "UserProfiles",
            schema: "User",
            columns: table => new
            {
                InternalId = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR UserProfiles_Sequence"),
                UserId = table.Column<int>(type: "int", nullable: false),
                Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                LinkedIn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                Portfolio = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                Experience = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                Skills = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                PreferredLocation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                PreferredJobType = table.Column<int>(type: "int", nullable: true),
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
                table.PrimaryKey("PK_UserProfiles", x => x.InternalId);
                table.ForeignKey(
                    name: "FK_UserProfiles_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "User",
                    principalTable: "Users",
                    principalColumn: "InternalId",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("SqlServer:IsTemporal", true)
            .Annotation("SqlServer:TemporalHistoryTableName", "UserProfilesHistory")
            .Annotation("SqlServer:TemporalHistoryTableSchema", "User")
            .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
            .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

        migrationBuilder.CreateTable(
            name: "JobApplications",
            schema: "Application",
            columns: table => new
            {
                InternalId = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR JobApplications_Sequence"),
                JobId = table.Column<int>(type: "int", nullable: false),
                UserId = table.Column<int>(type: "int", nullable: false),
                ResumeId = table.Column<int>(type: "int", nullable: true),
                CoverLetter = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
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
                table.PrimaryKey("PK_JobApplications", x => x.InternalId);
                table.ForeignKey(
                    name: "FK_JobApplications_Jobs_JobId",
                    column: x => x.JobId,
                    principalSchema: "Job",
                    principalTable: "Jobs",
                    principalColumn: "InternalId",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_JobApplications_Resumes_ResumeId",
                    column: x => x.ResumeId,
                    principalSchema: "User",
                    principalTable: "Resumes",
                    principalColumn: "InternalId",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_JobApplications_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "User",
                    principalTable: "Users",
                    principalColumn: "InternalId");
            })
            .Annotation("SqlServer:IsTemporal", true)
            .Annotation("SqlServer:TemporalHistoryTableName", "JobApplicationsHistory")
            .Annotation("SqlServer:TemporalHistoryTableSchema", "Application")
            .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
            .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

        migrationBuilder.CreateIndex(
            name: "IX_JobApplications_JobId",
            schema: "Application",
            table: "JobApplications",
            column: "JobId");

        migrationBuilder.CreateIndex(
            name: "IX_JobApplications_JobId_UserId",
            schema: "Application",
            table: "JobApplications",
            columns: new[] { "JobId", "UserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_JobApplications_ResumeId",
            schema: "Application",
            table: "JobApplications",
            column: "ResumeId");

        migrationBuilder.CreateIndex(
            name: "IX_JobApplications_UserId",
            schema: "Application",
            table: "JobApplications",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Resumes_UserId",
            schema: "User",
            table: "Resumes",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserProfiles_UserId",
            schema: "User",
            table: "UserProfiles",
            column: "UserId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "JobApplications",
            schema: "Application")
            .Annotation("SqlServer:IsTemporal", true)
            .Annotation("SqlServer:TemporalHistoryTableName", "JobApplicationsHistory")
            .Annotation("SqlServer:TemporalHistoryTableSchema", "Application")
            .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
            .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

        migrationBuilder.DropTable(
            name: "UserProfiles",
            schema: "User")
            .Annotation("SqlServer:IsTemporal", true)
            .Annotation("SqlServer:TemporalHistoryTableName", "UserProfilesHistory")
            .Annotation("SqlServer:TemporalHistoryTableSchema", "User")
            .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
            .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

        migrationBuilder.DropTable(
            name: "Resumes",
            schema: "User")
            .Annotation("SqlServer:IsTemporal", true)
            .Annotation("SqlServer:TemporalHistoryTableName", "ResumesHistory")
            .Annotation("SqlServer:TemporalHistoryTableSchema", "User")
            .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
            .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

        migrationBuilder.DropSequence(
            name: "JobApplications_Sequence");

        migrationBuilder.DropSequence(
            name: "Resumes_Sequence");

        migrationBuilder.DropSequence(
            name: "UserProfiles_Sequence");
    }
}
