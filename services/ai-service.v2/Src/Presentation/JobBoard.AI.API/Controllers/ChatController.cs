using JobBoard.AI.API.Infrastructure.Authorization;
using JobBoard.AI.Application.Actions.Chat;
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
}