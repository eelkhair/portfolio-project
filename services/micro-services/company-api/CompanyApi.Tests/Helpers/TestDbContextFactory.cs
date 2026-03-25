using CompanyApi.Infrastructure.Data;
using CompanyApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanyApi.Tests.Helpers;

public static class TestDbContextFactory
{
    private static readonly DateTime Now = DateTime.UtcNow;

    public static CompanyDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<CompanyDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new CompanyDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static async Task<(CompanyDbContext context, Industry industry)> CreateWithIndustryAsync(string? dbName = null)
    {
        var context = Create(dbName);
        var industry = CreateIndustry("Technology");
        context.Industries.Add(industry);
        await context.SaveChangesAsync();
        return (context, industry);
    }

    public static Industry CreateIndustry(string name = "Technology") => new()
    {
        UId = Guid.NewGuid(),
        Name = name,
        CreatedAt = Now,
        UpdatedAt = Now,
        CreatedBy = "seed",
        UpdatedBy = "seed"
    };

    public static Company CreateCompany(Industry industry, string name = "Test Corp", string email = "test@corp.com",
        string status = "Active", Guid? uid = null) => new()
    {
        Name = name,
        Email = email,
        Status = status,
        IndustryId = industry.Id,
        Industry = industry,
        UId = uid ?? Guid.NewGuid(),
        CreatedAt = Now,
        UpdatedAt = Now,
        CreatedBy = "test",
        UpdatedBy = "test"
    };
}
