using System.Globalization;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Datn.PcStore.Services;

public record AiChatResponse(bool Success, string Reply, IReadOnlyList<AiProductContext> SuggestedProducts);

internal sealed class AiConversationContext
{
    public List<string> RecentMessages { get; } = [];
    public AiProductContext? CurrentProductContext { get; set; }
    public AiProductContext? LastReferencedProduct { get; set; }
}


internal enum AiChatIntent
{
    Greeting,
    OutOfScope,
    PolicyOrSupport,
    ProductSearch,
    ProductAdvice,
    ProductAnalysis,
    ProductBenefits,
    ProductProsCons,
    ProductRecommendation,
    ClarifyProductAdvice,
    GeneralShop
}

public interface IAiChatService
{
    Task<AiChatResponse> AskAsync(string message, string? sessionId, string? ipAddress, CancellationToken cancellationToken = default);
}

public partial class GeminiChatService : IAiChatService
{
    private const string BusyMessage = "AI đang bận, bạn vui lòng thử lại sau hoặc chọn Gặp nhân viên.";
    private readonly HttpClient _httpClient;
    private readonly IProductSearchForAiService _productSearch;
    private readonly IMemoryCache _cache;
    private readonly IShopPolicyService _shopPolicy;
    private readonly AiChatOptions _options;
    private readonly ILogger<GeminiChatService> _logger;

    public GeminiChatService(HttpClient httpClient, IProductSearchForAiService productSearch, IMemoryCache cache, IShopPolicyService shopPolicy, IOptions<AiChatOptions> options, ILogger<GeminiChatService> logger)
    {
        _httpClient = httpClient;
        _productSearch = productSearch;
        _cache = cache;
        _shopPolicy = shopPolicy;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiChatResponse> AskAsync(string message, string? sessionId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        message = (message ?? string.Empty).Trim();
        if (message.Length == 0) return new(false, "Bạn vui lòng nhập câu hỏi cần tư vấn.", []);
        if (message.Length > 500) return new(false, "Câu hỏi quá dài, bạn vui lòng rút gọn dưới 500 ký tự.", []);
        if (!IsAllowed(sessionId, ipAddress)) return new(false, "Bạn đang gửi quá nhanh, vui lòng thử lại sau vài giây.", []);
        var conversation = GetConversationContext(sessionId, ipAddress);
        conversation.RecentMessages.Add($"User: {message}");
        if (conversation.RecentMessages.Count > 10) conversation.RecentMessages.RemoveRange(0, conversation.RecentMessages.Count - 10);

        var intent = DetectIntent(message);
        var productFromUrl = await TryResolveProductUrlAsync(message, cancellationToken);
        if (productFromUrl != null)
        {
            conversation.CurrentProductContext = productFromUrl;
            conversation.LastReferencedProduct = productFromUrl;
        }
        if (intent == AiChatIntent.Greeting)
            return new(true, "Chào bạn! KKSHOP AI có thể hỗ trợ tư vấn PC, linh kiện, cấu hình, đơn hàng, bảo hành và thanh toán. Bạn cần mình hỗ trợ phần nào ạ?", []);
        if (intent == AiChatIntent.OutOfScope)
            return new(true, "Mình là KKSHOP AI nên lúc nào cũng sẵn sàng hỗ trợ bạn ạ 😄 Bạn muốn mình tư vấn PC, linh kiện, đơn hàng hay bảo hành không?", []);
        if (intent == AiChatIntent.ClarifyProductAdvice && !HasContextReference(message, conversation))
            return new(true, "Bạn muốn mình phân tích sản phẩm hoặc dòng sản phẩm nào ạ? Hãy gửi tên đầy đủ, nhu cầu sử dụng hoặc link sản phẩm để mình tư vấn ưu/nhược điểm kỹ hơn nhé.", []);

        var policyAnswer = _shopPolicy.Answer(message);
        if (policyAnswer.IsPolicyQuestion)
        {
            _logger.LogInformation("[AI] Message: {Message}; Intent: POLICY_QA; Scope: POLICY; Products: 0; TopProducts: none", message);
            return new(true, policyAnswer.Reply, []);
        }

        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey)) return new(false, BusyMessage, []);

