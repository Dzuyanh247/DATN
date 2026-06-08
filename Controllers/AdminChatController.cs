using Datn.PcStore.Data;
using Datn.PcStore.Hubs;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin")]
[Route("AdminChat")]
public class AdminChatController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<ChatHub> _hub;

    public AdminChatController(ApplicationDbContext db, IHubContext<ChatHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var conversations = await _db.ChatConversations.AsNoTracking()
            .OrderBy(x => x.Status == ChatConversationStatus.Closed)
            .ThenByDescending(x => x.UpdatedAt)
            .Select(x => new
            {
                x.Id,
                name = x.User != null ? x.User.FullName : x.GuestName,
                email = x.User != null ? x.User.Email : x.GuestEmail,
                phone = x.User != null ? x.User.Phone : x.GuestPhone,
                status = x.Status.ToString(),
                x.CreatedAt,
                x.UpdatedAt,
                unreadCount = x.Messages.Count(m => m.SenderType == ChatSenderType.Customer && !m.IsRead),
                lastMessage = x.Messages.OrderByDescending(m => m.CreatedAt).Select(m => m.Message).FirstOrDefault()
            })
            .ToListAsync();

        return Ok(new { success = true, conversations });
    }

    [HttpGet("conversations/{conversationId:int}/messages")]
    public async Task<IActionResult> GetMessages(int conversationId)
    {
        var conversation = await _db.ChatConversations
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == conversationId);
        if (conversation == null) return NotFound(new { success = false, message = "Không tìm thấy cuộc trò chuyện." });

        var unread = await _db.ChatMessages
            .Where(x => x.ConversationId == conversationId && x.SenderType == ChatSenderType.Customer && !x.IsRead)
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

        return Ok(new
        {
            success = true,
            conversation = new
            {
                conversation.Id,
                name = conversation.User?.FullName ?? conversation.GuestName,
                email = conversation.User?.Email ?? conversation.GuestEmail,
                phone = conversation.User?.Phone ?? conversation.GuestPhone,
                status = conversation.Status.ToString(),
                conversation.CreatedAt
            },
            messages
        });
    }

    [HttpPost("conversations/{conversationId:int}/messages")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int conversationId, [FromBody] AdminSendChatMessageRequest request)
    {
        request.Message = request.Message?.Trim() ?? string.Empty;
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { success = false, message = "Tin nhắn không được để trống và tối đa 1000 ký tự." });

        var conversation = await _db.ChatConversations.FirstOrDefaultAsync(x => x.Id == conversationId);
        if (conversation == null) return NotFound(new { success = false, message = "Không tìm thấy cuộc trò chuyện." });
        if (conversation.Status == ChatConversationStatus.Closed)
            return BadRequest(new { success = false, message = "Cuộc trò chuyện đã đóng." });

        var message = new ChatMessage
        {
            ConversationId = conversationId,
            SenderType = ChatSenderType.Admin,
            Message = request.Message,
            IsRead = false
        };
        conversation.UpdatedAt = DateTime.UtcNow;
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        var payload = new
        {
            message.Id,
            senderType = message.SenderType.ToString(),
            message.Message,
            message.IsRead,
            message.CreatedAt
        };
        await _hub.Clients.Group(ChatHub.ConversationGroup(conversationId)).SendAsync("MessageReceived", conversationId, payload);
        await _hub.Clients.Group(ChatHub.AdminGroup).SendAsync("ConversationUpdated", conversationId);
        return Ok(new { success = true, message = payload });
    }

    [HttpPost("conversations/{conversationId:int}/close")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseConversation(int conversationId)
    {
        var conversation = await _db.ChatConversations.FirstOrDefaultAsync(x => x.Id == conversationId);
        if (conversation == null) return NotFound(new { success = false, message = "Không tìm thấy cuộc trò chuyện." });

        conversation.Status = ChatConversationStatus.Closed;
        conversation.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(ChatHub.ConversationGroup(conversationId)).SendAsync("ConversationClosed", conversationId);
        await _hub.Clients.Group(ChatHub.AdminGroup).SendAsync("ConversationUpdated", conversationId);
        return Ok(new { success = true, message = "Đã đóng cuộc trò chuyện." });
    }
}
