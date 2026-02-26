using JobBoard.Application.Actions.Drafts.Rewrite;
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
public class RewriteDraftItemCommandHandlerTests
{
    private readonly IAiServiceClient _aiServiceClient;
    private readonly RewriteDraftItemCommandHandler _sut;

    public RewriteDraftItemCommandHandlerTests()
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

        _sut = new RewriteDraftItemCommandHandler(handlerContext, _aiServiceClient);
    }

    [Fact]
    public async Task HandleAsync_ShouldCallAiServiceWithRequest()
    {
        var rewriteRequest = new DraftItemRewriteRequest
        {
            Field = "title",
            Value = "Software Engineer",
            Context = new Dictionary<string, object> { ["companyName"] = "Acme" },
            Style = new Dictionary<string, object> { ["tone"] = "professional" }
        };
        var command = new RewriteDraftItemCommand
        {
            DraftItemRewriteRequest = rewriteRequest,
            UserId = "user-123"
        };
        var expectedResponse = new DraftRewriteResponse
        {
            Field = "title",
            Options = ["Senior Software Engineer", "Staff Software Engineer"],
            Meta = new DraftRewriteMetadata { Model = "gpt-4.1-mini", TotalTokens = 150 }
        };

        _aiServiceClient.RewriteItem(rewriteRequest, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        await _sut.HandleAsync(command, CancellationToken.None);

        await _aiServiceClient.Received(1).RewriteItem(
            rewriteRequest,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnRewriteResponse()
    {
        var expectedResponse = new DraftRewriteResponse
        {
            Field = "aboutRole",
            Options = ["Option A", "Option B", "Option C"],
            Meta = new DraftRewriteMetadata { Model = "gpt-4.1-mini" }
        };
        var command = new RewriteDraftItemCommand
        {
            DraftItemRewriteRequest = new DraftItemRewriteRequest
            {
                Field = "aboutRole",
                Value = "Original about role text",
                Context = new Dictionary<string, object>(),
                Style = new Dictionary<string, object>()
            },
            UserId = "user-123"
        };

        _aiServiceClient.RewriteItem(Arg.Any<DraftItemRewriteRequest>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        var result = await _sut.HandleAsync(command, CancellationToken.None);

        result.ShouldBe(expectedResponse);
        result.Field.ShouldBe("aboutRole");
        result.Options.Count.ShouldBe(3);
    }

    [Fact]
    public async Task HandleAsync_ShouldImplementINoTransaction()
    {
        var command = new RewriteDraftItemCommand();

        command.ShouldBeAssignableTo<INoTransaction>();
    }

    [Fact]
    public async Task HandleAsync_WhenAiServiceThrows_ShouldPropagateException()
    {
        var command = new RewriteDraftItemCommand
        {
            DraftItemRewriteRequest = new DraftItemRewriteRequest
            {
                Field = "title",
                Value = "Test",
                Context = new Dictionary<string, object>(),
                Style = new Dictionary<string, object>()
            },
            UserId = "user-123"
        };

        _aiServiceClient.RewriteItem(Arg.Any<DraftItemRewriteRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DraftRewriteResponse>(new HttpRequestException("Service unavailable")));

        await Should.ThrowAsync<HttpRequestException>(
            () => _sut.HandleAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ShouldPassFieldAndValueCorrectly()
    {
        var command = new RewriteDraftItemCommand
        {
            DraftItemRewriteRequest = new DraftItemRewriteRequest
            {
                Field = "qualifications",
                Value = "Must have 3 years experience",
                Context = new Dictionary<string, object> { ["role"] = "Backend Engineer" },
                Style = new Dictionary<string, object> { ["tone"] = "formal" }
            },
            UserId = "user-123"
        };

        _aiServiceClient.RewriteItem(Arg.Any<DraftItemRewriteRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DraftRewriteResponse
            {
                Field = "qualifications",
                Options = ["5+ years of backend experience"],
                Meta = new DraftRewriteMetadata()
            });

        await _sut.HandleAsync(command, CancellationToken.None);

        await _aiServiceClient.Received(1).RewriteItem(
            Arg.Is<DraftItemRewriteRequest>(r =>
                r.Field == "qualifications" &&
                r.Value == "Must have 3 years experience"),
            Arg.Any<CancellationToken>());
    }
}
