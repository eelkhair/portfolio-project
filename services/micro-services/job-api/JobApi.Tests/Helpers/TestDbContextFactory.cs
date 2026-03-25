using JobApi.Infrastructure.Data;
using JobApi.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobApi.Tests.Helpers;

public static class TestDbContextFactory
{
    private static readonly DateTime Now = DateTime.UtcNow;

    public static JobDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<JobDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new JobDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static async Task<(JobDbContext context, Company company)> CreateWithCompanyAsync(
        string? dbName = null, string companyName = "Test Corp", Guid? companyUId = null)
    {
        var context = Create(dbName);
        var company = CreateCompany(companyName, companyUId);
        context.Companies.Add(company);
        await context.SaveChangesAsync();
        return (context, company);
    }

    public static Company CreateCompany(string name = "Test Corp", Guid? uid = null) => new()
    {
        Name = name,
        UId = uid ?? Guid.NewGuid(),
        CreatedAt = Now,
        UpdatedAt = Now,
        CreatedBy = "test",
        UpdatedBy = "test"
    };

    public static Job CreateJob(int companyId, string title = "Software Engineer", Guid? uid = null) => new()
    {
        Title = title,
        CompanyId = companyId,
        Location = "Remote",
        JobType = JobAPI.Contracts.Enums.JobType.FullTime,
        AboutRole = "Build great software",
        SalaryRange = "$100k-$150k",
        UId = uid ?? Guid.NewGuid(),
        CreatedAt = Now,
        UpdatedAt = Now,
        CreatedBy = "test",
        UpdatedBy = "test"
    };

    public static Draft CreateDraft(int companyId, string contentJson = "{}", Guid? uid = null) => new()
    {
        CompanyId = companyId,
        DraftType = "job",
        DraftStatus = "generated",
        ContentJson = contentJson,
        UId = uid ?? Guid.NewGuid(),
        CreatedAt = Now,
        UpdatedAt = Now,
        CreatedBy = "test",
        UpdatedBy = "test"
    };
}
