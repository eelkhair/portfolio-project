using System.Diagnostics;
using System.Text;
using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Resumes.Parse;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.IntegrationEvents.Resume;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using AppUnit = JobBoard.AI.Application.Interfaces.Configurations.Unit;

namespace JobBoard.AI.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class ProcessResumeUploadedCommandHandlerTests
{
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly IMonolithApiClient _monolithClient = Substitute.For<IMonolithApiClient>();
    private readonly IChatService _chatService = Substitute.For<IChatService>();
    private readonly IActivityFactory _activityFactory = Substitute.For<IActivityFactory>();
    private readonly IMetricsService _metricsService = Substitute.For<IMetricsService>();
    private readonly ProcessResumeUploadedCommandHandler _sut;

    public ProcessResumeUploadedCommandHandlerTests()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        var handlerContext = new HandlerContext(loggerFactory);
        _activityFactory.StartActivity(Arg.Any<string>(), Arg.Any<ActivityKind>(), Arg.Any<ActivityContext>())
            .Returns((Activity?)null);

        _sut = new ProcessResumeUploadedCommandHandler(
            handlerContext, _blobStorage, _monolithClient, _chatService, _activityFactory, _metricsService);
    }

    private static ProcessResumeUploadedCommand CreateCommand(Guid? resumeUId = null)
    {
        var uid = resumeUId ?? Guid.NewGuid();
        var evt = new EventDto<ResumeUploadedV1Event>(
            "user1", "key1",
            new ResumeUploadedV1Event(uid, "stored-resume.txt", "resume.txt", "text/plain", "/resumes")
            {
                UserId = "user1"
            });
        return new ProcessResumeUploadedCommand(evt);
    }

    [Fact]
    public async Task HandleAsync_DownloadsBlobAndExtractsText()
    {
        // Arrange
        var command = CreateCommand();
        var resumeBytes = Encoding.UTF8.GetBytes("John Doe\nSenior Developer\njohn@test.com");

        _blobStorage.DownloadAsync("resumes", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(resumeBytes);

        // Return a valid response for all section parse calls
        _chatService.GetResponseAsync<ResumeContactParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeContactParseResponse());
        _chatService.GetResponseAsync<ResumeSkillsParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeSkillsParseResponse());
        _chatService.GetResponseAsync<ResumeWorkHistoryParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeWorkHistoryParseResponse());
        _chatService.GetResponseAsync<ResumeEducationParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeEducationParseResponse());
        _chatService.GetResponseAsync<ResumeCertificationsParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeCertificationsParseResponse());
        _chatService.GetResponseAsync<ResumeProjectsParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeProjectsParseResponse());

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        result.ShouldBe(AppUnit.Value);
        await _blobStorage.Received(1).DownloadAsync("resumes", "stored-resume.txt", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NotifiesAllSectionsCompleted()
    {
        // Arrange
        var command = CreateCommand();
        _blobStorage.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Encoding.UTF8.GetBytes("test resume"));

        _chatService.GetResponseAsync<ResumeContactParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeContactParseResponse());
        _chatService.GetResponseAsync<ResumeSkillsParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeSkillsParseResponse());
        _chatService.GetResponseAsync<ResumeWorkHistoryParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeWorkHistoryParseResponse());
        _chatService.GetResponseAsync<ResumeEducationParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeEducationParseResponse());
        _chatService.GetResponseAsync<ResumeCertificationsParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeCertificationsParseResponse());
        _chatService.GetResponseAsync<ResumeProjectsParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeProjectsParseResponse());

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        await _monolithClient.Received(1).NotifyAllSectionsCompletedAsync(
            Arg.Any<ResumeAllSectionsCompletedRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RecordsMetrics()
    {
        // Arrange
        var command = CreateCommand();
        _blobStorage.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Encoding.UTF8.GetBytes("test resume"));

        _chatService.GetResponseAsync<ResumeContactParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeContactParseResponse());
        _chatService.GetResponseAsync<ResumeSkillsParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeSkillsParseResponse());
        _chatService.GetResponseAsync<ResumeWorkHistoryParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeWorkHistoryParseResponse());
        _chatService.GetResponseAsync<ResumeEducationParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeEducationParseResponse());
        _chatService.GetResponseAsync<ResumeCertificationsParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeCertificationsParseResponse());
        _chatService.GetResponseAsync<ResumeProjectsParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new ResumeProjectsParseResponse());

        // Act
        await _sut.HandleAsync(command, CancellationToken.None);

        // Assert
        _metricsService.Received(1).RecordResumeParseDuration(Arg.Any<double>());
    }

    [Fact]
    public async Task HandleAsync_WhenBlobDownloadFails_NotifiesParseFailure()
    {
        // Arrange
        var command = CreateCommand();
        _blobStorage.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Blob not found"));

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert — should still return Unit (error is caught)
        result.ShouldBe(AppUnit.Value);
        await _monolithClient.Received(1).NotifyResumeParseFailedAsync(
            Arg.Is<ResumeParseFailedRequest>(r => r.Reason.Contains("Blob not found")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenContactParseFails_NotifiesParseFailure()
    {
        // Arrange
        var command = CreateCommand();
        _blobStorage.DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Encoding.UTF8.GetBytes("test resume"));

        // Contact parse (phase 1) failure is fatal and rethrown from ParseSectionAsync
        _chatService.GetResponseAsync<ResumeContactParseResponse>(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("LLM timeout"));

        // Act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // Assert — error is caught in outer try/catch, failure notification sent
        result.ShouldBe(AppUnit.Value);
        await _monolithClient.Received(1).NotifyResumeParseFailedAsync(
            Arg.Is<ResumeParseFailedRequest>(r => r.Reason.Contains("LLM timeout")),
            Arg.Any<CancellationToken>());
    }
}
