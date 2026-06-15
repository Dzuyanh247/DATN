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
public record SupportMessageAction(string Label, string? Url = null, string? ActionType = null, string Style = "primary", string Target = "sameTab", object? Payload = null);
public record SupportCard(
    string Type,
    string Title,
    string? Subtitle = null,
    string? ImageUrl = null,
    IReadOnlyList<SupportQuickReply>? Actions = null,
    int? OrderId = null,
    string? OrderCode = null,
    int? ProductId = null,
    int? OrderDetailId = null,
    decimal? TotalAmount = null,
    string? PaymentStatus = null,
    string? OrderStatus = null,
    DateTime? OrderedAt = null,
    DateTime? WarrantyUntil = null,
    string? WarrantyStatus = null,
    int AdditionalProductCount = 0);
public record SupportMessageMetadata(
    string MessageType,
    IReadOnlyList<SupportCard> Cards,
    IReadOnlyList<SupportMessageAction> MessageActions,
    IReadOnlyList<SupportQuickReply> QuickReplies);
public record SupportAutomationResult(
    IReadOnlyList<ChatMessage> Messages,
    IReadOnlyList<SupportQuickReply> QuickReplies,
    IReadOnlyList<SupportCard> Cards,
    IReadOnlyList<SupportMessageAction> MessageActions);

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
        var orders = await OwnedOrders(userId.Value)
            .Include(x => x.Details)
            .OrderByDescending(x => x.CreatedAt).Take(5).ToListAsync(ct);
        if (orders.Count == 0) return Result(AddSystem(c, "Hiện tài khoản của bạn chưa có đơn hàng để kiểm tra."), [StaffReply()]);
        var reviewed = await ReviewedProductsAsync(userId.Value, orders.Select(x => x.Id), ct);
        return Result(
            AddSystem(c, "Đây là các đơn hàng gần nhất của bạn. Hãy chọn một đơn để xem tình trạng:"),
            cards: orders.Select(order => OrderCard(order, reviewed)).ToList());
    }

    private async Task<SupportAutomationResult> SelectOrderAsync(ChatConversation c, int? userId, int? orderId, CancellationToken ct)
    {
        if (!userId.HasValue || !orderId.HasValue)
            return Result(AddSystem(c, "KKSHOP chưa thể xác minh quyền xem đơn hàng này. Vui lòng đăng nhập bằng tài khoản đã đặt hàng."), [new("login", "Đăng nhập", null, "/Account/Login"), StaffReply()]);
        var order = await OwnedOrders(userId.Value).Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (order == null) return Result(AddSystem(c, "Không tìm thấy đơn hàng thuộc tài khoản của bạn. KKSHOP không thể hiển thị dữ liệu đơn của tài khoản khác."), [StaffReply()]);
        SetContext(c, "Order", false, 1, new { orderCode = Code(order.Id) });
        var message =
            "KKSHOP đã kiểm tra đơn hàng của bạn:\n\n" +
            $"Đơn hàng: {Code(order.Id)}\n" +
            $"Trạng thái: {OrderStatusHelper.Label(order.Status)}\n" +
            $"Thanh toán: {OrderStatusHelper.PaymentLabel(order.PaymentStatus, order.Status)}\n" +
            $"Tổng tiền: {Money(order.TotalAmount)}\n" +
            $"Ngày đặt: {order.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}\n\n" +
            "Bạn có thể xem chi tiết đơn hàng hoặc liên hệ nhân viên nếu cần hỗ trợ thêm.";
        var reviewed = await ReviewedProductsAsync(userId.Value, [order.Id], ct);
        var actions = OrderActions(order, reviewed);
        return Result(AddSystem(c, message), cards: [OrderCard(order, reviewed, includeSelectionAction: false)], messageActions: actions);
    }

    private async Task<SupportAutomationResult> WarrantyAsync(ChatConversation c, int? userId, CancellationToken ct)
    {
        SetContext(c, "Warranty", false, 1);
        if (!userId.HasValue) return Result(AddSystem(c, "Bạn vui lòng đăng nhập hoặc nhập mã đơn hàng/mã sản phẩm đã mua để kiểm tra bảo hành."), [new("login", "Đăng nhập", null, "/Account/Login"), StaffReply()]);
        var details = await _db.OrderDetails.AsNoTracking().Include(x => x.Order).Include(x => x.Product)
            .Where(x => x.Order!.UserId == userId && x.Order.Status != OrderStatus.Cancelled && x.Order.Status != OrderStatus.Expired)
            .OrderByDescending(x => x.Order!.CreatedAt).Take(5).ToListAsync(ct);
        if (details.Count == 0) return Result(AddSystem(c, "Tài khoản của bạn chưa có sản phẩm đã mua phù hợp để kiểm tra bảo hành."), [StaffReply()]);
        var cards = details.Select(WarrantySelectionCard).ToList();
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
            return Result(
                AddSystem(c, "KKSHOP chưa xác định được thời hạn bảo hành của sản phẩm này. Bạn vui lòng gặp nhân viên tư vấn để shop kiểm tra thủ công giúp bạn."),
                cards: [WarrantyCard(detail, "Chưa xác định thời hạn bảo hành")],
                messageActions: [StaffAction()]);
        }
        var expires = WarrantyPolicy.ExpiresAt(detail.Order!.CreatedAt, months);
        var inWarranty = DateTime.UtcNow <= expires;
        var purchaseDate = detail.Order.CreatedAt.ToLocalTime();
        var expiryDate = expires.ToLocalTime();
        var warrantyStatus = inWarranty
            ? $"Còn bảo hành đến {expiryDate:dd/MM/yyyy}"
            : $"Đã hết bảo hành từ {expiryDate:dd/MM/yyyy}";
        var closingMessage = inWarranty
            ? "Bạn có thể tạo yêu cầu bảo hành nếu sản phẩm đang gặp lỗi."
            : "Bạn vẫn có thể gặp nhân viên tư vấn để được hỗ trợ phương án xử lý phù hợp.";
        var text =
            "KKSHOP đã kiểm tra bảo hành sản phẩm của bạn:\n\n" +
            $"Sản phẩm: {detail.ProductName}\n" +
            $"Đơn hàng: {Code(detail.OrderId)}\n" +
            $"Ngày mua: {purchaseDate:dd/MM/yyyy}\n" +
            $"Thời hạn bảo hành: {months} tháng\n" +
            $"Tình trạng: {warrantyStatus}\n\n" +
            closingMessage;
        var actions = new List<SupportMessageAction>();
        if (inWarranty)
        {
            var phone = detail.Order.ReceiverPhone;
            if (string.IsNullOrWhiteSpace(phone))
                phone = await _db.Users.AsNoTracking().Where(x => x.Id == userId.Value).Select(x => x.Phone).FirstOrDefaultAsync(ct);
            var warrantyUrl = $"/Warranty/Create?orderDetailId={detail.Id}";
            if (!string.IsNullOrWhiteSpace(phone))
                warrantyUrl += $"&phone={Uri.EscapeDataString(phone.Trim())}";
            actions.Add(new("Tạo yêu cầu bảo hành", warrantyUrl));
            actions.Add(StaffAction());
        }
        else
        {
            actions.Add(StaffAction());
        }
        return Result(AddSystem(c, text), cards: [WarrantyCard(detail, warrantyStatus, expires)], messageActions: actions);
    }

    private async Task<SupportAutomationResult> PaymentAsync(ChatConversation c, int? userId, CancellationToken ct)
    {
        SetContext(c, "Payment", false, 1);
        if (!userId.HasValue) return Result(AddSystem(c, "Bạn vui lòng đăng nhập để KKSHOP kiểm tra các đơn hàng cần thanh toán."), [new("login", "Đăng nhập", null, "/Account/Login"), StaffReply("Gặp nhân viên thanh toán")]);
        var orders = await OwnedOrders(userId.Value).Include(x => x.Details).Where(x => x.Status == OrderStatus.PendingPayment || x.Status == OrderStatus.PendingConfirmation || x.Status == OrderStatus.Pending)
            .OrderByDescending(x => x.CreatedAt).Take(5).ToListAsync(ct);
        if (orders.Count == 0) return Result(AddSystem(c, "Hiện tài khoản của bạn chưa có đơn hàng cần thanh toán."), [StaffReply("Gặp nhân viên thanh toán")]);
        return Result(AddSystem(c, "Chọn đơn hàng bạn cần hỗ trợ thanh toán:"), cards: orders.Select(x => OrderCard(x, action: "select_payment_order", label: "Kiểm tra thanh toán")).ToList());
    }

    private async Task<SupportAutomationResult> SelectPaymentAsync(ChatConversation c, int? userId, int? orderId, CancellationToken ct)
    {
        if (!userId.HasValue || !orderId.HasValue) return Result(AddSystem(c, "KKSHOP chưa thể xác minh đơn thanh toán này."), [StaffReply("Gặp nhân viên thanh toán")]);
        var order = await OwnedOrders(userId.Value).Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == orderId, ct);
        if (order == null) return Result(AddSystem(c, "Không tìm thấy đơn hàng thanh toán thuộc tài khoản của bạn."), [StaffReply()]);
        SetContext(c, "Payment", false, 1, new { orderCode = Code(order.Id) });
        var now = DateTime.UtcNow;
        var expired = OrderStatusHelper.IsExpiredPayment(order, now);
        var paid = OrderStatusHelper.IsPaid(order);
        var isCod = PaymentMethods.IsCod(order.PaymentMethod);
        var text = $"KKSHOP đã kiểm tra thanh toán cho đơn {Code(order.Id)}. Trạng thái hiện tại là {OrderStatusHelper.PaymentLabel(order.PaymentStatus, order.Status).ToLowerInvariant()}.";
        var actions = new List<SupportMessageAction>();
        if (isCod)
        {
            text += " Đơn hàng sẽ thanh toán khi nhận hàng.";
            actions.Add(new("Xem chi tiết đơn hàng", $"/Orders/Detail/{order.Id}"));
        }
        else if (paid)
        {
            text += " Đơn hàng đã được thanh toán. Bạn có thể xem chi tiết đơn hàng.";
            actions.Add(new("Xem chi tiết đơn hàng", $"/Orders/Detail/{order.Id}"));
        }
        else if (expired)
        {
            text += " Đơn hàng này đã hết hạn thanh toán, bạn có thể xem chi tiết hoặc gặp nhân viên để được hỗ trợ.";
            actions.Add(new("Xem chi tiết đơn hàng", $"/Orders/Detail/{order.Id}"));
            actions.Add(StaffAction());
        }
        else if (OrderStatusHelper.CanPayNow(order, now))
        {
            text += " Bạn có thể tiếp tục thanh toán đơn hàng ngay bên dưới.";
            var paymentUrl = string.IsNullOrWhiteSpace(order.PaymentUrl)
                ? $"/Orders/BankTransfer/{order.Id}"
                : order.PaymentUrl;
            actions.Add(new("Thanh toán đơn hàng", paymentUrl));
            actions.Add(StaffAction());
        }
        else
        {
            text += " Đơn hàng hiện không thể thanh toán online. Bạn có thể xem chi tiết hoặc gặp nhân viên để được hỗ trợ.";
            actions.Add(new("Xem chi tiết đơn hàng", $"/Orders/Detail/{order.Id}"));
            actions.Add(StaffAction());
        }
        return Result(
            AddSystem(c, text),
            cards: [OrderSummaryCard(order, $"Thanh toán: {OrderStatusHelper.PaymentLabel(order.PaymentStatus, order.Status)}")],
            messageActions: actions);
    }

    private SupportAutomationResult StaffSupport(ChatConversation c)
    {
        SetContext(c, "StaffSupport", true, 3);
        return Result(AddSystem(c, "KKSHOP đã chuyển yêu cầu của bạn đến nhân viên hỗ trợ. Bạn vui lòng để lại nội dung cần tư vấn, nhân viên sẽ phản hồi sớm nhất."));
    }

    private IQueryable<Order> OwnedOrders(int userId) => _db.Orders.AsNoTracking().Where(x => x.UserId == userId);
    private ChatMessage AddSystem(ChatConversation c, string text)
    {
        var message = new ChatMessage { Conversation = c, SenderType = ChatSenderType.System, SenderName = "KKSHOP", Message = text, IsSystem = true, IsRead = true, ReadAt = DateTime.UtcNow };
        _db.ChatMessages.Add(message);
        return message;
    }
    private static SupportAutomationResult Result(
        ChatMessage message,
        IReadOnlyList<SupportQuickReply>? replies = null,
        IReadOnlyList<SupportCard>? cards = null,
        IReadOnlyList<SupportMessageAction>? messageActions = null)
    {
        var actualReplies = replies ?? [];
        var actualCards = cards ?? [];
        var actualActions = messageActions ?? [];
        var messageType = actualCards.FirstOrDefault()?.Type switch
        {
            "order" => "orderCard",
            "product" => message.Message.Contains("bảo hành", StringComparison.OrdinalIgnoreCase) ? "warrantyResult" : "productCard",
            _ when message.Message.Contains("thanh toán", StringComparison.OrdinalIgnoreCase) => "paymentResult",
            _ => "text"
        };
        message.MetadataJson = JsonSerializer.Serialize(new SupportMessageMetadata(messageType, actualCards, actualActions, actualReplies));
        return new([message], actualReplies, actualCards, actualActions);
    }
    private static SupportQuickReply Reply(string action, string label) => new(action, label);
    private static SupportQuickReply StaffReply(string label = "Gặp nhân viên tư vấn") => new("staff_support", label);
    private static SupportMessageAction StaffAction(string label = "Gặp nhân viên tư vấn") => new(label, ActionType: "staff_support", Style: "secondary");
    private static string Code(int id) => $"DH{id:000000}";
    private static string Money(decimal value) => $"{value:N0} đ";
    private static SupportCard OrderCard(
        Order x,
        ISet<(int OrderId, int ProductId)>? reviewed = null,
        string action = "select_order",
        string label = "",
        bool includeSelectionAction = true)
    {
        var first = x.Details.OrderBy(d => d.Id).FirstOrDefault();
        var actions = new List<SupportQuickReply>();
        if (includeSelectionAction)
            actions.Add(new(action, string.IsNullOrEmpty(label) ? "Kiểm tra đơn hàng" : label, new { orderId = x.Id }));
        actions.Add(new("open_order", "Xem chi tiết", Url: $"/Orders/Detail/{x.Id}"));
        if (OrderStatusHelper.CanPayNow(x, DateTime.UtcNow))
            actions.Add(new("pay_order", "Thanh toán", Url: string.IsNullOrWhiteSpace(x.PaymentUrl) ? $"/Orders/BankTransfer/{x.Id}" : x.PaymentUrl));
        if (x.Status == OrderStatus.Completed && reviewed != null)
        {
            var reviewable = x.Details.FirstOrDefault(d => !reviewed.Contains((x.Id, d.ProductId)));
            if (reviewable != null)
                actions.Add(new("review_product", "Đánh giá", Url: $"/ProductReviews/Create?orderId={x.Id}&productId={reviewable.ProductId}"));
        }
        return new(
            "order", first?.ProductName ?? $"Đơn hàng {Code(x.Id)}",
            x.Details.Count > 1 ? $"+{x.Details.Count - 1} sản phẩm khác" : null,
            first?.ProductImage, actions, x.Id, Code(x.Id), first?.ProductId, first?.Id,
            x.TotalAmount, OrderStatusHelper.PaymentLabel(x.PaymentStatus, x.Status),
            OrderStatusHelper.Label(x.Status), x.CreatedAt, AdditionalProductCount: Math.Max(0, x.Details.Count - 1));
    }
    private static SupportCard OrderSummaryCard(Order x, string? status = null) => new(
        "order",
        $"Đơn hàng {Code(x.Id)}",
        $"{status ?? OrderStatusHelper.Label(x.Status)} • {Money(x.TotalAmount)} • {x.CreatedAt.ToLocalTime():dd/MM/yyyy}",
        x.Details.OrderBy(d => d.Id).Select(d => d.ProductImage).FirstOrDefault(),
        OrderId: x.Id, OrderCode: Code(x.Id), TotalAmount: x.TotalAmount,
        PaymentStatus: OrderStatusHelper.PaymentLabel(x.PaymentStatus, x.Status),
        OrderStatus: OrderStatusHelper.Label(x.Status), OrderedAt: x.CreatedAt);
    private static SupportCard WarrantyCard(OrderDetail detail, string status, DateTime? expires = null) => new(
        "product",
        detail.ProductName,
        $"{Code(detail.OrderId)} • {status}",
        detail.ProductImage,
        ProductId: detail.ProductId, OrderDetailId: detail.Id, OrderCode: Code(detail.OrderId),
        OrderedAt: detail.Order?.CreatedAt, WarrantyUntil: expires, WarrantyStatus: status);
    private static SupportCard WarrantySelectionCard(OrderDetail detail)
    {
        var months = detail.WarrantyMonths > 0 ? detail.WarrantyMonths : detail.Product?.WarrantyMonths ?? 0;
        var expires = months > 0 ? WarrantyPolicy.ExpiresAt(detail.Order!.CreatedAt, months) : (DateTime?)null;
        var status = !expires.HasValue ? "Chưa rõ thời hạn"
            : DateTime.UtcNow <= expires ? $"Còn hạn đến {expires.Value.ToLocalTime():dd/MM/yyyy}"
            : $"Hết hạn từ {expires.Value.ToLocalTime():dd/MM/yyyy}";
        return new(
            "product", detail.ProductName,
            $"Mua ngày {detail.Order!.CreatedAt.ToLocalTime():dd/MM/yyyy} • {months switch { > 0 => $"{months} tháng", _ => "Chưa rõ thời hạn" }}",
            detail.ProductImage, [new("select_warranty_product", "Kiểm tra", new { orderDetailId = detail.Id })],
            ProductId: detail.ProductId, OrderDetailId: detail.Id, OrderCode: Code(detail.OrderId),
            OrderedAt: detail.Order.CreatedAt, WarrantyUntil: expires, WarrantyStatus: status);
    }
    private static List<SupportMessageAction> OrderActions(Order order, ISet<(int OrderId, int ProductId)> reviewed)
    {
        var actions = new List<SupportMessageAction>
        {
            new("Xem chi tiết đơn hàng", $"/Orders/Detail/{order.Id}")
        };
        if (OrderStatusHelper.CanPayNow(order, DateTime.UtcNow))
            actions.Add(new("Thanh toán đơn hàng", string.IsNullOrWhiteSpace(order.PaymentUrl) ? $"/Orders/BankTransfer/{order.Id}" : order.PaymentUrl));
        if (order.Status == OrderStatus.Completed)
        {
            var reviewable = order.Details.FirstOrDefault(d => !reviewed.Contains((order.Id, d.ProductId)));
            if (reviewable != null)
                actions.Add(new("Đánh giá sản phẩm", $"/ProductReviews/Create?orderId={order.Id}&productId={reviewable.ProductId}", Style: "secondary"));
        }
        return actions;
    }
    private async Task<HashSet<(int OrderId, int ProductId)>> ReviewedProductsAsync(int userId, IEnumerable<int> orderIds, CancellationToken ct)
    {
        var ids = orderIds.Distinct().ToList();
        var rows = await _db.ProductReviews.AsNoTracking()
            .Where(x => x.UserId == userId && ids.Contains(x.OrderId))
            .Select(x => new { x.OrderId, x.ProductId })
            .ToListAsync(ct);
        return rows.Select(x => (x.OrderId, x.ProductId)).ToHashSet();
    }
    private static void SetContext(ChatConversation c, string topic, bool needsStaff, int priority, object? context = null)
    {
        c.Topic = topic; c.NeedsStaff = needsStaff; c.Priority = priority;
        if (context != null) c.AutomationContext = JsonSerializer.Serialize(context);
    }
    [GeneratedRegex(@"\bDH0*(\d{1,9})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OrderCodeRegex();
}
