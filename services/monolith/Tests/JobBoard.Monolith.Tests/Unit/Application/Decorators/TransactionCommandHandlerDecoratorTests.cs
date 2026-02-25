using JobBoard.Application.Infrastructure.Decorators;
using JobBoard.Application.Interfaces;
using JobBoard.Application.Interfaces.Configurations;
using JobBoard.Application.Interfaces.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using System.Diagnostics;
using JobBoard.Monolith.Tests.Unit.Application.Helpers;

namespace JobBoard.Monolith.Tests.Unit.Application.Decorators;

/// <summary>
/// Minimal DbContext to provide a real DatabaseFacade (CurrentTransaction is not virtual).
/// </summary>
internal class StubDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseInMemoryDatabase($"TransactionTests_{Guid.NewGuid()}");
}

[Trait("Category", "Unit")]
public class TransactionCommandHandlerDecoratorTests : IDisposable
{
    private readonly IHandler<TestCommand, string> _innerHandler;
    private readonly IActivityFactory _activityFactory;
    private readonly ITransactionDbContext _dbContext;
    private readonly IUnitOfWorkEvents _unitOfWorkEvents;
    private readonly ILogger<TransactionCommandHandlerDecorator<TestCommand, string>> _logger;
    private readonly IDbContextTransaction _transaction;
    private readonly StubDbContext _stubDbContext;
    private readonly TransactionCommandHandlerDecorator<TestCommand, string> _sut;

    public TransactionCommandHandlerDecoratorTests()
    {
        _innerHandler = Substitute.For<IHandler<TestCommand, string>>();
        _activityFactory = Substitute.For<IActivityFactory>();
        _activityFactory.StartActivity(Arg.Any<string>(), Arg.Any<ActivityKind>(), Arg.Any<ActivityContext>())
            .Returns((Activity?)null);
        _dbContext = Substitute.For<ITransactionDbContext>();
        _unitOfWorkEvents = Substitute.For<IUnitOfWorkEvents>();
        _logger = Substitute.For<ILogger<TransactionCommandHandlerDecorator<TestCommand, string>>>();
        _transaction = Substitute.For<IDbContextTransaction>();

        // Use a real DbContext so DatabaseFacade.CurrentTransaction returns null (not virtual)
        _stubDbContext = new StubDbContext();
        _dbContext.Database.Returns(_stubDbContext.Database);
        _dbContext.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_transaction);

        _sut = new TransactionCommandHandlerDecorator<TestCommand, string>(
            _innerHandler, _activityFactory, _dbContext, _unitOfWorkEvents, _logger);
    }

    public void Dispose() => _stubDbContext.Dispose();

    [Fact]
    public async Task HandleAsync_OnSuccess_ShouldCommitTransaction()
    {
        var request = new TestCommand { Name = "Test" };
        _innerHandler.HandleAsync(request, Arg.Any<CancellationToken>()).Returns("ok");

        var result = await _sut.HandleAsync(request, CancellationToken.None);

        result.ShouldBe("ok");
        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_OnSuccess_ShouldExecuteUnitOfWorkEvents()
    {
        var request = new TestCommand { Name = "Test" };
        _innerHandler.HandleAsync(request, Arg.Any<CancellationToken>()).Returns("ok");

        await _sut.HandleAsync(request, CancellationToken.None);

        await _unitOfWorkEvents.Received(1).ExecuteAndClearAsync();
    }

    [Fact]
    public async Task HandleAsync_OnException_ShouldRollbackAndClearEvents()
    {
        var request = new TestCommand { Name = "Test" };
        _innerHandler.HandleAsync(request, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("fail"));

        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.HandleAsync(request, CancellationToken.None));

        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
        _unitOfWorkEvents.Received(1).Clear();
        await _unitOfWorkEvents.DidNotReceive().ExecuteAndClearAsync();
    }

    [Fact]
    public async Task HandleAsync_WhenINoTransaction_ShouldSkipTransaction()
    {
        var noTxHandler = Substitute.For<IHandler<TestNoTransactionCommand, string>>();
        var noTxLogger = Substitute.For<ILogger<TransactionCommandHandlerDecorator<TestNoTransactionCommand, string>>>();
        noTxHandler.HandleAsync(Arg.Any<TestNoTransactionCommand>(), Arg.Any<CancellationToken>()).Returns("ok");

        var noTxDbContext = Substitute.For<ITransactionDbContext>();
        using var noTxStub = new StubDbContext();
        noTxDbContext.Database.Returns(noTxStub.Database);

        var sut = new TransactionCommandHandlerDecorator<TestNoTransactionCommand, string>(
            noTxHandler, _activityFactory, noTxDbContext, _unitOfWorkEvents, noTxLogger);

        var result = await sut.HandleAsync(new TestNoTransactionCommand(), CancellationToken.None);

        result.ShouldBe("ok");
        await noTxDbContext.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    }
}
