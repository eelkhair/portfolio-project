using JobBoard.Application.Actions.Companies.Get;
using JobBoard.Application.Interfaces;
using JobBoard.Domain.Aggregates;
using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Application.Handlers;

[Trait("Category", "Unit")]
public class GetCompaniesQueryHandlerTests
{
    private readonly GetCompaniesQueryHandler _sut;
    private readonly TestCompanyDbContext _dbContext;

    public GetCompaniesQueryHandlerTests()
    {
        _dbContext = new TestCompanyDbContext();

        var context = Substitute.For<IJobBoardDbContext, ITransactionDbContext>();
        ((ITransactionDbContext)context).ChangeTracker.Returns(_dbContext.ChangeTracker);
        context.Companies.Returns(_dbContext.Companies);

        _sut = new GetCompaniesQueryHandler(
            context,
            Substitute.For<ILogger<GetCompaniesQueryHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ShouldProjectAllCompanyFields()
    {
        var industry = Industry.Create("Tech");
        industry.InternalId = 1;
        industry.Id = Guid.NewGuid();
        industry.CreatedAt = DateTime.UtcNow;
        industry.CreatedBy = "seed";
        industry.UpdatedAt = DateTime.UtcNow;
        industry.UpdatedBy = "seed";
        _dbContext.Industries.Add(industry);

        var companyId = Guid.NewGuid();
        var company = Company.Create(new CompanyInput(
            InternalId: 1, Id: companyId, Name: "TestCorp", Email: "test@corp.com",
            Status: "Active", IndustryId: industry.InternalId,
            Description: "A test company", Website: "https://test.com",
            Logo: "logo.png", Phone: "+1234567890", About: "About us",
            EEO: "Equal Opportunity", Founded: new DateTime(2020, 1, 1),
            Size: "50-100", CreatedAt: DateTime.UtcNow, CreatedBy: "admin"));
        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetCompaniesQuery(), CancellationToken.None);
        var dto = result.First();

        dto.Name.ShouldBe("TestCorp");
        dto.Email.ShouldBe("test@corp.com");
        dto.Description.ShouldBe("A test company");
        dto.Website.ShouldBe("https://test.com");
        dto.Logo.ShouldBe("logo.png");
        dto.Phone.ShouldBe("+1234567890");
        dto.About.ShouldBe("About us");
        dto.EEO.ShouldBe("Equal Opportunity");
        dto.Founded.ShouldBe(new DateTime(2020, 1, 1));
        dto.Size.ShouldBe("50-100");
        dto.Status.ShouldBe("Active");
        dto.Id.ShouldBe(companyId);
    }

    [Fact]
    public async Task HandleAsync_ShouldProjectNestedIndustry()
    {
        var industry = Industry.Create("Finance");
        industry.InternalId = 10;
        industry.Id = Guid.NewGuid();
        industry.CreatedAt = DateTime.UtcNow;
        industry.CreatedBy = "seed";
        industry.UpdatedAt = DateTime.UtcNow;
        industry.UpdatedBy = "seed";
        _dbContext.Industries.Add(industry);

        var company = Company.Create(new CompanyInput(
            InternalId: 10, Id: Guid.NewGuid(), Name: "FinCorp", Email: "fin@corp.com",
            Status: "Active", IndustryId: industry.InternalId,
            CreatedAt: DateTime.UtcNow, CreatedBy: "seed"));
        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetCompaniesQuery(), CancellationToken.None);
        var dto = result.First();

        dto.Industry.ShouldNotBeNull();
        dto.Industry.Name.ShouldBe("Finance");
        dto.Industry.Id.ShouldBe(industry.Id);
        dto.IndustryUId.ShouldBe(industry.Id);
    }

    [Fact]
    public async Task HandleAsync_WithNoCompanies_ShouldReturnEmptyQueryable()
    {
        var result = await _sut.HandleAsync(new GetCompaniesQuery(), CancellationToken.None);

        result.ToList().ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldProjectAuditFields()
    {
        var now = DateTime.UtcNow;
        var industry = Industry.Create("Health");
        industry.InternalId = 20;
        industry.Id = Guid.NewGuid();
        industry.CreatedAt = now;
        industry.CreatedBy = "admin";
        industry.UpdatedAt = now;
        industry.UpdatedBy = "admin";
        _dbContext.Industries.Add(industry);

        var company = Company.Create(new CompanyInput(
            InternalId: 20, Id: Guid.NewGuid(), Name: "HealthCo", Email: "health@co.com",
            Status: "Active", IndustryId: industry.InternalId,
            CreatedAt: now, CreatedBy: "admin"));
        company.UpdatedAt = now;
        company.UpdatedBy = "admin";
        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetCompaniesQuery(), CancellationToken.None);
        var dto = result.First();

        dto.CreatedBy.ShouldBe("admin");
        dto.UpdatedBy.ShouldBe("admin");
        dto.CreatedAt.ShouldBe(now, TimeSpan.FromSeconds(1));
        dto.UpdatedAt.ShouldBe(now, TimeSpan.FromSeconds(1));
    }
}

internal class TestCompanyDbContext : DbContext
{
    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<Industry> Industries { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseInMemoryDatabase($"CompanyTests_{Guid.NewGuid()}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>().HasKey(c => c.InternalId);
        modelBuilder.Entity<Company>().HasOne(c => c.Industry)
            .WithMany().HasForeignKey(c => c.IndustryId);
        modelBuilder.Entity<Industry>().HasKey(i => i.InternalId);
    }
}
