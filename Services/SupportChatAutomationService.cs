using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Datn.PcStore.Data;
using Datn.PcStore.Helpers;
using Datn.PcStore.Models;
using Datn.PcStore.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Datn.PcStore.Services;

public record SupportQuickReply(string ActionType, string Label, object? Payload = null, string? Url = null);
public record SupportCard(string Type, string Title, string? Subtitle = null, string? ImageUrl = null, IReadOnlyList<SupportQuickReply>? Actions = null);
public record SupportAutomationResult(IReadOnlyList<ChatMessage> Messages, IReadOnlyList<SupportQuickReply> QuickReplies, IReadOnlyList<SupportCard> Cards);

public interface ISupportChatAutomationService
{
    Task<SupportAutomationResult> ExecuteAsync(ChatConversation conversation, int? userId, string actionType, SupportChatQuickActionPayload? payload, CancellationToken cancellationToken = default);
    Task<SupportAutomationResult?> TryHandleTextAsync(ChatConversation conversation, int? userId, string text, CancellationToken cancellationToken = default);
}

public partial class SupportChatAutomationService : ISupportChatAutomationService
{
    private readonly ApplicationDbContext _db;
    public SupportChatAutomationService(ApplicationDbContext db) => _db = db;

    public async Task<SupportAutomationResult> ExecuteAsync(ChatConversation conversation, int? userId, string actionType, SupportChatQuickActionPayload? payload, CancellationToken cancellationToken = default)
    {
        actionType = actionType.Trim().ToLowerInvariant();
        var result = actionType switch
        {
            "pc_consultation" => PcConsultation(conversation),
            "pc_need_gaming" => PcSelection(conversation, "Gaming"),
            "pc_need_render" => PcSelection(conversation, "Đồ họa / Render"),
            "pc_need_office" => PcSelection(conversation, "Văn phòng"),
            "pc_need_livestream" => PcSelection(conversation, "Livestream"),
            "pc_need_study" => PcSelection(conversation, "Học tập"),
            "pc_by_budget" => PcBudgets(conversation),
            "pc_budget_under_15" => PcSelection(conversation, "ngân sách dưới 15 triệu"),
            "pc_budget_15_25" => PcSelection(conversation, "ngân sách 15 - 25 triệu"),
            "pc_budget_25_40" => PcSelection(conversation, "ngân sách 25 - 40 triệu"),
            "pc_budget_over_40" => PcSelection(conversation, "ngân sách trên 40 triệu"),
            "staff_support" => StaffSupport(conversation),
            "order_status" => await OrderStatusAsync(conversation, userId, cancellationToken),
            "select_order" => await SelectOrderAsync(conversation, userId, payload?.OrderId, cancellationToken),
            "warranty_check" => await WarrantyAsync(conversation, userId, cancellationToken),
            "select_warranty_product" => await SelectWarrantyAsync(conversation, userId, payload?.OrderDetailId, cancellationToken),
            "payment_support" => await PaymentAsync(conversation, userId, cancellationToken),
            "select_payment_order" => await SelectPaymentAsync(conversation, userId, payload?.OrderId, cancellationToken),
            _ => Result(AddSystem(conversation, "KKSHOP chưa nhận diện được lựa chọn này. Bạn vui lòng thử lại hoặc gặp nhân viên hỗ trợ."), [StaffReply()])
        };
        conversation.LastMessageAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<SupportAutomationResult?> TryHandleTextAsync(ChatConversation conversation, int? userId, string text, CancellationToken cancellationToken = default)
    {
        var match = OrderCodeRegex().Match(text);
        if (!match.Success) return null;
        var id = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        return await SelectOrderAsync(conversation, userId, id, cancellationToken);
    }

    private SupportAutomationResult PcConsultation(ChatConversation c)
    {
        SetContext(c, "PCConsultation", false, 1);
        return Result(AddSystem(c, "Bạn cần tư vấn cấu hình PC theo nhu cầu nào ạ? Vui lòng cho KKSHOP biết ngân sách, mục đích sử dụng và màn hình đang dùng nếu có."),
        [
            Reply("pc_need_gaming", "Gaming"), Reply("pc_need_render", "Đồ họa / Render"),
            Reply("pc_need_office", "Văn phòng"), Reply("pc_need_livestream", "Livestream"),
            Reply("pc_need_study", "Học tập"), Reply("pc_by_budget", "Theo ngân sách"), StaffReply()
        ]);
    }

    private SupportAutomationResult PcBudgets(ChatConversation c) => Result(AddSystem(c, "Bạn dự kiến ngân sách cho bộ PC ở mức nào ạ?"),
    [
        Reply("pc_budget_under_15", "Dưới 15 triệu"), Reply("pc_budget_15_25", "15 - 25 triệu"),
        Reply("pc_budget_25_40", "25 - 40 triệu"), Reply("pc_budget_over_40", "Trên 40 triệu"), StaffReply()
    ]);

    private SupportAutomationResult PcSelection(ChatConversation c, string need)
    {
        SetContext(c, "PCConsultation", true, 2, new { pcNeed = need });
        return Result(AddSystem(c, $"KKSHOP đã ghi nhận nhu cầu {need}. Nhân viên tư vấn sẽ hỗ trợ cấu hình phù hợp sớm nhất. Bạn có thể nhập thêm game/phần mềm bạn dùng để tư vấn chính xác hơn."), [StaffReply()]);
    }

    private async Task<SupportAutomationResult> OrderStatusAsync(ChatConversation c, int? userId, CancellationToken ct)
    {
        SetContext(c, "Order", false, 1);
        if (!userId.HasValue)
            return Result(AddSystem(c, "Bạn vui lòng đăng nhập tài khoản đã đặt hàng để KKSHOP kiểm tra đơn nhanh hơn. Bạn cũng có thể nhập mã đơn hàng, ví dụ DH000016."),
            [new("login", "Đăng nhập", null, "/Account/Login"), Reply("enter_order_code", "Tôi có mã đơn hàng"), StaffReply("Gặp nhân viên")]);
        var orders = await OwnedOrders(userId.Value).OrderByDescending(x => x.CreatedAt).Take(5).ToListAsync(ct);
        if (orders.Count == 0) return Result(AddSystem(c, "Hiện tài khoản của bạn chưa có đơn hàng để kiểm tra."), [StaffReply()]);
        return Result(AddSystem(c, "Đây là các đơn hàng gần nhất của bạn. Hãy chọn một đơn để xem tình trạng:"), cards: orders.Select<Order, SupportCard>(order => OrderCard(order)).ToList());
    }

    private async Task<SupportAutomationResult> SelectOrderAsync(ChatConversation c, int? userId, int? orderId, CancellationToken ct)
    {
        if (!userId.HasValue || !orderId.HasValue)
            return Result(AddSystem(c, "KKSHOP chưa thể xác minh quyền xem đơn hàng này. Vui lòng đăng nhập bằng tài khoản đã đặt hàng."), [new("login", "Đăng nhập", null, "/Account/Login"), StaffReply()]);
        var order = await OwnedOrders(userId.Value).FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (order == null) return Result(AddSystem(c, "Không tìm thấy đơn hàng thuộc tài khoản của bạn. KKSHOP không thể hiển thị dữ liệu đơn của tài khoản khác."), [StaffReply()]);
        SetContext(c, "Order", false, 1, new { orderCode = Code(order.Id) });
        var message = $"Đơn {Code(order.Id)} hiện đang: {OrderStatusHelper.Label(order.Status)}. Thanh toán: {OrderStatusHelper.PaymentLabel(order.PaymentStatus, order.Status)}. Tổng tiền: {Money(order.TotalAmount)}. Ngày đặt: {order.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}.";
        return Result(AddSystem(c, message),
        [
            new("open_order", "Xem chi tiết đơn hàng", null, $"/Orders/Detail/{order.Id}"),
            new("track_order", "Theo dõi đơn hàng", null, $"/Orders/Detail/{order.Id}"),
            new("staff_support", "Cần nhân viên hỗ trợ đơn này", new { orderId = order.Id })
        ]);
    }

    private async Task<SupportAutomationResult> WarrantyAsync(ChatConversation c, int? userId, CancellationToken ct)
    {
        SetContext(c, "Warranty", false, 1);
        if (!userId.HasValue) return Result(AddSystem(c, "Bạn vui lòng đăng nhập hoặc nhập mã đơn hàng/mã sản phẩm đã mua để kiểm tra bảo hành."), [new("login", "Đăng nhập", null, "/Account/Login"), StaffReply()]);
        var details = await _db.OrderDetails.AsNoTracking().Include(x => x.Order).Include(x => x.Product)
            .Where(x => x.Order!.UserId == userId && x.Order.Status != OrderStatus.Cancelled && x.Order.Status != OrderStatus.Expired)
            .OrderByDescending(x => x.Order!.CreatedAt).Take(5).ToListAsync(ct);
        if (details.Count == 0) return Result(AddSystem(c, "Tài khoản của bạn chưa có sản phẩm đã mua phù hợp để kiểm tra bảo hành."), [StaffReply()]);
        var cards = details.Select(d => new SupportCard("product", d.ProductName, $"{Code(d.OrderId)} • {(d.WarrantyMonths > 0 ? $"{d.WarrantyMonths} tháng" : "Chưa rõ thời hạn")}", d.ProductImage,
            [new("select_warranty_product", "Kiểm tra", new { orderDetailId = d.Id })])).ToList();
        return Result(AddSystem(c, "Chọn sản phẩm đã mua bạn muốn kiểm tra bảo hành:"), cards: cards);
    }

    private async Task<SupportAutomationResult> SelectWarrantyAsync(ChatConversation c, int? userId, int? detailId, CancellationToken ct)
    {
        if (!userId.HasValue || !detailId.HasValue) return Result(AddSystem(c, "KKSHOP chưa thể xác minh sản phẩm này. Vui lòng đăng nhập tài khoản đã mua hàng."), [StaffReply()]);
        var detail = await _db.OrderDetails.AsNoTracking().Include(x => x.Order).Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == detailId && x.Order!.UserId == userId, ct);
        if (detail == null) return Result(AddSystem(c, "Không tìm thấy sản phẩm thuộc đơn hàng của bạn. KKSHOP không cung cấp dữ liệu mua hàng của tài khoản khác."), [StaffReply()]);
        SetContext(c, "Warranty", false, 1, new { warrantyProduct = detail.ProductName, orderCode = Code(detail.OrderId) });
        var months = detail.WarrantyMonths > 0 ? detail.WarrantyMonths : detail.Product?.WarrantyMonths ?? 0;
        if (months <= 0)
        {
            c.NeedsStaff = true; c.Priority = 2;
            return Result(AddSystem(c, "KKSHOP chưa xác định được thời hạn bảo hành cho sản phẩm này. Nhân viên sẽ kiểm tra thủ công giúp bạn."), [StaffReply()]);
        }
        var expires = WarrantyPolicy.ExpiresAt(detail.Order!.CreatedAt, months);
        var status = DateTime.UtcNow <= expires ? "Còn bảo hành" : "Hết bảo hành";
        return Result(AddSystem(c, $"Sản phẩm {detail.ProductName} trong đơn {Code(detail.OrderId)}: ngày mua {detail.Order.CreatedAt.ToLocalTime():dd/MM/yyyy}, bảo hành {months} tháng, {status.ToLowerInvariant()} đến {expires.ToLocalTime():dd/MM/yyyy}."),
        [new("open_warranty", "Tạo yêu cầu bảo hành", null, $"/Warranty/Create?orderDetailId={detail.Id}"), StaffReply()]);
    }

