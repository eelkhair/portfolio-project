using JobBoard.Application.Actions.Companies.Industries;
using JobBoard.Application.Interfaces;
using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Application.Handlers;

[Trait("Category", "Unit")]
public class GetIndustriesQueryHandlerTests
{
    private readonly GetIndustriesQueryHandler _sut;
    private readonly TestIndustryDbContext _dbContext;

    public GetIndustriesQueryHandlerTests()
    {
        _dbContext = new TestIndustryDbContext();

        var context = Substitute.For<IJobBoardQueryDbContext, ITransactionDbContext>();
        ((ITransactionDbContext)context).ChangeTracker.Returns(_dbContext.ChangeTracker);
        context.Industries.Returns(_dbContext.Industries);

        _sut = new GetIndustriesQueryHandler(
            context,
            Substitute.For<ILogger<GetIndustriesQueryHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ShouldProjectAllIndustryFields()
    {
        var now = DateTime.UtcNow;
        var industryId = Guid.NewGuid();
        var industry = Industry.Create("Technology");
        industry.InternalId = 1;
        industry.Id = industryId;
        industry.CreatedAt = now;
        industry.CreatedBy = "admin";
        industry.UpdatedAt = now;
        industry.UpdatedBy = "admin";
        _dbContext.Industries.Add(industry);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetIndustriesQuery(), CancellationToken.None);
        var dto = result.First();

        dto.Id.ShouldBe(industryId);
        dto.Name.ShouldBe("Technology");
        dto.CreatedAt.ShouldBe(now, TimeSpan.FromSeconds(1));
        dto.CreatedBy.ShouldBe("admin");
        dto.UpdatedAt.ShouldBe(now, TimeSpan.FromSeconds(1));
        dto.UpdatedBy.ShouldBe("admin");
    }

    [Fact]
    public async Task HandleAsync_WithNoIndustries_ShouldReturnEmptyQueryable()
    {
        var result = await _sut.HandleAsync(new GetIndustriesQuery(), CancellationToken.None);

        result.ToList().ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithMultipleIndustries_ShouldReturnAll()
    {
        var industries = new[] { "Tech", "Finance", "Healthcare" };
        for (var i = 0; i < industries.Length; i++)
        {
            var industry = Industry.Create(industries[i]);
            industry.InternalId = i + 1;
            industry.Id = Guid.NewGuid();
            industry.CreatedAt = DateTime.UtcNow;
            industry.CreatedBy = "seed";
            industry.UpdatedAt = DateTime.UtcNow;
            industry.UpdatedBy = "seed";
            _dbContext.Industries.Add(industry);
        }

        await _dbContext.SaveChangesAsync();

        var result = await _sut.HandleAsync(new GetIndustriesQuery(), CancellationToken.None);

        result.ToList().Count.ShouldBe(3);
    }
}

internal class TestIndustryDbContext : DbContext
{
    public DbSet<Industry> Industries { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseInMemoryDatabase($"IndustryTests_{Guid.NewGuid()}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Industry>().HasKey(i => i.InternalId);
    }
}
