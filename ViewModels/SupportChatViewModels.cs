using System.ComponentModel.DataAnnotations;

namespace Datn.PcStore.ViewModels;

public class CreateChatConversationRequest
{
    [MaxLength(64)] public string? GuestId { get; set; }
    [MaxLength(100)] public string? Name { get; set; }
    [EmailAddress, MaxLength(120)] public string? Email { get; set; }
    [MaxLength(20)] public string? Phone { get; set; }
    [Required, StringLength(1000, MinimumLength = 1)] public string Message { get; set; } = string.Empty;
    [StringLength(80)] public string? RequestId { get; set; }
}

public class SendChatMessageRequest
{
    [Required, StringLength(64)] public string AccessToken { get; set; } = string.Empty;
    [Required, StringLength(1000, MinimumLength = 1)] public string Message { get; set; } = string.Empty;
    [StringLength(80)] public string? RequestId { get; set; }
}

public class AdminSendChatMessageRequest
{
    [Required, StringLength(1000, MinimumLength = 1)] public string Message { get; set; } = string.Empty;
}

public class SystemChatMessageRequest
{
    [Required, StringLength(64)] public string AccessToken { get; set; } = string.Empty;
    [Required, RegularExpression("close")] public string MessageType { get; set; } = string.Empty;
}

public class AssignChatConversationRequest
{
    public int? StaffId { get; set; }
}

public class SupportChatQuickActionRequest
{
    [Range(1, int.MaxValue)] public int ConversationId { get; set; }
    [Required, StringLength(64)] public string AccessToken { get; set; } = string.Empty;
    [Required, StringLength(50)] public string ActionType { get; set; } = string.Empty;
    public SupportChatQuickActionPayload? Payload { get; set; }
}

public class SupportChatQuickActionPayload
{
    public int? OrderId { get; set; }
    public int? OrderDetailId { get; set; }
    public int? ProductId { get; set; }
    [StringLength(50)] public string? Budget { get; set; }
    [StringLength(50)] public string? NeedType { get; set; }
}