    private async Task<SupportAutomationResult> PaymentAsync(ChatConversation c, int? userId, CancellationToken ct)
    {
        SetContext(c, "Payment", false, 1);
        if (!userId.HasValue) return Result(AddSystem(c, "Bạn vui lòng đăng nhập để KKSHOP kiểm tra các đơn hàng cần thanh toán."), [new("login", "Đăng nhập", null, "/Account/Login"), StaffReply("Gặp nhân viên thanh toán")]);
        var orders = await OwnedOrders(userId.Value).Where(x => x.Status == OrderStatus.PendingPayment || x.Status == OrderStatus.PendingConfirmation || x.Status == OrderStatus.Pending)
            .OrderByDescending(x => x.CreatedAt).Take(5).ToListAsync(ct);
        if (orders.Count == 0) return Result(AddSystem(c, "Hiện tài khoản của bạn chưa có đơn hàng cần thanh toán."), [StaffReply("Gặp nhân viên thanh toán")]);
        return Result(AddSystem(c, "Chọn đơn hàng bạn cần hỗ trợ thanh toán:"), cards: orders.Select(x => OrderCard(x, "select_payment_order", "Kiểm tra thanh toán")).ToList());
    }

    private async Task<SupportAutomationResult> SelectPaymentAsync(ChatConversation c, int? userId, int? orderId, CancellationToken ct)
    {
        if (!userId.HasValue || !orderId.HasValue) return Result(AddSystem(c, "KKSHOP chưa thể xác minh đơn thanh toán này."), [StaffReply("Gặp nhân viên thanh toán")]);
        var order = await OwnedOrders(userId.Value).FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (order == null) return Result(AddSystem(c, "Không tìm thấy đơn hàng thanh toán thuộc tài khoản của bạn."), [StaffReply()]);
        SetContext(c, "Payment", false, 1, new { orderCode = Code(order.Id) });
        var expired = OrderStatusHelper.IsExpiredPayment(order, DateTime.UtcNow);
        var text = $"{Code(order.Id)} • Tổng tiền: {Money(order.TotalAmount)} • Phương thức: {PaymentMethods.Label(order.PaymentMethod)} • Trạng thái: {OrderStatusHelper.PaymentLabel(order.PaymentStatus, order.Status)}.";
        if (expired) text += " Đơn đã hết hạn thanh toán.";
        var replies = new List<SupportQuickReply>();
        if (!expired && OrderStatusHelper.CanPayNow(order, DateTime.UtcNow))
            replies.Add(new("open_payment", "Mở trang thanh toán", null, string.IsNullOrWhiteSpace(order.PaymentUrl) ? $"/Order/Pay/{order.Id}" : order.PaymentUrl));
        replies.Add(StaffReply("Gặp nhân viên thanh toán"));
        return Result(AddSystem(c, text), replies);
    }

