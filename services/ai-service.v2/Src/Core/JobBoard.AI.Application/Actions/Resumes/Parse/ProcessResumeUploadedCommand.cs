using System.Diagnostics;
using Elkhair.Dev.Common.Dapr;
using JobBoard.AI.Application.Actions.Base;
using JobBoard.AI.Application.Actions.Resumes.Parse.Prompts;
using JobBoard.AI.Application.Interfaces.AI;
using JobBoard.AI.Application.Interfaces.Clients;
using JobBoard.AI.Application.Interfaces.Configurations;
using JobBoard.AI.Application.Interfaces.Observability;
using JobBoard.IntegrationEvents.Resume;
using Microsoft.Extensions.Logging;

namespace JobBoard.AI.Application.Actions.Resumes.Parse;

/// <summary>
/// Orchestrates the full resume-uploaded flow with progressive parsing:
/// download blob → extract text → phase 1 (quick) → phase 2 (parallel sections) → notify completion.
/// Phase 2 sections are parsed in parallel and each is reported to the monolith via callback,
/// which streams results to the frontend via SignalR.
/// </summary>
public class ProcessResumeUploadedCommand(EventDto<ResumeUploadedV1Event> @event) : BaseCommand<Unit>, ISystemCommand
{
    public EventDto<ResumeUploadedV1Event> Event { get; set; } = @event;
}

