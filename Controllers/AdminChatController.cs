using System.Security.Claims;
using Datn.PcStore.Data;
using Datn.PcStore.Hubs;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Controllers;

[Authorize(Roles = "Admin,SupportStaff,CustomerSupport")]
[Route("AdminChat")]
public class AdminChatController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<ChatHub> _hub;
    private readonly ILogger<AdminChatController> _logger;
    public AdminChatController(ApplicationDbContext db, IHubContext<ChatHub> hub, ILogger<AdminChatController> logger) => (_db, _hub, _logger) = (db, hub, logger);

    [HttpGet("")] public IActionResult Index() => View();

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var rows = await _db.ChatConversations.AsNoTracking().Include(x => x.User).Include(x => x.Messages)
            .OrderBy(x => x.Status == ChatConversationStatus.Closed).ThenByDescending(x => x.NeedsStaff).ThenByDescending(x => x.Priority).ThenByDescending(x => x.LastMessageAt ?? x.UpdatedAt).ToListAsync();
        var conversations = rows.Select(x => new { x.Id, name = x.CustomerName ?? x.User?.FullName ?? x.GuestName, email = x.CustomerEmail ?? x.User?.Email ?? x.GuestEmail, phone = x.CustomerPhone ?? x.User?.Phone ?? x.GuestPhone, status = x.Status.ToString(), x.CreatedAt, x.UpdatedAt, x.LastMessageAt, x.StaffUnreadCount, unreadCount = x.StaffUnreadCount, x.AssignedStaffId, x.AssignedStaffName, x.Topic, x.NeedsStaff, x.Priority, x.AutomationContext, lastMessage = x.Messages.OrderByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id).Select(m => m.Message).FirstOrDefault() }).ToList();
        return Ok(Api(true, data: new { conversations }));
    }

    [HttpGet("conversations/{id:int}/messages")]
    public async Task<IActionResult> GetMessages(int id)
    {
        var conversation = await _db.ChatConversations.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        if (conversation == null) return NotFound(Api(false, "Không tìm thấy cuộc trò chuyện."));
        var now = DateTime.UtcNow;
        var unread = await _db.ChatMessages.Where(x => x.ConversationId == id && x.SenderType == ChatSenderType.Customer && !x.IsRead).ToListAsync();
        unread.ForEach(x => { x.IsRead = true; x.ReadAt = now; });
        conversation.StaffUnreadCount = 0;
        await _db.SaveChangesAsync();
        var messages = await LoadMessages(id);
        return Ok(Api(true, data: new { conversation = ConversationPayload(conversation), messages }));
    }

    [HttpPost("conversations/{id:int}/messages")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int id, [FromBody] AdminSendChatMessageRequest? request)
    {
        if (request == null) return BadRequest(Api(false, "Dữ liệu tin nhắn không hợp lệ."));
        request.Message = request.Message?.Trim() ?? "";
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Message)) return BadRequest(Api(false, "Tin nhắn không được để trống và tối đa 1000 ký tự."));
        var conversation = await _db.ChatConversations.FirstOrDefaultAsync(x => x.Id == id);
        if (conversation == null) return NotFound(Api(false, "Không tìm thấy cuộc trò chuyện."));
        if (conversation.Status == ChatConversationStatus.Closed) return BadRequest(Api(false, "Cuộc trò chuyện đã đóng."));
        var staff = await CurrentStaff();
        if (staff == null) return Unauthorized(Api(false, "Không xác định được nhân viên đang đăng nhập."));
        AssignIfEmpty(conversation, staff);
        var message = new ChatMessage { ConversationId = id, SenderType = ChatSenderType.Staff, SenderUserId = staff.Id, SenderName = staff.FullName, Message = request.Message };
        conversation.LastMessageAt = DateTime.UtcNow;
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();
        var payload = MessagePayload(message);
        await Notify(async () => { await _hub.Clients.Group(ChatHub.ConversationGroup(id)).SendAsync("MessageReceived", id, payload); await _hub.Clients.Group(ChatHub.StaffGroup).SendAsync("ConversationUpdated", id); }, id);
        return Ok(Api(true, "Đã gửi phản hồi.", payload));
    }

    [HttpPost("conversations/{id:int}/assign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignChatConversationRequest? request)
    {
        var conversation = await _db.ChatConversations.FirstOrDefaultAsync(x => x.Id == id);
        if (conversation == null) return NotFound(Api(false, "Không tìm thấy cuộc trò chuyện."));
        var current = await CurrentStaff();
        if (current == null) return Unauthorized(Api(false, "Không xác định được nhân viên."));
        var target = current;
        if (request?.StaffId is int staffId && staffId != current.Id)
        {
            if (!User.IsInRole("Admin")) return Forbid();
            target = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == staffId && x.IsActive && (x.Role!.Name == "Admin" || x.Role.Name == "SupportStaff" || x.Role.Name == "CustomerSupport"));
            if (target == null) return BadRequest(Api(false, "Nhân viên được chọn không hợp lệ."));
        }
        conversation.AssignedStaffId = target.Id; conversation.AssignedStaffName = target.FullName;
        conversation.NeedsStaff = false;
        await _db.SaveChangesAsync();
        await Notify(() => _hub.Clients.Group(ChatHub.StaffGroup).SendAsync("ConversationUpdated", id), id);
        return Ok(Api(true, $"Đã giao hội thoại cho {target.FullName}.", new { conversation.AssignedStaffId, conversation.AssignedStaffName }));
    }

    [HttpGet("staff")]
    public async Task<IActionResult> GetStaff()
    {
        if (!User.IsInRole("Admin")) return Ok(Api(true, data: new { staff = Array.Empty<object>() }));
        var staff = await _db.Users.AsNoTracking().Where(x => x.IsActive && (x.Role!.Name == "Admin" || x.Role.Name == "SupportStaff" || x.Role.Name == "CustomerSupport")).OrderBy(x => x.FullName).Select(x => new { x.Id, x.FullName }).ToListAsync();
        return Ok(Api(true, data: new { staff }));
    }

    [HttpPost("conversations/{id:int}/close")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var conversation = await _db.ChatConversations.FirstOrDefaultAsync(x => x.Id == id);
        if (conversation == null) return NotFound(Api(false, "Không tìm thấy cuộc trò chuyện."));
        if (conversation.Status == ChatConversationStatus.Closed) return Ok(Api(true, "Hội thoại đã được đóng trước đó."));
        conversation.Status = ChatConversationStatus.Closed; conversation.ClosedAt = DateTime.UtcNow; conversation.StaffUnreadCount = 0;
        await _db.SaveChangesAsync();
        await Notify(async () => { await _hub.Clients.Group(ChatHub.ConversationGroup(id)).SendAsync("ConversationClosed", id); await _hub.Clients.Group(ChatHub.StaffGroup).SendAsync("ConversationUpdated", id); }, id);
        return Ok(Api(true, "Đã đóng cuộc trò chuyện."));
    }

    private async Task<User?> CurrentStaff() { var id = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var value) ? value : 0; return await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive); }
    private static void AssignIfEmpty(ChatConversation c, User staff) { if (c.AssignedStaffId == null) { c.AssignedStaffId = staff.Id; c.AssignedStaffName = staff.FullName; } }
    private async Task<List<object>> LoadMessages(int id)
    {
        var rows = await _db.ChatMessages.AsNoTracking().Where(x => x.ConversationId == id).OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync();
        return rows.Select(x => MessagePayload(x)).ToList();
    }
    private static object ConversationPayload(ChatConversation x) => new { x.Id, name = x.CustomerName ?? x.User?.FullName ?? x.GuestName, email = x.CustomerEmail ?? x.User?.Email ?? x.GuestEmail, phone = x.CustomerPhone ?? x.User?.Phone ?? x.GuestPhone, status = x.Status.ToString(), x.CreatedAt, x.ClosedAt, x.AssignedStaffId, x.AssignedStaffName, x.Topic, x.NeedsStaff, x.Priority, x.AutomationContext };
    private static object MessagePayload(ChatMessage x) => new { x.Id, senderType = x.SenderType == ChatSenderType.Staff ? "Staff" : x.SenderType.ToString(), x.SenderName, x.Message, x.IsSystem, x.IsRead, x.ReadAt, x.CreatedAt, x.MetadataJson };
    private async Task Notify(Func<Task> action, int id) { try { await action(); } catch (Exception e) { _logger.LogWarning(e, "Realtime notification failed for conversation {ConversationId}", id); } }
    private static object Api(bool success, string? message = null, object? data = null) => new { success, message, data };
}
