using System.Security.Claims;
using System.Security.Cryptography;
using Datn.PcStore.Data;
using Datn.PcStore.Hubs;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[AllowAnonymous]
[Route("support-chat")]
public class SupportChatController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<ChatHub> _hub;
    private readonly ILogger<SupportChatController> _logger;

    public SupportChatController(
        ApplicationDbContext db,
        IHubContext<ChatHub> hub,
        ILogger<SupportChatController> logger)
    {
        _db = db;
        _hub = hub;
        _logger = logger;
    }

    [HttpPost("conversations")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateConversation([FromBody] CreateChatConversationRequest? request)
    {
        if (request == null) return BadRequest(JsonError("Dữ liệu cuộc trò chuyện không hợp lệ."));

        request.Message = request.Message?.Trim() ?? string.Empty;
        var userId = CurrentUserId();
        User? user = null;

        if (userId.HasValue)
        {
            user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value);
            if (user == null) return Unauthorized(JsonError("Không tìm thấy tài khoản đang đăng nhập."));
        }
        else
        {
            request.Name = request.Name?.Trim();
            request.Email = request.Email?.Trim();
            request.Phone = request.Phone?.Trim();
            if (string.IsNullOrWhiteSpace(request.Name))
                ModelState.AddModelError(nameof(request.Name), "Vui lòng nhập tên của bạn.");
            if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
                ModelState.AddModelError(nameof(request.Email), "Vui lòng nhập email hoặc số điện thoại.");
        }

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(JsonError(FirstModelError("Thông tin chat chưa hợp lệ.")));

        var conversation = new ChatConversation
        {
            UserId = user?.Id,
            GuestName = user?.FullName ?? request.Name,
            GuestEmail = user?.Email ?? request.Email,
            GuestPhone = user?.Phone ?? request.Phone,
            AccessToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            Status = ChatConversationStatus.Open
        };
        var message = new ChatMessage
        {
            Conversation = conversation,
            SenderType = ChatSenderType.Customer,
            Message = request.Message,
            IsRead = false
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        var payload = MessagePayload(message);
        await NotifyAdmins(conversation.Id, payload);

        return Ok(new
        {
            success = true,
            conversationId = conversation.Id,
            accessToken = conversation.AccessToken,
            status = conversation.Status.ToString(),
            message = payload
        });
    }

    [HttpGet("conversations/{conversationId:int}/messages")]
    public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] string? accessToken)
    {
        var conversation = await FindOwnedConversation(conversationId, accessToken);
        if (conversation == null) return NotFound(JsonError("Không tìm thấy cuộc trò chuyện."));

        var unread = await _db.ChatMessages
            .Where(x => x.ConversationId == conversationId && x.SenderType == ChatSenderType.Admin && !x.IsRead)
            .ToListAsync();
        if (unread.Count > 0)
        {
            foreach (var item in unread) item.IsRead = true;
            await _db.SaveChangesAsync();
        }

        var messages = await _db.ChatMessages.AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new { x.Id, senderType = x.SenderType.ToString(), x.Message, x.IsRead, x.CreatedAt })
            .ToListAsync();

        return Ok(new { success = true, status = conversation.Status.ToString(), messages });
    }

    [HttpPost("conversations/{conversationId:int}/messages")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int conversationId, [FromBody] SendChatMessageRequest? request)
    {
        if (request == null) return BadRequest(JsonError("Dữ liệu tin nhắn không hợp lệ."));

        request.Message = request.Message?.Trim() ?? string.Empty;
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(JsonError(FirstModelError("Tin nhắn không được để trống.")));

        var conversation = await FindOwnedConversation(conversationId, request.AccessToken);
        if (conversation == null) return NotFound(JsonError("Không tìm thấy cuộc trò chuyện."));
        if (conversation.Status == ChatConversationStatus.Closed)
            return BadRequest(JsonError("Cuộc trò chuyện đã đóng. Vui lòng bắt đầu cuộc trò chuyện mới."));

        var message = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderType = ChatSenderType.Customer,
            Message = request.Message,
            IsRead = false
        };
        conversation.UpdatedAt = DateTime.UtcNow;
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        var payload = MessagePayload(message);
        await NotifyAdmins(conversation.Id, payload);
        return Ok(new { success = true, message = payload });
    }

    private async Task NotifyAdmins(int conversationId, object payload)
    {
        try
        {
            await _hub.Clients.Group(ChatHub.AdminGroup).SendAsync("MessageReceived", conversationId, payload);
            await _hub.Clients.Group(ChatHub.AdminGroup).SendAsync("ConversationUpdated", conversationId);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Conversation {ConversationId} was saved but realtime admin notification failed.", conversationId);
        }
    }

    private async Task<ChatConversation?> FindOwnedConversation(int id, string? accessToken)
    {
        var userId = CurrentUserId();
        return await _db.ChatConversations.FirstOrDefaultAsync(x => x.Id == id &&
            ((userId.HasValue && x.UserId == userId.Value) ||
             (!string.IsNullOrWhiteSpace(accessToken) && x.AccessToken == accessToken)));
    }

    private int? CurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private string FirstModelError(string fallback) =>
        ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault() ?? fallback;

    private static object JsonError(string error) => new { success = false, error };

    private static object MessagePayload(ChatMessage message) => new
    {
        message.Id,
        senderType = message.SenderType.ToString(),
        message.Message,
        message.IsRead,
        message.CreatedAt
    };
}