public class ProcessResumeUploadedCommandHandler(
    IHandlerContext context,
    IBlobStorageService blobStorage,
    IMonolithApiClient monolithClient,
    IChatService chatService,
    IActivityFactory activityFactory) : BaseCommandHandler(context),
    IHandler<ProcessResumeUploadedCommand, Unit>
{
    private const string ResumeContainer = "resumes";

    public async Task<Unit> HandleAsync(ProcessResumeUploadedCommand request, CancellationToken cancellationToken)
    {
        using var activity = activityFactory.StartActivity(
            "ProcessResumeUploadedCommandHandler.HandleAsync", ActivityKind.Internal);

        var eventData = request.Event.Data;
        activity?.SetTag("resume.uid", eventData.ResumeUId);
        activity?.SetTag("resume.fileName", eventData.OriginalFileName);

        Logger.LogInformation(
            "Processing resume parse event for {ResumeUId} ({FileName})",
            eventData.ResumeUId, eventData.OriginalFileName);

        try
        {
            // Download resume from blob storage
            var blobBytes = await blobStorage.DownloadAsync(ResumeContainer, eventData.FileName, cancellationToken);
            var base64Content = Convert.ToBase64String(blobBytes);

            // Extract text once — reused for all section prompts
            var resumeText = ResumeTextExtractor.ExtractText(base64Content, eventData.ContentType);

            Logger.LogInformation("Extracted {Length} characters from resume {FileName}",
                resumeText.Length, eventData.OriginalFileName);

            var parseRequest = new ResumeParseRequest
            {
                FileName = eventData.OriginalFileName,
                ContentType = eventData.ContentType,
                FileContent = base64Content
            };

            // Phase 1: Quick parse (contact info + summary + skills)
            await ParseSectionAsync<ResumeQuickParseResponse>(
                "quick", parseRequest, resumeText,
                ParseResumeQuickPrompt.SystemPrompt,
                ParseResumeQuickPrompt.BuildUserPrompt(parseRequest),
                eventData, request.Event.UserId, result =>
                {
                    // Backfill email/phone from regex if the LLM missed them
                    if (string.IsNullOrEmpty(result.Email))
                        result.Email = ResumeTextExtractor.ExtractEmail(resumeText);
                    if (string.IsNullOrEmpty(result.Phone))
                        result.Phone = ResumeTextExtractor.ExtractPhone(resumeText);
                },
                cancellationToken);

            // Phase 2: Parallel section parsing — all 4 sections are independent
            await Task.WhenAll(
                ParseSectionAsync<ResumeWorkHistoryParseResponse>(
                    "workHistory", parseRequest, resumeText,
                    ParseResumeWorkHistoryPrompt.SystemPrompt,
                    ParseResumeWorkHistoryPrompt.BuildUserPrompt(parseRequest),
                    eventData, request.Event.UserId, null,
                    cancellationToken),

                ParseSectionAsync<ResumeEducationParseResponse>(
                    "education", parseRequest, resumeText,
                    ParseResumeEducationPrompt.SystemPrompt,
                    ParseResumeEducationPrompt.BuildUserPrompt(parseRequest),
                    eventData, request.Event.UserId, null,
                    cancellationToken),

                ParseSectionAsync<ResumeCertificationsParseResponse>(
                    "certifications", parseRequest, resumeText,
                    ParseResumeCertificationsPrompt.SystemPrompt,
                    ParseResumeCertificationsPrompt.BuildUserPrompt(parseRequest),
                    eventData, request.Event.UserId, null,
                    cancellationToken),

                ParseSectionAsync<ResumeProjectsParseResponse>(
                    "projects", parseRequest, resumeText,
                    ParseResumeProjectsPrompt.SystemPrompt,
                    ParseResumeProjectsPrompt.BuildUserPrompt(parseRequest),
                    eventData, request.Event.UserId, null,
                    cancellationToken));

            // Phase 3: Notify all sections completed (triggers embedding pipeline)
            await monolithClient.NotifyAllSectionsCompletedAsync(new ResumeAllSectionsCompletedRequest
            {
                ResumeUId = eventData.ResumeUId,
                UserId = request.Event.UserId,
                CurrentPage = eventData.CurrentPage
            }, cancellationToken);

            Logger.LogInformation("Resume {ResumeUId} all sections parsed successfully", eventData.ResumeUId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process resume {ResumeUId}", eventData.ResumeUId);

            try
            {
                await monolithClient.NotifyResumeParseFailedAsync(new ResumeParseFailedRequest
                {
                    ResumeUId = eventData.ResumeUId,
                    Reason = ex.Message,
                    UserId = request.Event.UserId,
                    CurrentPage = eventData.CurrentPage
                }, cancellationToken);
            }
            catch (Exception callbackEx)
            {
                Logger.LogError(callbackEx,
                    "Failed to notify monolith of parse failure for resume {ResumeUId}",
                    eventData.ResumeUId);
            }
        }

        return Unit.Value;
    }

    private const int MaxSectionRetries = 2;

    private async Task ParseSectionAsync<T>(
        string sectionName,
        ResumeParseRequest parseRequest,
        string resumeText,
        string systemPrompt,
        string userPromptTemplate,
        ResumeUploadedV1Event eventData,
        string userId,
        Action<T>? postProcess,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxSectionRetries; attempt++)
        {
            try
            {
                Logger.LogInformation("Parsing section {Section} for resume {ResumeUId} (attempt {Attempt}/{Max})",
                    sectionName, eventData.ResumeUId, attempt, MaxSectionRetries);

                var userPrompt = userPromptTemplate.Replace("{RESUME_TEXT}", resumeText);

                var result = await chatService.GetResponseAsync<T>(
                    systemPrompt, userPrompt, false, cancellationToken);

                postProcess?.Invoke(result);

                await monolithClient.NotifySectionParsedAsync(new ResumeSectionParsedRequest
                {
                    ResumeUId = eventData.ResumeUId,
                    Section = sectionName,
                    SectionContent = result!,
                    UserId = userId,
                    CurrentPage = eventData.CurrentPage
                }, cancellationToken);

                Logger.LogInformation("Section {Section} parsed for resume {ResumeUId}",
                    sectionName, eventData.ResumeUId);

                return; // Success — exit retry loop
            }
            catch (Exception ex)
            {
                lastException = ex;
                Logger.LogWarning(ex,
                    "Section {Section} attempt {Attempt}/{Max} failed for resume {ResumeUId}",
                    sectionName, attempt, MaxSectionRetries, eventData.ResumeUId);

                // Re-throw immediately for quick section (Phase 1 failure is fatal)
                if (sectionName == "quick")
                    throw;
            }
        }

        // All retries exhausted — notify failure
        Logger.LogError(lastException,
            "Section {Section} failed after {Max} attempts for resume {ResumeUId}",
            sectionName, MaxSectionRetries, eventData.ResumeUId);

        try
        {
            await monolithClient.NotifySectionFailedAsync(new ResumeSectionFailedRequest
            {
                ResumeUId = eventData.ResumeUId,
                Section = sectionName,
                Reason = lastException?.Message ?? "Unknown error",
                UserId = userId,
                CurrentPage = eventData.CurrentPage
            }, cancellationToken);
        }
        catch (Exception callbackEx)
        {
            Logger.LogError(callbackEx,
                "Failed to notify monolith of section {Section} failure for resume {ResumeUId}",
                sectionName, eventData.ResumeUId);
        }
    }
}
