using JobBoard.AI.API.Infrastructure.Authorization;
using JobBoard.AI.Application.Actions.Chat;
using JobBoard.AI.Application.Interfaces.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobBoard.AI.API.Controllers;

/// <summary>
/// Chat endpoints scoped by role: admin, company-admin, and public (applicant).
/// Each endpoint resolves a different tool registry server-side.
/// </summary>
[ApiController]
[Route("chat")]
public class ChatController : BaseApiController
{
    /// <summary>
    /// System admin chat — full tool access including system mode switching.
    /// </summary>
    [HttpPost("system")]
    [Authorize(Policy = "SystemAdmin")]
    [StandardApiResponses]
    public async Task<IActionResult> SystemChat([FromBody] ChatRequest request)
        => await ExecuteCommandAsync(
            new ChatCommand(request.Message, request.CompanyId, request.ConversationId,
                scope: ChatScope.SystemAdmin),
            Ok);

    /// <summary>
    /// Admin chat — full tool access (companies, jobs, system).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "AdminChat")]
    [StandardApiResponses]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        => await ExecuteCommandAsync(
            new ChatCommand(request.Message, request.CompanyId, request.ConversationId,
                scope: ChatScope.Admin),
            Ok);

    /// <summary>
    /// Company admin chat — company-scoped tools (no company creation).
    /// </summary>
    [HttpPost("company")]
    [Authorize(Policy = "CompanyAdminChat")]
    [StandardApiResponses]
    public async Task<IActionResult> CompanyChat([FromBody] ChatRequest request)
        => await ExecuteCommandAsync(
            new ChatCommand(request.Message, request.CompanyId, request.ConversationId,
                scope: ChatScope.CompanyAdmin),
            Ok);

    /// <summary>
    /// Public chat for applicants — no admin tools.
    /// </summary>
    [HttpPost("public")]
    [Authorize(Policy = "PublicChat")]
    [StandardApiResponses]
    public async Task<IActionResult> PublicChat([FromBody] PublicChatRequest request)
        => await ExecuteCommandAsync(
            new ChatCommand(request.Message, companyId: null, request.ConversationId,
                scope: ChatScope.Public),
            Ok);

    /// <summary>
    /// Anonymous demo chat for landing-page visitors. Walks the visitor through
    /// creating a real demo company that auto-deletes after 1 hour. Single tool
    /// only: create_demo_company.
    /// </summary>
    [HttpPost("demo")]
    [AllowAnonymous]
    [StandardApiResponses]
    public async Task<IActionResult> DemoChat(
        [FromBody] PublicChatRequest request,
        [FromServices] IUserAccessor userAccessor)
    {
        // The UserContextCommandHandlerDecorator throws 401 when userAccessor.UserId is
        // empty; this endpoint is anonymous so no JWT populates it. Stamp a synthetic
        // visitor id so the decorator passes and ChatService has something to bucket
        // conversations against.
        userAccessor.UserId = "demo-anonymous";
        userAccessor.FirstName = "Demo";
        userAccessor.LastName = "Visitor";
        userAccessor.Email = "demo-visitor@demo.elkhair.tech";
        userAccessor.Roles = ["Demo"];

        return await ExecuteCommandAsync(
            new ChatCommand(request.Message, companyId: null, request.ConversationId,
                scope: ChatScope.Demo),
            Ok);
    }
}
