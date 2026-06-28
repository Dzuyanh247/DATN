using Datn.PcStore.Services;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Datn.PcStore.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/chat/ai")]
public class AiChatController : ControllerBase
{
    private readonly IAiChatService _aiChat;
    public AiChatController(IAiChatService aiChat) => _aiChat = aiChat;

    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] AiChatRequest? request, CancellationToken cancellationToken)
    {
        if (request == null) return BadRequest(new { success = false, reply = "Dữ liệu chat không hợp lệ.", suggestedProducts = Array.Empty<object>() });
        if (!string.Equals(request.Mode, "ai", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { success = false, reply = "Chế độ chat không hợp lệ.", suggestedProducts = Array.Empty<object>() });

        Console.WriteLine($"AI_TRACE AiChatController.Ask BEFORE_ASK sessionId={request.SessionId ?? "none"} message={request.Message}");
        var result = await _aiChat.AskAsync(request.Message, request.SessionId, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
        return Ok(new
        {
            success = result.Success,
            reply = result.Reply,
            requestId = result.RequestId,
            shouldAttachProductCards = result.AttachProductCards,
            suggestedProducts = result.AttachProductCards ? result.SuggestedProducts.Select(AiChatProductDto.From) : Array.Empty<AiChatProductDto>()
        });
    }
}
