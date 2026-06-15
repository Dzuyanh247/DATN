namespace Datn.PcStore.Constants;

public static class SupportChatDefaults
{
    public const int MaximumMessageLength = 1000;
    public const string GreetingMessage = "KKSHOP xin chào! Bạn cần hỗ trợ gì ạ?";
    public const string CloseMessage = "Cảm ơn bạn đã liên hệ KKSHOP. Nếu cần hỗ trợ thêm, bạn có thể bắt đầu một hội thoại mới.";

    public static readonly IReadOnlyList<string> QuickQuestions =
    [
        "Tư vấn cấu hình PC",
        "Kiểm tra bảo hành",
        "Hỏi tình trạng đơn hàng",
        "Hỗ trợ thanh toán",
        "Chính sách giao hàng",
        "Cần nhân viên tư vấn"
    ];
}
