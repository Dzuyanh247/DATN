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
    Admin = 2
}

public class ChatConversation : BaseEntity
{
    public int? UserId { get; set; }
    public User? User { get; set; }

    [MaxLength(100)]
    public string? GuestName { get; set; }

    [MaxLength(120)]
    public string? GuestEmail { get; set; }

    [MaxLength(20)]
    public string? GuestPhone { get; set; }

    [MaxLength(64)]
    public string AccessToken { get; set; } = string.Empty;

    public ChatConversationStatus Status { get; set; } = ChatConversationStatus.Open;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public class ChatMessage : BaseEntity
{
    public int ConversationId { get; set; }
    public ChatConversation? Conversation { get; set; }
    public ChatSenderType SenderType { get; set; }

    [Required, MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }
}
