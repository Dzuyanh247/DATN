using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Datn.PcStore.Services;

public record AiChatResponse(bool Success, string Reply, IReadOnlyList<AiProductContext> SuggestedProducts);

public interface IAiChatService
{
    Task<AiChatResponse> AskAsync(string message, string? sessionId, string? ipAddress, CancellationToken cancellationToken = default);
}

public class GeminiChatService : IAiChatService
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
        var policyAnswer = _shopPolicy.Answer(message);
        if (policyAnswer.IsPolicyQuestion)
        {
            _logger.LogInformation("[AI] Message: {Message}; Intent: POLICY_QA; Scope: POLICY; Products: 0; TopProducts: none", message);
            return new(true, policyAnswer.Reply, []);
        }

        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey)) return new(false, BusyMessage, []);

        var cacheKey = $"ai-chat:{NormalizeCacheKey(message)}";
        if (_cache.TryGetValue<AiChatResponse>(cacheKey, out var cached) && cached != null) return cached;

        var products = await _productSearch.SearchAsync(message, cancellationToken);
        if (IsOutOfScope(message))
        {
            return new(true, "KKSHOP AI chỉ hỗ trợ tư vấn PC, linh kiện, đơn hàng, bảo hành và thanh toán. Bạn cần mình tư vấn cấu hình hoặc sản phẩm nào không?", products);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 10, 15)));

        try
        {
            var prompt = BuildPrompt(message, products, _shopPolicy.BuildKnowledgePrompt());
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

    private bool IsAllowed(string? sessionId, string? ipAddress)
    {
        var key = $"ai-rate:{sessionId ?? ipAddress ?? "unknown"}";
        var count = _cache.Get<int>(key);
        if (count >= 12) return false;
        _cache.Set(key, count + 1, TimeSpan.FromMinutes(1));
        return true;
    }

    private static bool IsOutOfScope(string message)
    {
        var lower = message.ToLowerInvariant();
        return lower.Contains("thời tiết") || lower.Contains("bóng đá") || lower.Contains("chứng khoán") || lower.Contains("nấu ăn");
    }

    private static string BuildPrompt(string message, IReadOnlyList<AiProductContext> products, string policyKnowledge)
    {
        var productLines = products.Count == 0
            ? "Hiện KKSHOP chưa tìm thấy cấu hình phù hợp trong dữ liệu hiện có."
            : string.Join("\n", products.Select((p, i) => $"{i + 1}. Tên: {p.Name}; Giá: {p.Price:N0} đ; Mô tả: {p.Description}; Cấu hình/thông số: {p.Specifications}; Bảo hành: {p.Warranty}; Tồn kho: {p.StockStatus}; Link: {p.Link}; Danh mục: {p.Category}"));
        return $"Câu hỏi khách hàng: {message}\n\nQuy tắc bắt buộc khi trả lời:\n- Chỉ tư vấn dựa trên danh sách sản phẩm backend cung cấp bên dưới. Không bịa sản phẩm ngoài danh sách.\n- Nếu danh sách sản phẩm trống, nói đúng: \"Hiện KKSHOP chưa tìm thấy cấu hình phù hợp trong dữ liệu hiện có\" và gợi ý khách nhập ngân sách hoặc bấm Gặp nhân viên.\n- Nếu có sản phẩm, chọn 2-3 sản phẩm phù hợp nhất, nêu giá và lý do phù hợp.\n- Với FPS/game, không cam kết tuyệt đối; dùng các cụm như \"dự kiến\", \"phù hợp ở mức tham khảo\" vì FPS phụ thuộc setting và bản cập nhật game.\n- Nếu khách hỏi PC/cấu hình/gaming, chỉ tư vấn PC bộ/cấu hình PC có trong danh sách.\n- Nếu khách hỏi chính sách, chỉ dùng nguồn sự thật chính sách bên dưới.\n\nNguồn sự thật chính sách KKSHOP:\n{policyKnowledge}\n\nDữ liệu sản phẩm KKSHOP được phép dùng:\n{productLines}";
    }

    private static string ExtractReply(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0) return string.Empty;
        var first = candidates[0];
        if (!first.TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts)) return string.Empty;
        return string.Join("\n", parts.EnumerateArray().Select(p => p.TryGetProperty("text", out var text) ? text.GetString() : null).Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string NormalizeCacheKey(string text) => text.Trim().ToLowerInvariant()[..Math.Min(text.Trim().Length, 160)];

    private const string SystemPrompt = "Bạn là KKSHOP AI, trợ lý tư vấn bán PC và linh kiện của KKSHOP. Chỉ tư vấn dựa trên dữ liệu sản phẩm và nguồn sự thật chính sách được backend cung cấp. Không bịa sản phẩm, giá, thông số, bảo hành, tồn kho, khuyến mãi hoặc chính sách. Nếu thiếu dữ liệu sản phẩm, nói: Hiện tại hệ thống chưa có đủ thông tin xác nhận. Nếu thiếu dữ liệu chính sách, nói: Hiện tại KKSHOP chưa hỗ trợ hoặc chưa có thông tin xác nhận về nội dung này. Khi tư vấn gaming/FPS chỉ nói dự kiến/phù hợp ở mức tham khảo, không cam kết FPS tuyệt đối. Trả lời tiếng Việt ngắn gọn, dễ hiểu.";
}
