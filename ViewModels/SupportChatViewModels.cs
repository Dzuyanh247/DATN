using System.ComponentModel.DataAnnotations;

namespace Datn.PcStore.ViewModels;

public class CreateChatConversationRequest
{
    [MaxLength(100)] public string? Name { get; set; }
    [EmailAddress, MaxLength(120)] public string? Email { get; set; }
    [MaxLength(20)] public string? Phone { get; set; }
    [Required, StringLength(1000, MinimumLength = 1)] public string Message { get; set; } = string.Empty;
}

public class SendChatMessageRequest
{
    [Required, StringLength(64)] public string AccessToken { get; set; } = string.Empty;
    [Required, StringLength(1000, MinimumLength = 1)] public string Message { get; set; } = string.Empty;
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
