using JobBoard.Infrastructure.Smtp;
using NSubstitute;
using RazorLight;
using Shouldly;

namespace JobBoard.Monolith.Tests.Unit.Infrastructure;

[Trait("Category", "Unit")]
public class EmailTemplateRendererTests
{
    private readonly IRazorLightEngine _engine;
    private readonly EmailTemplateRenderer _sut;

    public EmailTemplateRendererTests()
    {
        _engine = Substitute.For<IRazorLightEngine>();
        _sut = new EmailTemplateRenderer(_engine);
    }

    [Fact]
    public async Task RenderAsync_ShouldDelegateToRazorLightEngine()
    {
        var model = new { Name = "Test" };
        _engine.CompileRenderAsync("Welcome", model)
            .Returns("<h1>Welcome Test</h1>");

        var result = await _sut.RenderAsync("Welcome", model);

        result.ShouldBe("<h1>Welcome Test</h1>");
    }

    [Fact]
    public async Task RenderAsync_ShouldPassTemplateNameToEngine()
    {
        var model = new { Id = 1 };
        _engine.CompileRenderAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns("");

        await _sut.RenderAsync("DeadLetterNotification", model);

        await _engine.Received(1).CompileRenderAsync("DeadLetterNotification", model);
    }

    [Fact]
    public async Task RenderAsync_WhenEngineThrows_ShouldPropagateException()
    {
        _engine.CompileRenderAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromException<string>(new FileNotFoundException("Template not found")));

        await Should.ThrowAsync<FileNotFoundException>(
            () => _sut.RenderAsync("Missing", new { }));
    }
}
