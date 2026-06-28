using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Datn.PcStore.Data;
using Datn.PcStore.Constants;
using Datn.PcStore.Hubs;
using Datn.PcStore.Models;
using Datn.PcStore.Services;
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
    private readonly ISupportChatAutomationService _automation;
    private readonly IAiChatService _aiChat;

    public SupportChatController(ApplicationDbContext db, IHubContext<ChatHub> hub, ILogger<SupportChatController> logger, ISupportChatAutomationService automation, IAiChatService aiChat)
        => (_db, _hub, _logger, _automation, _aiChat) = (db, hub, logger, automation, aiChat);

    [HttpPost("conversations")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateConversation([FromBody] CreateChatConversationRequest? request)
    {
        if (request == null) return BadRequest(Api(false, "Dữ liệu cuộc trò chuyện không hợp lệ."));
        Normalize(request);
        var userId = CurrentUserId();
        var user = userId.HasValue ? await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId) : null;
        if (userId.HasValue && user == null) return Unauthorized(Api(false, "Không tìm thấy tài khoản đang đăng nhập."));

        if (user == null)
        {
            if (string.IsNullOrWhiteSpace(request.Name)) ModelState.AddModelError(nameof(request.Name), "Vui lòng nhập tên của bạn.");
            if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
                ModelState.AddModelError(nameof(request.Email), "Vui lòng nhập email hoặc số điện thoại.");
        }
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(Api(false, FirstModelError("Thông tin chat chưa hợp lệ.")));

        var guestId = user == null ? NormalizeGuestId(request.GuestId) : null;
        var conversation = await FindOpenConversation(user?.Id, guestId, request.Email, request.Phone);
        var isNew = conversation == null;
        if (conversation == null)
        {
            conversation = new ChatConversation
            {
                UserId = user?.Id,
                CustomerId = user?.Id,
                GuestId = guestId,
                GuestName = user?.FullName ?? request.Name,
                GuestEmail = user?.Email ?? request.Email,
                GuestPhone = user?.Phone ?? request.Phone,
                CustomerName = user?.FullName ?? request.Name,
                CustomerEmail = user?.Email ?? request.Email,
                CustomerPhone = user?.Phone ?? request.Phone,
                AccessToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                Status = ChatConversationStatus.Open
            };
            _db.ChatConversations.Add(conversation);
            _db.ChatMessages.Add(new ChatMessage
            {
                Conversation = conversation, SenderType = ChatSenderType.System, SenderName = "KKSHOP",
                Message = SupportChatDefaults.GreetingMessage, IsSystem = true, IsRead = true, ReadAt = DateTime.UtcNow
            });
        }

        var customerMessage = new ChatMessage
        {
            Conversation = conversation, SenderType = ChatSenderType.Customer, SenderUserId = user?.Id,
            SenderName = user?.FullName ?? request.Name ?? "Khách hàng", Message = request.Message, IsRead = false
        };
        _db.ChatMessages.Add(customerMessage);
        conversation.LastMessageAt = DateTime.UtcNow;
        conversation.StaffUnreadCount++;
        await _db.SaveChangesAsync();

        await NotifyStaff(conversation.Id, MessagePayload(customerMessage));
        if (!IsQuickActionLabel(request.Message) && !conversation.NeedsStaff)
        {
            var aiResult = await _aiChat.AskAsync(request.Message, conversation.Id.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString());
            var aiMessage = AddAiMessage(conversation, aiResult.Reply, aiResult.SuggestedProducts, aiResult.AttachProductCards, request.RequestId ?? aiResult.RequestId);
            await _db.SaveChangesAsync();
            await NotifyConversation(conversation.Id, MessagePayload(aiMessage));
        }
        var messages = await LoadMessages(conversation.Id);
        return Ok(Api(true, isNew ? "Đã bắt đầu cuộc trò chuyện." : "Đã tiếp tục cuộc trò chuyện đang mở.", new
        {
            conversationId = conversation.Id, accessToken = conversation.AccessToken,
            status = conversation.Status.ToString(), messages
        }));
    }

    [HttpGet("conversations/{conversationId:int}/messages")]
    public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] string? accessToken)
    {
        var conversation = await FindOwnedConversation(conversationId, accessToken);
        if (conversation == null) return NotFound(Api(false, "Không tìm thấy cuộc trò chuyện."));
        var now = DateTime.UtcNow;
        var unread = await _db.ChatMessages.Where(x => x.ConversationId == conversationId && x.SenderType == ChatSenderType.Staff && !x.IsRead).ToListAsync();
        unread.ForEach(x => { x.IsRead = true; x.ReadAt = now; });
        if (unread.Count > 0) await _db.SaveChangesAsync();
        return Ok(Api(true, data: new { status = conversation.Status.ToString(), messages = await LoadMessages(conversationId) }));
    }

    [HttpPost("conversations/{conversationId:int}/messages")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int conversationId, [FromBody] SendChatMessageRequest? request)
    {
        if (request == null) return BadRequest(Api(false, "Dữ liệu tin nhắn không hợp lệ."));
        request.Message = request.Message?.Trim() ?? "";
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request.Message)) return BadRequest(Api(false, "Tin nhắn không được để trống và tối đa 1000 ký tự."));
        var conversation = await FindOwnedConversation(conversationId, request.AccessToken);
        if (conversation == null) return NotFound(Api(false, "Không tìm thấy cuộc trò chuyện."));
        if (conversation.Status == ChatConversationStatus.Closed) return BadRequest(Api(false, "Cuộc trò chuyện đã đóng. Vui lòng bắt đầu cuộc trò chuyện mới."));

        var message = new ChatMessage { ConversationId = conversation.Id, SenderType = ChatSenderType.Customer, SenderUserId = CurrentUserId(), SenderName = conversation.CustomerName ?? conversation.GuestName ?? "Khách hàng", Message = request.Message };
        conversation.LastMessageAt = DateTime.UtcNow;
        conversation.StaffUnreadCount++;
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();
        var payload = MessagePayload(message);
        await NotifyStaff(conversation.Id, payload);
        var automated = await _automation.TryHandleTextAsync(conversation, CurrentUserId(), request.Message);
        if (automated != null)
        {
            await _db.SaveChangesAsync();
            foreach (var systemMessage in automated.Messages)
                await NotifyConversation(conversation.Id, MessagePayload(systemMessage));
            return Ok(Api(true, "Đã gửi tin nhắn.", new
            {
                customerMessage = payload,
                automation = AutomationPayload(automated)
            }));
        }
        if (!conversation.NeedsStaff)
        {
            var aiResult = await _aiChat.AskAsync(request.Message, conversation.Id.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString(), HttpContext.RequestAborted);
            var aiMessage = AddAiMessage(conversation, aiResult.Reply, aiResult.SuggestedProducts, aiResult.AttachProductCards, request.RequestId ?? aiResult.RequestId);
            await _db.SaveChangesAsync();
            await NotifyConversation(conversation.Id, MessagePayload(aiMessage));
            return Ok(Api(true, "Đã gửi tin nhắn.", new { customerMessage = payload, aiMessage = MessagePayload(aiMessage) }));
        }
        return Ok(Api(true, "Đã gửi tin nhắn.", new { customerMessage = payload }));
    }

    [HttpPost("quick-action")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickAction([FromBody] SupportChatQuickActionRequest? request)
    {
        if (request == null || !ModelState.IsValid) return BadRequest(Api(false, "Quick action không hợp lệ."));
        var conversation = await FindOwnedConversation(request.ConversationId, request.AccessToken);
        if (conversation == null) return NotFound(Api(false, "Không tìm thấy cuộc trò chuyện."));
        if (conversation.Status == ChatConversationStatus.Closed) return BadRequest(Api(false, "Cuộc trò chuyện đã đóng."));
        var userId = CurrentUserId();
        var quickLabel = QuickActionLabel(request.ActionType);
        ChatMessage? customerChoice = null;
        if (!string.IsNullOrWhiteSpace(quickLabel))
        {
            customerChoice = new ChatMessage
            {
                ConversationId = conversation.Id,
                SenderType = ChatSenderType.Customer,
                SenderUserId = userId,
                SenderName = conversation.CustomerName ?? conversation.GuestName ?? "Khách hàng",
                Message = quickLabel
            };
            conversation.StaffUnreadCount++;
            _db.ChatMessages.Add(customerChoice);
        }

        var result = await _automation.ExecuteAsync(conversation, userId, request.ActionType, request.Payload);
        if (customerChoice != null)
        {
            var customerPayload = MessagePayload(customerChoice);
            await NotifyConversation(conversation.Id, customerPayload);
            await NotifyStaff(conversation.Id, customerPayload);
        }
        foreach (var message in result.Messages)
        {
            var messagePayload = MessagePayload(message);
            await NotifyConversation(conversation.Id, messagePayload);
            await NotifyStaff(conversation.Id, messagePayload);
        }
        return Ok(Api(true, data: AutomationPayload(result)));
    }

    [HttpPost("conversations/{conversationId:int}/system-message")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSystemMessage(int conversationId, [FromBody] SystemChatMessageRequest? request)
    {
        if (request == null || !string.Equals(request.MessageType, "close", StringComparison.OrdinalIgnoreCase)) return BadRequest(Api(false, "Loại tin nhắn hệ thống không hợp lệ."));
        var conversation = await FindOwnedConversation(conversationId, request.AccessToken);
        if (conversation == null) return NotFound(Api(false, "Không tìm thấy cuộc trò chuyện."));
        if (conversation.Status != ChatConversationStatus.Closed) return BadRequest(Api(false, "Chỉ gửi lời cảm ơn sau khi hội thoại đã đóng."));
        var existing = await _db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(x => x.ConversationId == conversationId && x.IsSystem && x.Message == SupportChatDefaults.CloseMessage);
        if (existing != null) return Ok(Api(true, data: MessagePayload(existing)));
        var message = new ChatMessage { ConversationId = conversationId, SenderType = ChatSenderType.System, SenderName = "KKSHOP", Message = SupportChatDefaults.CloseMessage, IsSystem = true, IsRead = true, ReadAt = DateTime.UtcNow };
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();
        return Ok(Api(true, "Đã gửi lời cảm ơn.", MessagePayload(message)));
    }


    private static bool IsQuickActionLabel(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var quickActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Tư vấn cấu hình PC",
            "Kiểm tra bảo hành",
            "Hỏi tình trạng đơn hàng",
            "Hỗ trợ thanh toán",
            "Gặp nhân viên"
        };

        return quickActions.Contains(value.Trim());
    }

    private static string? QuickActionLabel(string actionType) => SupportChatDefaults.QuickQuestions.Select(x => new { ActionType = (string?)x.GetType().GetProperty("actionType")?.GetValue(x), Label = (string?)x.GetType().GetProperty("label")?.GetValue(x) }).FirstOrDefault(x => string.Equals(x.ActionType, actionType, StringComparison.OrdinalIgnoreCase))?.Label;
    private ChatMessage AddAiMessage(ChatConversation conversation, string reply, IReadOnlyList<AiProductContext> products, bool attachProductCards, string? requestId)
    {
        var cards = attachProductCards ? products.Take(8).Select(p => (object)new
        {
            type = "product",
            title = p.Name,
            subtitle = $"{p.Price:N0} đ",
            imageUrl = p.ImageUrl,
            badge = p.ProductTypeLabel,
            price = p.Price,
            stockStatus = p.StockStatus,
            warranty = p.Warranty,
            canAddToBuild = p.CanAddToBuild,
            actions = ProductCardActions(p).ToArray()
        }).ToList() : [];
        var buildIds = products.Where(p => p.CanAddToBuild).Select(p => p.Id).Distinct().ToList();
        var actions = buildIds.Count >= 7
            ? new object[] { new { label = "Thêm toàn bộ vào Build PC", url = $"/buildpc?aiBuild={string.Join(',', buildIds)}", style = "primary" } }
            : Array.Empty<object>();
        var message = new ChatMessage
        {
            Conversation = conversation,
            SenderType = ChatSenderType.System,
            SenderName = "KKSHOP AI",
            Message = reply,
            IsSystem = true,
            IsRead = true,
            ReadAt = DateTime.UtcNow,
            MetadataJson = JsonSerializer.Serialize(new { type = "ai", requestId, cards, messageActions = actions, quickReplies = Array.Empty<object>() })
        };
        _db.ChatMessages.Add(message);
        conversation.LastMessageAt = DateTime.UtcNow;
        return message;
    }


    private static IEnumerable<object> ProductCardActions(AiProductContext p)
    {
        yield return new { label = "Xem chi tiết", url = p.Link };
        yield return new { label = "Mua ngay", url = "/Cart/BuyNow", method = "post", payload = new { productId = p.Id, quantity = 1 } };
        if (p.CanAddToBuild) yield return new { label = "Thêm vào Build PC", url = $"/buildpc?aiBuild={p.Id}" };
    }

    private async Task<ChatConversation?> FindOpenConversation(int? userId, string? guestId, string? email, string? phone)
    {
        if (userId.HasValue) return await _db.ChatConversations.FirstOrDefaultAsync(x => x.Status == ChatConversationStatus.Open && (x.CustomerId == userId || x.UserId == userId));
        if (!string.IsNullOrWhiteSpace(guestId)) return await _db.ChatConversations.FirstOrDefaultAsync(x => x.Status == ChatConversationStatus.Open && x.GuestId == guestId);
        return await _db.ChatConversations.FirstOrDefaultAsync(x => x.Status == ChatConversationStatus.Open && ((!string.IsNullOrEmpty(email) && (x.CustomerEmail == email || x.GuestEmail == email)) || (!string.IsNullOrEmpty(phone) && (x.CustomerPhone == phone || x.GuestPhone == phone))));
    }

    private async Task<ChatConversation?> FindOwnedConversation(int id, string? token)
    {
        var userId = CurrentUserId();
        return await _db.ChatConversations.FirstOrDefaultAsync(x => x.Id == id && ((userId.HasValue && (x.CustomerId == userId || x.UserId == userId)) || (!string.IsNullOrWhiteSpace(token) && x.AccessToken == token)));
    }

    private async Task<List<object>> LoadMessages(int id)
    {
        var rows = await _db.ChatMessages.AsNoTracking().Where(x => x.ConversationId == id).OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync();
        return rows.Select(x => MessagePayload(x)).ToList();
    }
    private async Task NotifyStaff(int id, object payload) { try { await _hub.Clients.Group(ChatHub.StaffGroup).SendAsync("MessageReceived", id, payload); await _hub.Clients.Group(ChatHub.StaffGroup).SendAsync("ConversationUpdated", id); } catch (Exception e) { _logger.LogWarning(e, "Realtime notification failed for conversation {ConversationId}", id); } }
    private async Task NotifyConversation(int id, object payload) { try { await _hub.Clients.Group(ChatHub.ConversationGroup(id)).SendAsync("MessageReceived", id, payload); } catch (Exception e) { _logger.LogWarning(e, "Realtime customer notification failed for conversation {ConversationId}", id); } }
    private int? CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private string FirstModelError(string fallback) => ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).FirstOrDefault() ?? fallback;
    private static string NormalizeGuestId(string? value) => string.IsNullOrWhiteSpace(value) || value.Length > 64 ? Convert.ToHexString(RandomNumberGenerator.GetBytes(16)) : value.Trim();
    private static void Normalize(CreateChatConversationRequest r) { r.Name = r.Name?.Trim(); r.Email = r.Email?.Trim().ToLowerInvariant(); r.Phone = r.Phone?.Trim(); r.Message = r.Message?.Trim() ?? ""; }
    private static object Api(bool success, string? message = null, object? data = null) => new { success, message, data };
    private static object MessagePayload(ChatMessage x) => new
    {
        x.Id,
        senderType = x.SenderType == ChatSenderType.Staff ? "Staff" : x.SenderType.ToString(),
        senderName = x.IsSystem ? "KKSHOP" : x.SenderName,
        displaySenderName = x.IsSystem ? "KKSHOP" : x.SenderName,
        x.Message,
        x.IsSystem,
        x.IsRead,
        x.ReadAt,
        x.CreatedAt,
        metadata = ParseMetadata(x.MetadataJson)
    };
    private static JsonElement? ParseMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }
    private static object AutomationPayload(SupportAutomationResult result) => new
    {
        messages = result.Messages.Select(MessagePayload),
        quickReplies = result.QuickReplies,
        cards = result.Cards,
        messageActions = result.MessageActions
    };
}
