using JobBoard.Application.Actions.Drafts;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Infrastructure;
using JobBoard.Application.Interfaces.Messaging;
using JobBoard.Application.Interfaces.Observability;
using JobBoard.Monolith.Contracts.Drafts;
using JobBoard.Monolith.Tests.Unit.Application.Decorators;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Application.Handlers;

[Trait("Category", "Unit")]
public class GenerateDraftCommandHandlerTests
{
    private readonly IAiServiceClient _aiServiceClient;
    private readonly GenerateDraftCommandHandler _sut;

    public GenerateDraftCommandHandlerTests()
    {
        var unitOfWork = Substitute.For<IUnitOfWork, ITransactionDbContext>();
        var changeTracker = new StubDbContext().ChangeTracker;
        ((ITransactionDbContext)unitOfWork).ChangeTracker.Returns(changeTracker);

        _aiServiceClient = Substitute.For<IAiServiceClient>();

        var handlerContext = Substitute.For<IHandlerContext>();
        handlerContext.UnitOfWork.Returns(unitOfWork);
        handlerContext.OutboxPublisher.Returns(Substitute.For<IOutboxPublisher>());
        handlerContext.MetricsService.Returns(Substitute.For<IMetricsService>());
        handlerContext.UnitOfWorkEvents.Returns(Substitute.For<IUnitOfWorkEvents>());
        handlerContext.LoggerFactory.Returns(Substitute.For<ILoggerFactory>());

        _sut = new GenerateDraftCommandHandler(handlerContext, _aiServiceClient);
    }

    private static DraftGenResponse CreateDraftResponse() => new()
    {
        DraftId = "draft-001",
        Title = "Senior Software Engineer",
        AboutRole = "Join our team as a senior engineer",
        Location = "Remote",
        Responsibilities = ["Design systems", "Code reviews"],
        Qualifications = ["5+ years experience", "C# proficiency"]
    };

    [Fact]
    public async Task HandleAsync_ShouldCallAiServiceWithCompanyIdAndRequest()
    {
        var companyId = Guid.NewGuid();
        var request = new DraftGenRequest { Brief = "Need a backend engineer" };
        var command = new GenerateDraftCommand
        {
            CompanyId = companyId,
            Request = request,
            UserId = "user-123"
        };

        _aiServiceClient.GenerateDraft(companyId, request, Arg.Any<CancellationToken>())
            .Returns(CreateDraftResponse());

        await _sut.HandleAsync(command, CancellationToken.None);

        await _aiServiceClient.Received(1).GenerateDraft(
            companyId,
            request,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnDraftGenResponse()
    {
        var companyId = Guid.NewGuid();
        var expectedResponse = CreateDraftResponse();
        var command = new GenerateDraftCommand
        {
            CompanyId = companyId,
            Request = new DraftGenRequest { Brief = "Need a backend engineer" },
            UserId = "user-123"
        };

        _aiServiceClient.GenerateDraft(companyId, Arg.Any<DraftGenRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.ShouldBe(expectedResponse);
        result.DraftId.ShouldBe("draft-001");
        result.Title.ShouldBe("Senior Software Engineer");
    }

    [Fact]
    public async Task HandleAsync_ShouldImplementINoTransaction()
    {
        var command = new GenerateDraftCommand();

        command.ShouldBeAssignableTo<INoTransaction>();
    }

    [Fact]
    public async Task HandleAsync_ShouldPassRequestParametersThrough()
    {
        var companyId = Guid.NewGuid();
        var request = new DraftGenRequest
        {
            Brief = "Full-stack developer",
            RoleLevel = RoleLevel.Senior,
            Tone = Tone.Friendly,
            MaxBullets = 8,
            CompanyName = "Acme Corp",
            Location = "New York"
        };
        var command = new GenerateDraftCommand
        {
            CompanyId = companyId,
            Request = request,
            UserId = "user-123"
        };

        _aiServiceClient.GenerateDraft(Arg.Any<Guid>(), Arg.Any<DraftGenRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateDraftResponse());

        await _sut.HandleAsync(command, CancellationToken.None);

        await _aiServiceClient.Received(1).GenerateDraft(
            companyId,
            Arg.Is<DraftGenRequest>(r =>
                r.Brief == "Full-stack developer" &&
                r.RoleLevel == RoleLevel.Senior &&
                r.MaxBullets == 8),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenAiServiceThrows_ShouldPropagateException()
    {
        var command = new GenerateDraftCommand
        {
            CompanyId = Guid.NewGuid(),
            Request = new DraftGenRequest { Brief = "Test" },
            UserId = "user-123"
        };

        _aiServiceClient.GenerateDraft(Arg.Any<Guid>(), Arg.Any<DraftGenRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DraftGenResponse>(new HttpRequestException("AI service down")));

        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.HandleAsync(command, CancellationToken.None));
    }
}
