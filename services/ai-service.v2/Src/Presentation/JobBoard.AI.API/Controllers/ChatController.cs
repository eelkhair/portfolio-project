using System.Diagnostics;
using System.Text.Json;
using JobBoard.AI.API.Infrastructure.Authorization;
using JobBoard.AI.Application.Actions.Chat;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

/// <summary>
/// Controller responsible for managing chat-related operations.
/// Provides endpoints for interaction with the chat service, including sending user messages
/// and retrieving responses.
/// </summary>
[ApiController]
[Route("chat")]
public class ChatController : BaseApiController
{
    /// <summary>
    /// Processes a chat message sent by the user and retrieves the corresponding response from the chat service.
    /// </summary>
    /// <param name="request">
    /// An instance of <see cref="ChatRequest"/> containing the user's message and an optional company identifier.
    /// </param>
    /// <returns>
    /// An <see cref="IActionResult"/> representing the result of the operation, typically a JSON response containing the chat output.
    /// </returns>
    [HttpPost]
    [StandardApiResponses]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        => await ExecuteCommandAsync(
            new ChatCommand(request.Message, request.CompanyId, request.ConversationId),
            Ok);

    /// <summary>
    /// Streams a chat response as Server-Sent Events (SSE).
    /// Each token chunk is sent as a separate SSE data event.
    /// The final event contains conversation metadata (conversationId, traceId).
    /// </summary>
    [HttpPost("stream")]
    public async Task Stream(
        [FromBody] ChatRequest request,
        [FromServices] IConversationContext conversationContext)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var command = new ChatStreamCommand(request.Message, request.CompanyId, request.ConversationId);
        var stream = await ExecuteCoreAsync(command);

        await foreach (var chunk in stream.WithCancellation(HttpContext.RequestAborted))
        {
            var payload = JsonSerializer.Serialize(new { content = chunk });
            await Response.WriteAsync($"data: {payload}\n\n", HttpContext.RequestAborted);
            await Response.Body.FlushAsync(HttpContext.RequestAborted);
        }

        var done = JsonSerializer.Serialize(new
        {
            done = true,
            conversationId = conversationContext.ConversationId,
            traceId = Activity.Current?.TraceId.ToString() ?? string.Empty
        });
        await Response.WriteAsync($"data: {done}\n\n", HttpContext.RequestAborted);
        await Response.Body.FlushAsync(HttpContext.RequestAborted);
    }
}