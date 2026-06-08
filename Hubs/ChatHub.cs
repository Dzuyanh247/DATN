using System.Security.Claims;
using Datn.PcStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Hubs;

public class ChatHub : Hub
{
    private readonly ApplicationDbContext _db;

    public ChatHub(ApplicationDbContext db) => _db = db;

    public async Task<bool> JoinConversation(int conversationId, string accessToken)
    {
        var userId = int.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;
        var canJoin = await _db.ChatConversations.AnyAsync(x =>
            x.Id == conversationId &&
            ((userId.HasValue && x.UserId == userId) || x.AccessToken == accessToken));

        if (!canJoin) return false;
        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
        return true;
    }

    [Authorize(Roles = "Admin")]
    public Task JoinAdmin() => Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);

    public const string AdminGroup = "support-chat-admin";
    public static string ConversationGroup(int conversationId) => $"support-chat-{conversationId}";
}
