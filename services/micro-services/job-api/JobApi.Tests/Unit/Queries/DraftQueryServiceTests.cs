using System.Text.Json;
using System.Text.Json.Serialization;
using JobApi.Application;
using JobApi.Infrastructure.Data;
using JobApi.Infrastructure.Data.Entities;
using JobApi.Tests.Helpers;
using JobAPI.Contracts.Models.Drafts.Requests;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobApi.Tests.Unit.Queries;

[Trait("Category", "Unit")]
public class DraftQueryServiceTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private JobDbContext _context = null!;
    private DraftQueryService _sut = null!;
    private Company _company = null!;

    public async Task InitializeAsync()
    {
        (_context, _company) = await TestDbContextFactory.CreateWithCompanyAsync();
        _sut = new DraftQueryService(_context, Substitute.For<ILogger<DraftQueryService>>());
    }

    public Task DisposeAsync()
    {
        _context.Dispose();
        return Task.CompletedTask;
    }

    private string SerializeDraftContent(string title, string aboutRole = "", string location = "")
    {
        return JsonSerializer.Serialize(new SaveDraftRequest
        {
            Title = title,
            AboutRole = aboutRole,
            Location = location
        }, JsonOpts);
    }

    [Fact]
    public async Task ListDraftsAsync_ShouldReturnDraftsForCompany()
    {
        var draft1 = TestDbContextFactory.CreateDraft(_company.Id, SerializeDraftContent("Draft A"));
        var draft2 = TestDbContextFactory.CreateDraft(_company.Id, SerializeDraftContent("Draft B"));
        _context.Drafts.AddRange(draft1, draft2);
        await _context.SaveChangesAsync();

        var result = await _sut.ListDraftsAsync(_company.UId, CancellationToken.None);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ListDraftsAsync_ShouldDeserializeContentJson()
    {
        var draft = TestDbContextFactory.CreateDraft(_company.Id,
            SerializeDraftContent("Backend Dev", "Build APIs", "Remote"));
        _context.Drafts.Add(draft);
        await _context.SaveChangesAsync();

        var result = await _sut.ListDraftsAsync(_company.UId, CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Title.ShouldBe("Backend Dev");
        result[0].AboutRole.ShouldBe("Build APIs");
        result[0].Location.ShouldBe("Remote");
    }

    [Fact]
    public async Task ListDraftsAsync_ShouldSetIdFromDraftUId()
    {
        var draftUId = Guid.NewGuid();
        var draft = TestDbContextFactory.CreateDraft(_company.Id, SerializeDraftContent("Test"), draftUId);
        _context.Drafts.Add(draft);
        await _context.SaveChangesAsync();

        var result = await _sut.ListDraftsAsync(_company.UId, CancellationToken.None);

        result[0].Id.ShouldBe(draftUId.ToString());
    }

    [Fact]
    public async Task ListDraftsAsync_ShouldNotReturnDraftsFromOtherCompanies()
    {
        var otherCompany = TestDbContextFactory.CreateCompany("Other Corp");
        _context.Companies.Add(otherCompany);
        await _context.SaveChangesAsync();

        var myDraft = TestDbContextFactory.CreateDraft(_company.Id, SerializeDraftContent("My Draft"));
        var otherDraft = TestDbContextFactory.CreateDraft(otherCompany.Id, SerializeDraftContent("Other Draft"));
        _context.Drafts.AddRange(myDraft, otherDraft);
        await _context.SaveChangesAsync();

        var result = await _sut.ListDraftsAsync(_company.UId, CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].Title.ShouldBe("My Draft");
    }

    [Fact]
    public async Task ListDraftsAsync_ShouldReturnEmptyList_WhenNoDrafts()
    {
        var result = await _sut.ListDraftsAsync(_company.UId, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetDraftAsync_ShouldReturnDraft_WhenExists()
    {
        var draftUId = Guid.NewGuid();
        var draft = TestDbContextFactory.CreateDraft(_company.Id,
            SerializeDraftContent("Found Draft", "Role description"), draftUId);
        _context.Drafts.Add(draft);
        await _context.SaveChangesAsync();

        var result = await _sut.GetDraftAsync(draftUId, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("Found Draft");
        result.AboutRole.ShouldBe("Role description");
        result.Id.ShouldBe(draftUId.ToString());
    }

    [Fact]
    public async Task GetDraftAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _sut.GetDraftAsync(Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeNull();
    }
}
