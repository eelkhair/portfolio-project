using System.Diagnostics;
using CompanyApi.Application.Queries;
using CompanyApi.Infrastructure.Data;
using CompanyApi.Infrastructure.Data.Entities;
using CompanyApi.Tests.Helpers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace CompanyApi.Tests.Unit.Queries;

[Trait("Category", "Unit")]
public class IndustryQueryServiceTests : IAsyncLifetime
{
    private CompanyDbContext _context = null!;
    private IndustryQueryService _sut = null!;
    private readonly ILogger<IndustryQueryService> _logger = Substitute.For<ILogger<IndustryQueryService>>();
    private readonly ActivitySource _activitySource = new("test");

    public Task InitializeAsync()
    {
        _context = TestDbContextFactory.Create();
        _sut = new IndustryQueryService(_context, _logger, _activitySource);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _context.Dispose();
        _activitySource.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ListAsync_ShouldReturnIndustries()
    {
        _context.Industries.AddRange(
            TestDbContextFactory.CreateIndustry("Technology"),
            TestDbContextFactory.CreateIndustry("Healthcare")
        );
        await _context.SaveChangesAsync();

        var result = await _sut.ListAsync(CancellationToken.None);

        result.Count.ShouldBe(2);
        result.ShouldContain(i => i.Name == "Technology");
        result.ShouldContain(i => i.Name == "Healthcare");
    }

    [Fact]
    public async Task ListAsync_WithNoIndustries_ShouldReturnEmptyList()
    {
        var result = await _sut.ListAsync(CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListAsync_ShouldMapUIdCorrectly()
    {
        var industry = TestDbContextFactory.CreateIndustry("Finance");
        var uid = industry.UId;
        _context.Industries.Add(industry);
        await _context.SaveChangesAsync();

        var result = await _sut.ListAsync(CancellationToken.None);

        result[0].UId.ShouldBe(uid);
    }
}
