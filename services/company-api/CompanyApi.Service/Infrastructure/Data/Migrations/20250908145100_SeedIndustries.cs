using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyApi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedIndustries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                                 INSERT INTO [Company].[Industries] ([Name], [CreatedAt], [UpdatedAt], [CreatedBy], [UpdatedBy])
                                 SELECT v.[Name], SYSUTCDATETIME(), SYSUTCDATETIME(), 'seed', 'seed'
                                 FROM (VALUES
                                  ('Technology'),
                                  ('Healthcare'),
                                  ('Finance'),
                                  ('Education'),
                                  ('Manufacturing'),
                                  ('Retail'),
                                  ('Construction'),
                                  ('Transportation & Logistics'),
                                  ('Energy'),
                                  ('Hospitality & Tourism'),
                                  ('Real Estate'),
                                  ('Media & Entertainment'),
                                  ('Agriculture'),
                                  ('Nonprofit & Government'),
                                  ('Insurance'),
                                  ('Other')
                                 ) AS v([Name])
                                 WHERE NOT EXISTS (
                                     SELECT 1 FROM [Company].[Industries] i WHERE i.[Name] = v.[Name]
                                 );
                                 ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                    DELETE FROM [Company].[Industries]
                    WHERE [CreatedBy] = 'seed'
                    AND [UpdatedBy] = 'seed'
                    AND [Name] IN (
                     'Technology',
                     'Healthcare',
                     'Finance',
                     'Education',
                     'Manufacturing',
                     'Retail',
                     'Construction',
                     'Transportation & Logistics',
                     'Energy',
                     'Hospitality & Tourism',
                     'Real Estate',
                     'Media & Entertainment',
                     'Agriculture',
                     'Nonprofit & Government',
                     'Insurance',
                     'Other'
                    );
                    ");
        }
    }
}
