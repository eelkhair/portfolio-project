using JobBoard.Infrastructure.Diagnostics.Observability;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class UnitOfWorkEventsTests
{
    private readonly UnitOfWorkEvents _sut = new();

    [Fact]
    public async Task ExecuteAndClearAsync_ShouldExecuteEnqueuedAction()
    {
        var executed = false;
        _sut.Enqueue(() =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        await _sut.ExecuteAndClearAsync();

        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAndClearAsync_ShouldExecuteMultipleActionsInOrder()
    {
        var order = new List<int>();
        _sut.Enqueue(() => { order.Add(1); return Task.CompletedTask; });
        _sut.Enqueue(() => { order.Add(2); return Task.CompletedTask; });
        _sut.Enqueue(() => { order.Add(3); return Task.CompletedTask; });

        await _sut.ExecuteAndClearAsync();

        order.ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public async Task ExecuteAndClearAsync_ShouldClearAfterExecution()
    {
        var count = 0;
        _sut.Enqueue(() => { count++; return Task.CompletedTask; });

        await _sut.ExecuteAndClearAsync();
        await _sut.ExecuteAndClearAsync(); // Second call should execute nothing

        count.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAndClearAsync_WithNoActions_ShouldNotThrow()
    {
        await Should.NotThrowAsync(() => _sut.ExecuteAndClearAsync());
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllEnqueuedActions()
    {
        var executed = false;
        _sut.Enqueue(() => { executed = true; return Task.CompletedTask; });

        _sut.Clear();

        // After clear, executing should not run the enqueued action
        await _sut.ExecuteAndClearAsync();
        executed.ShouldBeFalse();
    }

    [Fact]
    public void Clear_WithNoActions_ShouldNotThrow()
    {
        Should.NotThrow(() => _sut.Clear());
    }

    [Fact]
    public async Task Enqueue_AfterClear_ShouldOnlyExecuteNewActions()
    {
        var first = false;
        var second = false;

        _sut.Enqueue(() => { first = true; return Task.CompletedTask; });
        _sut.Clear();
        _sut.Enqueue(() => { second = true; return Task.CompletedTask; });

        await _sut.ExecuteAndClearAsync();

        first.ShouldBeFalse();
        second.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAndClearAsync_ShouldSupportAsyncActions()
    {
        var result = 0;
        _sut.Enqueue(async () =>
        {
            await Task.Delay(1);
            result = 42;
        });

        await _sut.ExecuteAndClearAsync();

        result.ShouldBe(42);
    }
}