    private SupportAutomationResult StaffSupport(ChatConversation c)
    {
        SetContext(c, "StaffSupport", true, 3);
        return Result(AddSystem(c, "KKSHOP đã chuyển yêu cầu của bạn đến nhân viên hỗ trợ. Bạn vui lòng để lại nội dung cần tư vấn, nhân viên sẽ phản hồi sớm nhất."));
    }

    private IQueryable<Order> OwnedOrders(int userId) => _db.Orders.AsNoTracking().Where(x => x.UserId == userId);
    private ChatMessage AddSystem(ChatConversation c, string text)
    {
        var message = new ChatMessage { Conversation = c, SenderType = ChatSenderType.System, SenderName = "KKSHOP Bot", Message = text, IsSystem = true, IsRead = true, ReadAt = DateTime.UtcNow };
        _db.ChatMessages.Add(message);
        return message;
    }
    private static SupportAutomationResult Result(ChatMessage message, IReadOnlyList<SupportQuickReply>? replies = null, IReadOnlyList<SupportCard>? cards = null) => new([message], replies ?? [], cards ?? []);
    private static SupportQuickReply Reply(string action, string label) => new(action, label);
    private static SupportQuickReply StaffReply(string label = "Gặp nhân viên tư vấn") => new("staff_support", label);
    private static string Code(int id) => $"DH{id:000000}";
    private static string Money(decimal value) => $"{value:N0} đ";
    private static SupportCard OrderCard(Order x, string action = "select_order", string label = "") => new("order", $"{Code(x.Id)} - {OrderStatusHelper.Label(x.Status)}", $"{Money(x.TotalAmount)} • {x.CreatedAt.ToLocalTime():dd/MM/yyyy}", null, [new(action, string.IsNullOrEmpty(label) ? $"Xem {Code(x.Id)}" : label, new { orderId = x.Id })]);
    private static void SetContext(ChatConversation c, string topic, bool needsStaff, int priority, object? context = null)
    {
        c.Topic = topic; c.NeedsStaff = needsStaff; c.Priority = priority;
        if (context != null) c.AutomationContext = JsonSerializer.Serialize(context);
    }
    [GeneratedRegex(@"\bDH0*(\d{1,9})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OrderCodeRegex();
}
