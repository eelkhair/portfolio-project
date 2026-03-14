using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.Application.Actions.Drafts.List;
using JobBoard.Application.Interfaces;
using JobBoard.Domain.Entities;
using JobBoard.Monolith.Contracts.Drafts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Application.Handlers;

[Trait("Category", "Unit")]
public class ListDraftsQueryHandlerTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly ListDraftsQueryHandler _sut;
    private readonly DraftTestDbContext _dbContext;

    public ListDraftsQueryHandlerTests()
    {
        _dbContext = new DraftTestDbContext();
        _dbContext.Database.EnsureCreated();

        var queryContext = Substitute.For<IJobBoardQueryDbContext, ITransactionDbContext>();
        queryContext.Drafts.Returns(_dbContext.Drafts);
        var changeTracker = _dbContext.ChangeTracker;
        ((ITransactionDbContext)queryContext).ChangeTracker.Returns(changeTracker);

        _sut = new ListDraftsQueryHandler(
            queryContext,
            Substitute.For<ILogger<ListDraftsQueryHandler>>());
    }

    // TODO: Rewrite tests to seed Draft entities into the in-memory DbSet
    // and verify that the handler deserializes ContentJson correctly.
    // Commenting out old tests that relied on IAiServiceClient.ListDrafts which no longer exists.

    [Fact]
    public async Task HandleAsync_WhenNoDrafts_ShouldReturnEmptyList()
    {
        var companyId = Guid.NewGuid();

        var result = await _sut.HandleAsync(
            new ListDraftsQuery { CompanyId = companyId },
            CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnDraftsFromDatabase()
    {
        var companyId = Guid.NewGuid();
        var draftContent = new DraftResponse
        {
            Title = "Backend Engineer",
            Location = "Remote"
        };
        var contentJson = JsonSerializer.Serialize(draftContent, JsonOpts);
        var draft = Draft.Create(companyId, contentJson, 1, Guid.NewGuid());
        _dbContext.Drafts.Add(draft);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.HandleAsync(
            new ListDraftsQuery { CompanyId = companyId },
            CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Title.ShouldBe("Backend Engineer");
        result[0].Location.ShouldBe("Remote");
    }

    [Fact]
    public async Task HandleAsync_ShouldOnlyReturnDraftsForRequestedCompany()
    {
        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();

        var content1 = JsonSerializer.Serialize(new DraftResponse { Title = "Draft 1" }, JsonOpts);
        var content2 = JsonSerializer.Serialize(new DraftResponse { Title = "Draft 2" }, JsonOpts);

        _dbContext.Drafts.Add(Draft.Create(companyId, content1, 1, Guid.NewGuid()));
        _dbContext.Drafts.Add(Draft.Create(otherCompanyId, content2, 2, Guid.NewGuid()));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.HandleAsync(
            new ListDraftsQuery { CompanyId = companyId },
            CancellationToken.None);

        result.ShouldHaveSingleItem();
        result[0].Title.ShouldBe("Draft 1");
    }
}

/// <summary>
/// Minimal DbContext with Draft entity for in-memory testing.
/// </summary>
internal class DraftTestDbContext : DbContext
{
    public DbSet<Draft> Drafts { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseInMemoryDatabase($"DraftTests_{Guid.NewGuid()}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Draft>(e =>
        {
            e.HasKey(d => d.InternalId);
            e.Property(d => d.Id);
            e.Property(d => d.CompanyId);
            e.Property(d => d.ContentJson);
            e.Property(d => d.DraftType);
            e.Property(d => d.DraftStatus);
        });
    }
}