        var contextProduct = ResolveContextProduct(message, conversation);
        var isAnalysisIntent = intent is AiChatIntent.ProductAnalysis or AiChatIntent.ProductBenefits or AiChatIntent.ProductProsCons or AiChatIntent.ProductRecommendation or AiChatIntent.ProductAdvice;
        var shouldSearchProducts = intent is AiChatIntent.ProductSearch or AiChatIntent.ProductAdvice or AiChatIntent.ProductAnalysis or AiChatIntent.ProductBenefits or AiChatIntent.ProductProsCons or AiChatIntent.ProductRecommendation || productFromUrl != null;
        IReadOnlyList<AiProductContext> products = productFromUrl != null ? [productFromUrl] : contextProduct != null && isAnalysisIntent ? [contextProduct] : shouldSearchProducts ? await _productSearch.SearchAsync(message, cancellationToken) : [];
        if (products.Count > 0)
        {
            conversation.CurrentProductContext = products[0];
            conversation.LastReferencedProduct = products[0];
        }
        LogDebugState(message, intent, conversation, products);
        if (shouldSearchProducts && products.Count == 0)
        {
            var normalizedMessage = RemoveDiacritics(message.ToLowerInvariant());
            if (ContainsAny(normalizedMessage, "pc", "cau hinh", "build", "choi game", "gaming", "valorant", "gta", "ngan sach"))
                return new(true, "Mình chưa tìm thấy PC bộ/cấu hình phù hợp trong dữ liệu hiện tại, nhưng có thể tư vấn build từ linh kiện nếu shop đã có đủ CPU, main, RAM, VGA, SSD, nguồn và case.", []);

            return new(true, "Mình chưa tìm thấy sản phẩm phù hợp với từ khóa này trong dữ liệu KKSHOP. Bạn thử gửi tên đầy đủ hơn, loại linh kiện hoặc ngân sách để mình kiểm tra chính xác hơn nhé.", []);
        }

