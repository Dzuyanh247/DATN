namespace Datn.PcStore.Constants;

public static class SupportChatDefaults
{
    public const int MaximumMessageLength = 1000;
    public const string GreetingMessage = "KKSHOP xin chào! Bạn cần hỗ trợ gì ạ?";
    public const string CloseMessage = "Cảm ơn bạn đã liên hệ KKSHOP. Nếu cần hỗ trợ thêm, bạn có thể bắt đầu một hội thoại mới.";

    public static readonly IReadOnlyList<object> QuickQuestions =
    [
        new { actionType = "pc_consultation", label = "Tư vấn cấu hình PC" },
        new { actionType = "order_status", label = "Hỏi tình trạng đơn hàng" },
        new { actionType = "warranty_check", label = "Kiểm tra bảo hành" },
        new { actionType = "payment_support", label = "Hỗ trợ thanh toán" },
        new { actionType = "staff_support", label = "Gặp nhân viên" }
    ];
}
