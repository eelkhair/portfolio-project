using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elkhair.Dev.Common.Dapr;
using JobApi.Application;
using JobApi.Infrastructure.Data;
using JobApi.Tests.Helpers;
using JobAPI.Contracts.Models.Drafts.Requests;
using JobBoard.IntegrationEvents.Draft;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace JobApi.Tests.Unit.Commands;

[Trait("Category", "Unit")]
public class DraftCommandServiceTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly IMessageSender _messageSender = Substitute.For<IMessageSender>();
    private JobDbContext _context = null!;
    private DraftCommandService _sut = null!;
    private Infrastructure.Data.Entities.Company _company = null!;

    private readonly ClaimsPrincipal _user = new(new ClaimsIdentity(
    [
        new Claim("sub", "user-123")
    ]));

    public async Task InitializeAsync()
    {
        (_context, _company) = await TestDbContextFactory.CreateWithCompanyAsync();
        _sut = new DraftCommandService(_context, _messageSender);
    }

    public Task DisposeAsync()
    {
        _context.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SaveDraftAsync_NewDraft_ShouldCreateDraft()
    {
        var request = new SaveDraftRequest
        {
            Title = "Backend Job",
            AboutRole = "Build APIs",
            Location = "Remote",
            JobType = "FullTime",
            SalaryRange = "$100k"
        };

        var result = await _sut.SaveDraftAsync(_company.UId, request, _user, CancellationToken.None, publishEvent: false);

        result.Title.ShouldBe("Backend Job");
        result.Id.ShouldNotBeNullOrWhiteSpace();

        var saved = await _context.Drafts.FirstAsync();
        saved.CompanyId.ShouldBe(_company.Id);
        saved.DraftType.ShouldBe("job");
        saved.DraftStatus.ShouldBe("generated");
    }

    [Fact]
    public async Task SaveDraftAsync_ExistingDraft_ShouldUpdateContentJson()
    {
        // Create an existing draft
        var draftUId = Guid.NewGuid();
        var existingDraft = TestDbContextFactory.CreateDraft(_company.Id, uid: draftUId);
        existingDraft.ContentJson = JsonSerializer.Serialize(new SaveDraftRequest
        {
            Id = draftUId.ToString(),
            Title = "Old Title"
        }, JsonOpts);
        _context.Drafts.Add(existingDraft);
        await _context.SaveChangesAsync();

        var request = new SaveDraftRequest
        {
            Id = draftUId.ToString(),
            Title = "Updated Title",
            AboutRole = "Updated role",
            Location = "NYC",
            JobType = "Contract"
        };

        var result = await _sut.SaveDraftAsync(_company.UId, request, _user, CancellationToken.None, publishEvent: false);

        result.Title.ShouldBe("Updated Title");
        result.Id.ShouldBe(draftUId.ToString());

        var saved = await _context.Drafts.FirstAsync();
        saved.DraftStatus.ShouldBe("generated");
        // Content should be updated
        var content = JsonSerializer.Deserialize<SaveDraftRequest>(saved.ContentJson, JsonOpts);
        content!.Title.ShouldBe("Updated Title");
    }

    [Fact]
    public async Task SaveDraftAsync_WithProvidedId_ShouldPreserveUId()
    {
        var providedUId = Guid.NewGuid();
        var request = new SaveDraftRequest
        {
            Id = providedUId.ToString(),
            Title = "New Draft"
        };

        var result = await _sut.SaveDraftAsync(_company.UId, request, _user, CancellationToken.None, publishEvent: false);

        result.Id.ShouldBe(providedUId.ToString());
    }

    [Fact]
    public async Task SaveDraftAsync_WhenPublishEventTrue_ShouldPublishDraftSavedV1Event()
    {
        var request = new SaveDraftRequest
        {
            Title = "Published Draft",
            AboutRole = "Some role",
            Location = "London",
            JobType = "FullTime",
            SalaryRange = "$90k",
            Notes = "Some notes",
            Responsibilities = ["Design", "Code"],
            Qualifications = ["C#", "Azure"]
        };

        await _sut.SaveDraftAsync(_company.UId, request, _user, CancellationToken.None, publishEvent: true);

        await _messageSender.Received(1).SendEventAsync(
            "rabbitmq.pubsub", "micro.draft-saved.v1", "user-123",
            Arg.Is<DraftSavedV1Event>(e =>
                e.Title == "Published Draft" &&
                e.CompanyUId == _company.UId &&
                e.Location == "London"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveDraftAsync_WhenPublishEventFalse_ShouldNotPublishEvent()
    {
        var request = new SaveDraftRequest { Title = "Silent Draft" };

        await _sut.SaveDraftAsync(_company.UId, request, _user, CancellationToken.None, publishEvent: false);

        await _messageSender.DidNotReceive().SendEventAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DraftSavedV1Event>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteDraftAsync_ShouldSoftDeleteDraft()
    {
        var draftUId = Guid.NewGuid();
        var draft = TestDbContextFactory.CreateDraft(_company.Id, uid: draftUId);
        _context.Drafts.Add(draft);
        await _context.SaveChangesAsync();

        await _sut.DeleteDraftAsync(draftUId, _user, CancellationToken.None, publishEvent: false);

        // SaveChangesAsync converts Remove to soft-delete (RecordStatus = "Deleted")
        // so query filter excludes it, but it still exists in DB
        (await _context.Drafts.CountAsync()).ShouldBe(0);
        var softDeleted = await _context.Drafts.IgnoreQueryFilters().FirstAsync(d => d.UId == draftUId);
        softDeleted.RecordStatus.ShouldBe("Deleted");
    }

    [Fact]
    public async Task DeleteDraftAsync_WhenDraftNotFound_ShouldThrowInvalidOperationException()
    {
        var nonExistentUId = Guid.NewGuid();

        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.DeleteDraftAsync(nonExistentUId, _user, CancellationToken.None, publishEvent: false));
    }

    [Fact]
    public async Task DeleteDraftAsync_WhenPublishEventTrue_ShouldPublishDraftDeletedV1Event()
    {
        var draftUId = Guid.NewGuid();
        var draft = TestDbContextFactory.CreateDraft(_company.Id, uid: draftUId);
        _context.Drafts.Add(draft);
        await _context.SaveChangesAsync();

        await _sut.DeleteDraftAsync(draftUId, _user, CancellationToken.None, publishEvent: true);

        await _messageSender.Received(1).SendEventAsync(
            "rabbitmq.pubsub", "micro.draft-deleted.v1", "user-123",
            Arg.Is<DraftDeletedV1Event>(e =>
                e.UId == draftUId &&
                e.CompanyUId == _company.UId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteDraftAsync_WhenPublishEventFalse_ShouldNotPublishEvent()
    {
        var draftUId = Guid.NewGuid();
        var draft = TestDbContextFactory.CreateDraft(_company.Id, uid: draftUId);
        _context.Drafts.Add(draft);
        await _context.SaveChangesAsync();

        await _sut.DeleteDraftAsync(draftUId, _user, CancellationToken.None, publishEvent: false);

        await _messageSender.DidNotReceive().SendEventAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<DraftDeletedV1Event>(), Arg.Any<CancellationToken>());
    }
}
