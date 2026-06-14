using System.ComponentModel.DataAnnotations;

namespace Datn.PcStore.Models;

public enum ChatConversationStatus
{
    Open = 1,
    Closed = 2
}

public enum ChatSenderType
{
    Customer = 1,
    Staff = 2,
    Admin = Staff,
    System = 3
}

public class ChatConversation : BaseEntity
{
    // UserId/GuestName are retained for compatibility with existing chat data.
    public int? UserId { get; set; }
    public User? User { get; set; }

    public int? CustomerId { get; set; }

    [MaxLength(64)]
    public string? GuestId { get; set; }

    [MaxLength(100)]
    public string? CustomerName { get; set; }

    [MaxLength(120)]
    public string? CustomerEmail { get; set; }

    [MaxLength(20)]
    public string? CustomerPhone { get; set; }

    [MaxLength(100)]
    public string? GuestName { get; set; }

    [MaxLength(120)]
    public string? GuestEmail { get; set; }

    [MaxLength(20)]
    public string? GuestPhone { get; set; }

    [MaxLength(64)]
    public string AccessToken { get; set; } = string.Empty;

    public ChatConversationStatus Status { get; set; } = ChatConversationStatus.Open;
    public int? AssignedStaffId { get; set; }

    [MaxLength(100)]
    public string? AssignedStaffName { get; set; }

    public DateTime? ClosedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public int StaffUnreadCount { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public class ChatMessage : BaseEntity
{
    public int ConversationId { get; set; }
    public ChatConversation? Conversation { get; set; }
    public ChatSenderType SenderType { get; set; }
    public int? SenderUserId { get; set; }

    [MaxLength(100)]
    public string? SenderName { get; set; }

    [Required, MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    public bool IsSystem { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
