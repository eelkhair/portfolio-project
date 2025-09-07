using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIndustry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Industry_IndustryId",
                schema: "Company",
                table: "Companies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Industry",
                schema: "Company",
                table: "Industry");

            migrationBuilder.RenameTable(
                name: "Industry",
                schema: "Company",
                newName: "Industries",
                newSchema: "Company");

            migrationBuilder.AlterTable(
                name: "Industries",
                schema: "Company")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "IndustriesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "Company")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                schema: "Company",
                table: "Industries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                schema: "Company",
                table: "Industries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UId",
                schema: "Company",
                table: "Industries",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "newsequentialid()",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "RecordStatus",
                schema: "Company",
                table: "Industries",
                type: "nvarchar(25)",
                maxLength: 25,
                nullable: false,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Company",
                table: "Industries",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "Company",
                table: "Industries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "PeriodEnd",
                schema: "Company",
                table: "Industries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified))
                .Annotation("SqlServer:TemporalIsPeriodEndColumn", true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PeriodStart",
                schema: "Company",
                table: "Industries",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified))
                .Annotation("SqlServer:TemporalIsPeriodStartColumn", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Industries",
                schema: "Company",
                table: "Industries",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Industries_IndustryId",
                schema: "Company",
                table: "Companies",
                column: "IndustryId",
                principalSchema: "Company",
                principalTable: "Industries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Industries_IndustryId",
                schema: "Company",
                table: "Companies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Industries",
                schema: "Company",
                table: "Industries");

            migrationBuilder.DropColumn(
                name: "PeriodEnd",
                schema: "Company",
                table: "Industries")
                .Annotation("SqlServer:TemporalIsPeriodEndColumn", true);

            migrationBuilder.DropColumn(
                name: "PeriodStart",
                schema: "Company",
                table: "Industries")
                .Annotation("SqlServer:TemporalIsPeriodStartColumn", true);

            migrationBuilder.RenameTable(
                name: "Industries",
                schema: "Company",
                newName: "Industry",
                newSchema: "Company")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "IndustriesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "Company")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.AlterTable(
                name: "Industry",
                schema: "Company")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "IndustriesHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "Company")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                schema: "Company",
                table: "Industry",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                schema: "Company",
                table: "Industry",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<Guid>(
                name: "UId",
                schema: "Company",
                table: "Industry",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "newsequentialid()");

            migrationBuilder.AlterColumn<string>(
                name: "RecordStatus",
                schema: "Company",
                table: "Industry",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(25)",
                oldMaxLength: 25,
                oldDefaultValue: "Active");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "Company",
                table: "Industry",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "Company",
                table: "Industry",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Industry",
                schema: "Company",
                table: "Industry",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Industry_IndustryId",
                schema: "Company",
                table: "Companies",
                column: "IndustryId",
                principalSchema: "Company",
                principalTable: "Industry",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