        var cacheKey = $"ai-chat:{sessionId ?? ipAddress}:{intent}:{conversation.LastReferencedProduct?.Id}:{NormalizeCacheKey(message)}";
        if (_cache.TryGetValue<AiChatResponse>(cacheKey, out var cached) && cached != null) return cached;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 10, 15)));

        try
        {
            var prompt = BuildPrompt(message, products, _shopPolicy.BuildKnowledgePrompt(), intent, conversation);
            var request = new
            {
                systemInstruction = new { parts = new[] { new { text = SystemPrompt } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
                generationConfig = new { temperature = 0.2, maxOutputTokens = 700 }
            };
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(_options.Model)}:generateContent?key={Uri.EscapeDataString(_options.ApiKey)}";
            var response = await _httpClient.PostAsJsonAsync(url, request, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini returned status {StatusCode}", response.StatusCode);
                return new(false, BusyMessage, products);
            }
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(timeout.Token), cancellationToken: timeout.Token);
            var reply = ExtractReply(document.RootElement);
            if (string.IsNullOrWhiteSpace(reply)) reply = BusyMessage;
            var result = new AiChatResponse(true, reply.Trim(), products);
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(7));
            return result;
        }
        catch (OperationCanceledException)
        {
            return new(false, BusyMessage, products);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini chat failed");
            return new(false, BusyMessage, products);
        }
    }

    private AiConversationContext GetConversationContext(string? sessionId, string? ipAddress)
    {
        var key = $"ai-context:{sessionId ?? ipAddress ?? "unknown"}";
        return _cache.GetOrCreate(key, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(2);
            return new AiConversationContext();
        })!;
    }

    private async Task<AiProductContext?> TryResolveProductUrlAsync(string message, CancellationToken cancellationToken)
    {
        var match = ProductUrlRegex().Match(message);
        return match.Success && int.TryParse(match.Groups[1].Value, out var id)
            ? await _productSearch.GetByIdAsync(id, cancellationToken)
            : null;
    }

    private static AiProductContext? ResolveContextProduct(string message, AiConversationContext conversation)
        => HasContextReference(message, conversation) ? conversation.LastReferencedProduct ?? conversation.CurrentProductContext : conversation.CurrentProductContext;

    private static bool HasContextReference(string message, AiConversationContext conversation)
    {
        if (conversation.LastReferencedProduct == null && conversation.CurrentProductContext == null) return false;
        var normalized = RemoveDiacritics(message.ToLowerInvariant());
        return ContextReferenceWords.Any(normalized.Contains) || ProductUrlRegex().IsMatch(message);
    }

    private void LogDebugState(string message, AiChatIntent intent, AiConversationContext conversation, IReadOnlyList<AiProductContext> products)
    {
        var normalized = RemoveDiacritics(message.ToLowerInvariant());
        var budget = BudgetDebugRegex().Match(normalized).Value;
        var category = ProductTerms.FirstOrDefault(normalized.Contains) ?? "none";
        _logger.LogInformation("[KKSHOP_AI_DEBUG] Intent={Intent}; CurrentProductContext={CurrentProduct}; LastReferencedProduct={LastProduct}; Budget={Budget}; Category={Category}; SearchKeyword={SearchKeyword}; MatchedProducts={MatchedProducts}",
            intent,
            conversation.CurrentProductContext is null ? "none" : $"{conversation.CurrentProductContext.Id}:{conversation.CurrentProductContext.Name}",
            conversation.LastReferencedProduct is null ? "none" : $"{conversation.LastReferencedProduct.Id}:{conversation.LastReferencedProduct.Name}",
            string.IsNullOrWhiteSpace(budget) ? "none" : budget,
            category,
            TrimForLog(message),
            products.Count == 0 ? "none" : string.Join(" | ", products.Take(5).Select(p => $"{p.Id}:{p.Name}:{p.Price:N0}")));
    }

    private bool IsAllowed(string? sessionId, string? ipAddress)
    {
        var key = $"ai-rate:{sessionId ?? ipAddress ?? "unknown"}";
        var count = _cache.Get<int>(key);
        if (count >= 12) return false;
        _cache.Set(key, count + 1, TimeSpan.FromMinutes(1));
        return true;
    }

    private static AiChatIntent DetectIntent(string message)
    {
        var lower = message.ToLowerInvariant();
        var normalized = RemoveDiacritics(lower);
        var compact = normalized.Trim(' ', '.', '!', '?');
        if (GreetingWords.Contains(compact) || (GreetingWords.Any(g => compact.StartsWith(g + " ")) && compact.Length <= 28)) return AiChatIntent.Greeting;
        if (ContainsAny(normalized, "thoi tiet", "bong da", "chung khoan", "nau an", "an com", "xem phim", "du lich", "lich su")) return AiChatIntent.OutOfScope;
        if (ContainsAny(normalized, "bao hanh", "don hang", "thanh toan", "tra gop", "van chuyen", "ship", "nhan vien", "shop con hoat dong", "mo cua")) return AiChatIntent.PolicyOrSupport;

        var hasProductTerm = ProductTerms.Any(normalized.Contains) || Regex.IsMatch(normalized, @"\b(?:rtx|gtx|rx|i[3579]|ryzen|logitech|razer|akko|asus|msi|gigabyte|corsair|g304)\b", RegexOptions.IgnoreCase);
        var hasBudget = Regex.IsMatch(normalized, @"\b\d+(?:[\.,]\d+)?\s*(?:-|den|toi)?\s*\d*\s*(?:tr|trieu|m|million)\b", RegexOptions.IgnoreCase);
        var hasGame = ContainsAny(normalized, "gta", "gta5", "valorant", "black myth", "choi game", "gaming");
        var hasConfigIntent = ContainsAny(normalized, "cau hinh", "pc gaming", "may choi game", "may tinh", "build", "shop co san");
        var hasSearchIntent = SearchIntentWords.Any(normalized.Contains);
        var hasAdviceIntent = AdviceIntentWords.Any(normalized.Contains);
        if (ContainsAny(normalized, "loi ich")) return AiChatIntent.ProductBenefits;
        if (ContainsAny(normalized, "uu diem", "nhuoc diem")) return AiChatIntent.ProductProsCons;
        if (ContainsAny(normalized, "co nen mua", "phu hop voi ai", "choi game duoc khong", "choi duoc khong")) return AiChatIntent.ProductRecommendation;
        if (ContainsAny(normalized, "phan tich", "danh gia san pham", "danh gia", "phu hop", "choi game duoc")) return AiChatIntent.ProductAnalysis;
        if ((hasConfigIntent || hasGame || hasBudget) && !ContainsAny(normalized, "don hang", "bao hanh", "thanh toan", "tra gop")) return AiChatIntent.ProductAdvice;
        if (hasSearchIntent || (hasProductTerm && ContainsAny(normalized, "gia", "con hang", "mua", "ban", "tim", "goi y", "ngan sach", "duoi", "tam"))) return AiChatIntent.ProductSearch;
        if (hasAdviceIntent && hasProductTerm) return AiChatIntent.ProductAdvice;
        if (hasAdviceIntent) return AiChatIntent.ClarifyProductAdvice;
        if (hasProductTerm) return AiChatIntent.ProductAdvice;
        return AiChatIntent.GeneralShop;
    }

    private static bool ContainsAny(string source, params string[] values) => values.Any(source.Contains);

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Replace('đ', 'd').Replace('Đ', 'D').Normalize(System.Text.NormalizationForm.FormD);
        return new string(normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).Normalize(System.Text.NormalizationForm.FormC);
    }

    private static string BuildPrompt(string message, IReadOnlyList<AiProductContext> products, string policyKnowledge, AiChatIntent intent, AiConversationContext conversation)
    {
        var productLines = products.Count == 0
            ? "Hiện KKSHOP chưa tìm thấy cấu hình phù hợp trong dữ liệu hiện có."
            : string.Join("\n", products.Select((p, i) => $"{i + 1}. Tên: {p.Name}; Giá: {p.Price:N0} đ; Mô tả: {p.Description}; Cấu hình/thông số: {p.Specifications}; Bảo hành: {p.Warranty}; Tồn kho: {p.StockStatus}; Link: {p.Link}; Danh mục: {p.Category}; Loại card bắt buộc: {p.ProductTypeLabel}; Phạm vi: {p.CategoryScope}"));
        var productRule = intent == AiChatIntent.ProductAdvice
            ? "- Nếu khách hỏi lợi ích/ưu điểm/phân tích/có nên mua, hãy tư vấn chuyên nghiệp: ưu điểm, nhược điểm/lưu ý, phù hợp với ai. Chỉ nhắc sản phẩm trong danh sách nếu thật sự khớp câu hỏi."
            : "- Nếu có sản phẩm, chọn tối đa 2-3 sản phẩm phù hợp nhất, nêu giá và lý do phù hợp.";
        var history = conversation.RecentMessages.Count == 0 ? "Không có." : string.Join("\n", conversation.RecentMessages.TakeLast(10));
        var currentProductLine = conversation.LastReferencedProduct == null ? "Chưa có" : $"{conversation.LastReferencedProduct.Name} (ID {conversation.LastReferencedProduct.Id})";
        return $"Ngữ cảnh 10 tin gần nhất:\n{history}\n\nSản phẩm đang được nhắc tới gần nhất: {currentProductLine}\n\nCâu hỏi khách hàng: {message}\n\nQuy tắc bắt buộc khi trả lời:\n- Không tự gợi ý hoặc liệt kê sản phẩm nếu câu hỏi không có intent mua/tìm/giá/còn hàng/cấu hình/ngân sách/tên sản phẩm rõ ràng.\n- Chỉ tư vấn dựa trên danh sách sản phẩm backend cung cấp bên dưới. Không bịa sản phẩm ngoài danh sách.\n- Nếu có sản phẩm trong ngữ cảnh và khách dùng nó/máy này/con này/bộ này/sản phẩm này/em này thì hiểu là sản phẩm đang được nhắc tới gần nhất, không hỏi lại.\n- Khi phân tích sản phẩm, bắt buộc cố gắng suy luận từ tên, CPU, GPU, RAM, SSD, Mainboard, PSU, Case. Ví dụ Ryzen 7 5700X + RTX 3050 phù hợp eSports, GTA V, Valorant, CS2, học tập/làm việc và stream cơ bản ở mức tham khảo. Chỉ nói chưa đủ dữ liệu khi không đọc được cấu hình nào.\n{productRule}\n- Với FPS/game, không cam kết tuyệt đối; dùng các cụm như \"dự kiến\", \"phù hợp ở mức tham khảo\" vì FPS phụ thuộc setting và bản cập nhật game.\n- Nếu khách hỏi PC/cấu hình/gaming, chỉ dùng PC bộ/cấu hình có sẵn hoặc linh kiện build PC trong danh sách; tuyệt đối không biến chuột/bàn phím/tai nghe/màn hình thành PC đề xuất.\n- Với GTA V: 10-15 triệu chơi ổn Full HD thiết lập vừa/cao tùy linh kiện; 15-30 triệu rất ổn Full HD/2K tùy VGA; 50 triệu dư sức GTA V và có thể hướng tới game nặng hơn/2K/4K.\n- Không nói \"không tìm thấy\" nếu backend đã cung cấp bất kỳ sản phẩm hoặc linh kiện fallback nào.\n- Nếu khách hỏi chính sách, chỉ dùng nguồn sự thật chính sách bên dưới.\n\nNguồn sự thật chính sách KKSHOP:\n{policyKnowledge}\n\nDữ liệu sản phẩm KKSHOP được phép dùng:\n{productLines}";
    }

    private static string ExtractReply(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0) return string.Empty;
        var first = candidates[0];
        if (!first.TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts)) return string.Empty;
        return string.Join("\n", parts.EnumerateArray().Select(p => p.TryGetProperty("text", out var text) ? text.GetString() : null).Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string TrimForLog(string text) => text.Length <= 180 ? text : text[..180] + "...";

    [GeneratedRegex(@"/Products/Detail/(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ProductUrlRegex();

    [GeneratedRegex(@"(?:duoi|khoang|tam|tren|>=|>|<=|<)?\s*\d+(?:[\.,]\d+)?\s*(?:trieu|tr|m|million|k)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BudgetDebugRegex();

    private static string NormalizeCacheKey(string text) => text.Trim().ToLowerInvariant()[..Math.Min(text.Trim().Length, 160)];

    private static readonly string[] GreetingWords = ["chao", "chao ban", "hello", "hi", "alo", "shop oi", "ad oi"];
    private static readonly string[] ContextReferenceWords = ["no", "may nay", "con nay", "bo nay", "san pham nay", "em nay"];
    private static readonly string[] SearchIntentWords = ["mua", "tim", "can", "gia", "bao nhieu", "con hang", "co hang", "goi y", "tu van cau hinh", "build", "ngan sach", "duoi", "tam", "khoang", "shop co san"];
    private static readonly string[] AdviceIntentWords = ["loi ich", "uu diem", "nhuoc diem", "phan tich", "co nen mua", "danh gia", "tot khong", "phu hop", "nen chon", "choi duoc", "choi duoc khong"];
    private static readonly string[] ProductTerms = ["pc", "may tinh", "may bo", "cau hinh", "linh kien", "chuot", "ban phim", "tai nghe", "man hinh", "cpu", "vga", "gpu", "ram", "ssd", "hdd", "nguon", "psu", "case", "tan nhiet", "main", "mainboard", "laptop", "gta", "gaming"];

    private const string SystemPrompt = "Bạn là KKSHOP AI, nhân viên tư vấn PC và linh kiện chuyên nghiệp của KKSHOP. Trả lời thân thiện, không từ chối máy móc. Chỉ liệt kê sản phẩm khi khách có intent mua/tìm/giá/cấu hình/ngân sách/tên sản phẩm rõ ràng. Chỉ dùng dữ liệu sản phẩm và chính sách backend cung cấp; không bịa sản phẩm, giá, thông số, bảo hành, tồn kho, khuyến mãi hoặc chính sách. Nếu thiếu dữ liệu, hỏi lại hoặc fallback hợp lý; không nói không tìm thấy khi backend đã cung cấp sản phẩm/linh kiện. Khi tư vấn gaming/FPS chỉ nói dự kiến/phù hợp ở mức tham khảo, không cam kết FPS tuyệt đối. Trả lời tiếng Việt ngắn gọn, dễ hiểu.";
}
