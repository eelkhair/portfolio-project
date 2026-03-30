using JobBoard.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.API.Infrastructure;

public static class StartupExtensions
{
    public static async Task SeedIndustriesAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobBoardDbContext>();

        var hasIndustries = await db.Database
            .SqlQueryRaw<int>("SELECT 1 AS Value FROM [Company].[Industries]")
            .AnyAsync();

        if (!hasIndustries)
        {
            await db.Database.ExecuteSqlRawAsync(@"
                INSERT INTO [Company].[Industries] ([Id], [Name], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy])
                SELECT v.[Id], v.[Name], SYSUTCDATETIME(), 'seed', SYSUTCDATETIME(), 'seed'
                FROM (VALUES
                 ('A1B2C3D4-0001-4000-8000-000000000001', 'Technology'),
                 ('A1B2C3D4-0002-4000-8000-000000000002', 'Healthcare'),
                 ('A1B2C3D4-0003-4000-8000-000000000003', 'Finance'),
                 ('A1B2C3D4-0004-4000-8000-000000000004', 'Education'),
                 ('A1B2C3D4-0005-4000-8000-000000000005', 'Manufacturing'),
                 ('A1B2C3D4-0006-4000-8000-000000000006', 'Retail'),
                 ('A1B2C3D4-0007-4000-8000-000000000007', 'Construction'),
                 ('A1B2C3D4-0008-4000-8000-000000000008', 'Transportation & Logistics'),
                 ('A1B2C3D4-0009-4000-8000-000000000009', 'Energy'),
                 ('A1B2C3D4-000A-4000-8000-00000000000A', 'Hospitality & Tourism'),
                 ('A1B2C3D4-000B-4000-8000-00000000000B', 'Real Estate'),
                 ('A1B2C3D4-000C-4000-8000-00000000000C', 'Media & Entertainment'),
                 ('A1B2C3D4-000D-4000-8000-00000000000D', 'Agriculture'),
                 ('A1B2C3D4-000E-4000-8000-00000000000E', 'Nonprofit & Government'),
                 ('A1B2C3D4-000F-4000-8000-00000000000F', 'Insurance'),
                 ('A1B2C3D4-0010-4000-8000-000000000010', 'Other')
                ) AS v([Id], [Name])");
        }
    }
}
