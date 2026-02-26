using JobBoard.Application.Actions.Drafts.List;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Monolith.Contracts.Drafts;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Application.Handlers;

[Trait("Category", "Unit")]
public class ListDraftsQueryHandlerTests
{
    private readonly IAiServiceClient _aiServiceClient;
    private readonly ListDraftsQueryHandler _sut;

    public ListDraftsQueryHandlerTests()
    {
        var context = Substitute.For<IJobBoardQueryDbContext, ITransactionDbContext>();
        var changeTracker = new StubQueryDbContext().ChangeTracker;
        ((ITransactionDbContext)context).ChangeTracker.Returns(changeTracker);

        _aiServiceClient = Substitute.For<IAiServiceClient>();

        _sut = new ListDraftsQueryHandler(
            context,
            Substitute.For<ILogger<ListDraftsQueryHandler>>(),
            _aiServiceClient);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallAiServiceWithCompanyId()
    {
        var companyId = Guid.NewGuid();
        var query = new ListDraftsQuery { CompanyId = companyId };
        _aiServiceClient.ListDrafts(companyId, Arg.Any<CancellationToken>())
            .Returns(new List<DraftResponse>());

        await _sut.HandleAsync(query, CancellationToken.None);

        await _aiServiceClient.Received(1).ListDrafts(companyId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnDraftsFromAiService()
    {
        var companyId = Guid.NewGuid();
        var expectedDrafts = new List<DraftResponse>
        {
            new() { Id = "draft-1", Title = "Backend Engineer", Location = "Remote" },
            new() { Id = "draft-2", Title = "Frontend Engineer", Location = "NYC" }
        };
        _aiServiceClient.ListDrafts(companyId, Arg.Any<CancellationToken>())
            .Returns(expectedDrafts);

        var result = await _sut.HandleAsync(
            new ListDraftsQuery { CompanyId = companyId },
            CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].Title.ShouldBe("Backend Engineer");
        result[1].Title.ShouldBe("Frontend Engineer");
    }

    [Fact]
    public async Task HandleAsync_WhenNoDrafts_ShouldReturnEmptyList()
    {
        var companyId = Guid.NewGuid();
        _aiServiceClient.ListDrafts(companyId, Arg.Any<CancellationToken>())
            .Returns(new List<DraftResponse>());

        var result = await _sut.HandleAsync(
            new ListDraftsQuery { CompanyId = companyId },
            CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WhenAiServiceThrows_ShouldPropagateException()
    {
        var companyId = Guid.NewGuid();
        _aiServiceClient.ListDrafts(companyId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<DraftResponse>>(new HttpRequestException("AI service down")));

        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.HandleAsync(
                new ListDraftsQuery { CompanyId = companyId },
                CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnDraftWithAllFields()
    {
        var companyId = Guid.NewGuid();
        var draft = new DraftResponse
        {
            Id = "draft-abc",
            Title = "DevOps Engineer",
            AboutRole = "Manage CI/CD pipelines",
            Location = "London",
            JobType = "Full-time",
            SalaryRange = "$120k-$160k",
            Responsibilities = ["Manage deployments", "Monitor systems"],
            Qualifications = ["AWS certification", "Terraform experience"]
        };
        _aiServiceClient.ListDrafts(companyId, Arg.Any<CancellationToken>())
            .Returns(new List<DraftResponse> { draft });

        var result = await _sut.HandleAsync(
            new ListDraftsQuery { CompanyId = companyId },
            CancellationToken.None);

        var returned = result.ShouldHaveSingleItem();
        returned.Title.ShouldBe("DevOps Engineer");
        returned.AboutRole.ShouldBe("Manage CI/CD pipelines");
        returned.JobType.ShouldBe("Full-time");
        returned.SalaryRange.ShouldBe("$120k-$160k");
        returned.Responsibilities.Count.ShouldBe(2);
        returned.Qualifications.Count.ShouldBe(2);
    }
}
