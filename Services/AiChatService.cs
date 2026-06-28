using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Datn.PcStore.Services;

public record AiChatResponse(bool Success, string Reply, IReadOnlyList<AiProductContext> SuggestedProducts, bool AttachProductCards = false, string? RequestId = null);

internal sealed class AiConversationContext
{
    public List<string> RecentMessages { get; } = [];
    public AiProductContext? CurrentProductContext { get; set; }
    public AiProductContext? LastReferencedProduct { get; set; }
    public List<AiProductContext> RecentSuggestedProducts { get; } = [];
    public AiChatIntent? LastIntent { get; set; }
    public string LastUserMessage { get; set; } = string.Empty;
    public decimal? LastBudgetTarget { get; set; }
    public string? LastPurpose { get; set; }
    public string? LastGame { get; set; }
    public string? LastProductType { get; set; }
    public List<AiProductContext> LastRenderedCards { get; } = [];
    public AiProductContext? LastSelectedProduct { get; set; }
    public string? CurrentTopic { get; set; }
    public string? LastComponent { get; set; }
    public string LastSearchKeyword { get; set; } = string.Empty;
    public string LastAIResponse { get; set; } = string.Empty;
    public List<AiProductContext> LastComparedProducts { get; } = [];
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
    ProductQuestion,
    ProductCompare,
    PcBuildAdvice,
    ComponentAdvice,
    CompareRecommendation,
    ProductExtremeQuery,
    ClarifyProductAdvice,
    FriendlySmallTalk,
    GeneralShop
}

public interface IAiChatService
{
    Task<AiChatResponse> AskAsync(string message, string? sessionId, string? ipAddress, CancellationToken cancellationToken = default);
}

public partial class GeminiChatService : IAiChatService
{
    private readonly HttpClient _httpClient;
    private readonly IProductSearchForAiService _productSearch;
    private readonly IMemoryCache _cache;
    private readonly IShopPolicyService _shopPolicy;
    private readonly AiChatOptions _options;
    private readonly ILogger<GeminiChatService> _logger;
    private readonly IWebHostEnvironment _environment;

    public GeminiChatService(HttpClient httpClient, IProductSearchForAiService productSearch, IMemoryCache cache, IShopPolicyService shopPolicy, IOptions<AiChatOptions> options, ILogger<GeminiChatService> logger, IWebHostEnvironment environment)
    {
        _httpClient = httpClient;
        _productSearch = productSearch;
        _cache = cache;
        _shopPolicy = shopPolicy;
        _options = options.Value;
        _logger = logger;
        _environment = environment;
    }

    public async Task<AiChatResponse> AskAsync(string message, string? sessionId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var startedAt = Stopwatch.GetTimestamp();
        var rawMessage = (message ?? string.Empty).Trim();
        message = NormalizeUserMessage(rawMessage);
        Console.WriteLine($"AI_TRACE GeminiChatService.AskAsync ENTER requestId={requestId} sessionId={sessionId ?? "none"} raw={TrimForLog(rawMessage)} normalized={TrimForLog(message)}");
        if (message.Length == 0)
        {
            Console.WriteLine($"AI_TRACE GeminiChatService.AskAsync RETURN_BEFORE_PLAN requestId={requestId} reason=EMPTY_MESSAGE");
            return new(false, "Bạn vui lòng nhập câu hỏi cần tư vấn.", [], false, requestId);
        }
        _logger.LogInformation("[KKSHOP_AI_NORMALIZE] requestId={RequestId}; rawMessage={RawMessage}; normalizedMessage={NormalizedMessage}", requestId, TrimForLog(rawMessage), TrimForLog(message));
        if (message.Length > 500)
        {
            Console.WriteLine($"AI_TRACE GeminiChatService.AskAsync RETURN_BEFORE_PLAN requestId={requestId} reason=MESSAGE_TOO_LONG");
            return new(false, "Câu hỏi quá dài, bạn vui lòng rút gọn dưới 500 ký tự.", [], false, requestId);
        }
        if (!IsAllowed(sessionId, ipAddress))
        {
            Console.WriteLine($"AI_TRACE GeminiChatService.AskAsync RETURN_BEFORE_PLAN requestId={requestId} reason=RATE_LIMIT");
            return new(false, "Bạn đang gửi quá nhanh, vui lòng thử lại sau vài giây.", [], false, requestId);
        }
        var conversation = GetConversationContext(sessionId, ipAddress);
        conversation.RecentMessages.Add($"User: {message}");
        if (conversation.RecentMessages.Count > 10) conversation.RecentMessages.RemoveRange(0, conversation.RecentMessages.Count - 10);

        var lastIntentBefore = conversation.LastIntent;
        var analysis = AnalyzeChatTurn(message, conversation);
        var intent = analysis.Intent;
        Console.WriteLine($"AI_TRACE GeminiChatService.AskAsync ANALYSIS requestId={requestId} intent={IntentName(intent)} budget={analysis.BudgetTarget?.ToString(CultureInfo.InvariantCulture) ?? "none"} purpose={analysis.Purpose ?? "none"} game={analysis.Game ?? "none"} productType={analysis.ProductType ?? "none"}");
        if ((analysis.BudgetTarget.HasValue && analysis.BudgetTarget != conversation.LastBudgetTarget) || (lastIntentBefore == AiChatIntent.PcBuildAdvice && intent == AiChatIntent.ComponentAdvice))
        {
            conversation.LastRenderedCards.Clear();
            conversation.RecentSuggestedProducts.Clear();
            conversation.CurrentProductContext = null;
            conversation.LastReferencedProduct = null;
        }
        var productFromUrl = await TryResolveProductUrlAsync(message, cancellationToken);
        if (productFromUrl != null)
        {
            conversation.CurrentProductContext = productFromUrl;
            conversation.LastReferencedProduct = productFromUrl;
        }
        if (intent == AiChatIntent.Greeting)
        {
            Console.WriteLine($"AI_TRACE GeminiChatService.AskAsync RETURN_BEFORE_PLAN requestId={requestId} reason=GREETING");
            return new(true, "Chào bạn! KKSHOP AI có thể hỗ trợ tư vấn PC, linh kiện, cấu hình, đơn hàng, bảo hành và thanh toán. Bạn cần mình hỗ trợ phần nào ạ?", [], false, requestId);
        }
        if (intent == AiChatIntent.FriendlySmallTalk)
        {
            Console.WriteLine($"AI_TRACE GeminiChatService.AskAsync RETURN_BEFORE_PLAN requestId={requestId} reason=FRIENDLY_SMALLTALK");
            return new(true, "Mình vẫn ổn và luôn sẵn sàng hỗ trợ bạn đây ạ 😄 Bạn cần KKSHOP tư vấn PC, linh kiện hay kiểm tra thông tin sản phẩm nào không?", [], false, requestId);
        }
        if (intent == AiChatIntent.OutOfScope)
        {
            Console.WriteLine($"AI_TRACE GeminiChatService.AskAsync RETURN_BEFORE_PLAN requestId={requestId} reason=OUT_OF_SCOPE");
            return new(true, "Nội dung này hơi ngoài phạm vi hỗ trợ của KKSHOP AI rồi ạ. Mình có thể hỗ trợ bạn về PC, linh kiện, cấu hình, đơn hàng, bảo hành hoặc thanh toán nhé.", [], false, requestId);
        }
        if (intent == AiChatIntent.ProductCompare && !HasContextReference(message, conversation) && !HasConcreteProductSignal(message))
        {
            Console.WriteLine($"AI_TRACE GeminiChatService.AskAsync RETURN_BEFORE_PLAN requestId={requestId} reason=COMPARE_NEEDS_PRODUCTS");
            return new(true, "Bạn gửi giúp mình tên hoặc link 2 sản phẩm cần so sánh nhé. Có đủ thông tin, KKSHOP sẽ so sánh giá, cấu hình/thông số, điểm mạnh và nên chọn mẫu nào theo nhu cầu của bạn ạ.", [], false, requestId);
        }
        if (intent == AiChatIntent.ClarifyProductAdvice && !HasContextReference(message, conversation))
        {
            Console.WriteLine($"AI_TRACE GeminiChatService.AskAsync RETURN_BEFORE_PLAN requestId={requestId} reason=CLARIFY_PRODUCT_ADVICE");
            return new(true, "Bạn muốn mình phân tích sản phẩm hoặc dòng sản phẩm nào ạ? Hãy gửi tên đầy đủ, nhu cầu sử dụng hoặc link sản phẩm để mình tư vấn ưu/nhược điểm kỹ hơn nhé.", [], false, requestId);
        }

        if (intent == AiChatIntent.CompareRecommendation)
        {
            var selectedList = PickBestProducts(conversation.RecentSuggestedProducts, conversation.LastBudgetTarget);
            if (selectedList.Count > 0)
            {
                conversation.LastSelectedProduct = selectedList[0];
                SaveRecentSuggestedProducts(conversation, selectedList);
                var reply = BuildBestPickReply(selectedList, analysis.Game ?? conversation.LastGame, conversation.LastPurpose, conversation.LastBudgetTarget);
                UpdateConversationAfterRule(conversation, message, analysis, selectedList);
                LogPipelineDebug(requestId, sessionId, message, analysis, conversation, selectedList, false, false, "BEST_PICK_CONSULT", lastIntentBefore, null);
                return new(true, reply, [], false, requestId);
            }
            Console.WriteLine($"AI_TRACE GeminiChatService.AskAsync RETURN_BEFORE_PLAN requestId={requestId} reason=COMPARE_RECOMMENDATION_NO_RECENT_PRODUCTS");
            return new(true, "Mình chưa có danh sách sản phẩm vừa đề xuất trong phiên này. Bạn cho mình biết nhu cầu/ngân sách để mình gợi ý rồi chọn mẫu đáng mua nhất nhé.", [], false, requestId);
        }

        if (intent == AiChatIntent.ProductExtremeQuery || intent == AiChatIntent.PcBuildAdvice || intent == AiChatIntent.ComponentAdvice)
        {
            var salesPlan = BuildSalesSearchPlan(message, analysis, conversation);
            Console.WriteLine($"AI_TRACE GeminiChatService.AskAsync BEFORE_PRODUCT_PLAN_SEARCH requestId={requestId} intent={IntentName(intent)} budget={salesPlan.BudgetTarget?.ToString(CultureInfo.InvariantCulture) ?? "none"} scope={salesPlan.CategoryScope} signals={string.Join(",", salesPlan.SearchSignals)}");
            var productsByRule = LimitProductCards(intent == AiChatIntent.ProductExtremeQuery
                ? await _productSearch.QueryByIntentAsync(IntentName(intent), analysis.ProductType, analysis.PriceMode, analysis.BudgetTarget, cancellationToken)
                : await _productSearch.SearchByPlanAsync(salesPlan, cancellationToken));
            var reply = BuildRulePipelineReply(analysis, productsByRule);
            if (productsByRule.Count > 0) SaveRecentSuggestedProducts(conversation, productsByRule);
            UpdateConversationAfterRule(conversation, message, analysis, productsByRule);
            LogPlanner(requestId, sessionId, message, salesPlan, productsByRule);
            LogPipelineDebug(requestId, sessionId, message, analysis, conversation, productsByRule, true, false, IntentName(intent), lastIntentBefore, null);
            return new(true, reply, productsByRule, productsByRule.Count > 0, requestId);
        }

        var policyAnswer = _shopPolicy.Answer(message);
        if (policyAnswer.IsPolicyQuestion)
        {
            _logger.LogInformation("[AI] Message: {Message}; Intent: POLICY_QA; Scope: POLICY; Products: 0; TopProducts: none", message);
            return new(true, policyAnswer.Reply, [], false, requestId);
        }

        if (intent != AiChatIntent.ProductQuestion && !HasContextReference(message, conversation) && IsConfigurationConsultation(message) && analysis.ProductType == null)
        {
            var productsForConfig = await _productSearch.SearchAsync(message, cancellationToken);
            var reply = BuildConfigurationConsultationReply(message, productsForConfig);
            if (productsForConfig.Count > 0)
            {
                SaveRecentSuggestedProducts(conversation, productsForConfig);
            }
            conversation.RecentMessages.Add($"AI: {reply}");
            LogDebugState(requestId, sessionId, message, intent, conversation, productsForConfig, productsForConfig.Count > 0, "config-rule-based", reply.Length, productsForConfig.Count, null);
            return new(true, reply, productsForConfig, productsForConfig.Count > 0, requestId);
        }

        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            var noProviderFallback = BuildFallbackReply(message, intent, conversation);
            if (string.IsNullOrWhiteSpace(noProviderFallback))
                noProviderFallback = BuildGenericSalesFallback(message, analysis, conversation);
            LogDebugState(requestId, sessionId, message, intent, conversation, [], false, "provider-disabled-fallback", noProviderFallback.Length, 0, null);
            conversation.LastAIResponse = noProviderFallback;
            return new(true, noProviderFallback, [], false, requestId);
        }

        var contextProduct = ResolveContextProduct(message, conversation);
        var isAnalysisIntent = intent is AiChatIntent.ProductQuestion or AiChatIntent.ProductAnalysis or AiChatIntent.ProductBenefits or AiChatIntent.ProductProsCons or AiChatIntent.ProductRecommendation or AiChatIntent.ProductAdvice or AiChatIntent.ProductCompare;
        var shouldSearchProducts = IsSearchMode(intent, message, conversation) || productFromUrl != null;
        IReadOnlyList<AiProductContext> products = productFromUrl != null ? [productFromUrl] : contextProduct != null && isAnalysisIntent ? [contextProduct] : shouldSearchProducts ? await _productSearch.SearchAsync(message, cancellationToken) : [];
        products = LimitProductCards(products);
        if (products.Count > 0)
        {
            conversation.CurrentProductContext = products[0];
            conversation.LastReferencedProduct = products[0];
        }
        var shouldAttachProductCards = ShouldAttachProductCards(intent, message);
        if (isAnalysisIntent && contextProduct != null && HasContextReference(message, conversation))
        {
            var ruleBasedReply = BuildRuleBasedProductAnalysis(message, contextProduct, conversation.RecentSuggestedProducts);
            LogDebugState(requestId, sessionId, message, intent, conversation, [], false, "rule-based", ruleBasedReply.Length, 0, null);
            conversation.RecentMessages.Add($"AI: {ruleBasedReply}");
            return new(true, ruleBasedReply, [], false, requestId);
        }
        LogDebugState(requestId, sessionId, message, intent, conversation, products, shouldAttachProductCards, shouldSearchProducts ? "search-mode-pending" : "consult-mode-pending", 0, shouldAttachProductCards ? products.Count : 0, null);
        if (shouldSearchProducts && products.Count == 0)
        {
            var normalizedMessage = RemoveDiacritics(message.ToLowerInvariant());
            if (ContainsAny(normalizedMessage, "pc", "cau hinh", "build", "choi game", "gaming", "valorant", "gta", "ngan sach"))
                return new(true, "Mình chưa tìm thấy PC bộ/cấu hình phù hợp trong dữ liệu hiện tại, nhưng có thể tư vấn build từ linh kiện nếu shop đã có đủ CPU, main, RAM, VGA, SSD, nguồn và case.", [], false, requestId);

            return new(true, "Mình chưa tìm thấy sản phẩm phù hợp với từ khóa này trong dữ liệu KKSHOP. Bạn thử gửi tên đầy đủ hơn, loại linh kiện hoặc ngân sách để mình kiểm tra chính xác hơn nhé.", [], false, requestId);
        }

        var cacheKey = $"ai-chat:{sessionId ?? ipAddress}:{intent}:{conversation.LastReferencedProduct?.Id}:{NormalizeCacheKey(message)}";
        var isPcAdviceCacheDisabled = _environment.IsDevelopment() && (intent == AiChatIntent.PcBuildAdvice || (analysis.BudgetTarget.HasValue && ContainsAny(analysis.NormalizedMessage, "pc", "cau hinh", "build", "gaming", "valorant")));
        if (!isPcAdviceCacheDisabled && _cache.TryGetValue<AiChatResponse>(cacheKey, out var cached) && cached != null)
        {
            Console.WriteLine($"AI_TRACE GeminiChatService.AskAsync CACHE_HIT requestId={requestId} key={cacheKey}");
            return cached;
        }
        if (isPcAdviceCacheDisabled) Console.WriteLine($"AI_TRACE GeminiChatService.AskAsync CACHE_DISABLED_DEV_PC_ADVICE requestId={requestId} key={cacheKey}");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 20, 60)));

        try
        {
            var prompt = BuildPrompt(message, products, _shopPolicy.BuildKnowledgePrompt(), intent, conversation);
            var request = new
            {
                systemInstruction = new { parts = new[] { new { text = SystemPrompt } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
                generationConfig = new { temperature = 0.2, maxOutputTokens = Math.Clamp(_options.MaxOutputTokens, 800, 1200) }
            };
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(_options.Model)}:generateContent?key={Uri.EscapeDataString(_options.ApiKey)}";
            var response = await _httpClient.PostAsJsonAsync(url, request, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(timeout.Token);
                var isProviderBusy = response.StatusCode == HttpStatusCode.ServiceUnavailable || response.StatusCode == HttpStatusCode.TooManyRequests;
                var fallback = BuildFallbackReply(message, intent, conversation);
                if (string.IsNullOrWhiteSpace(fallback)) fallback = BuildGenericSalesFallback(message, analysis, conversation);
                var fallbackReply = fallback;
                var logBody = TrimForLog(body, isProviderBusy ? 120 : 180);

                if (isProviderBusy)
                {
                    _logger.LogWarning("[KKSHOP_AI_PROVIDER_BUSY] requestId={RequestId}; sessionId={SessionId}; status={StatusCode}; userMessage={UserMessage}; intent={Intent}; productContext={ProductContext}; body={Body}", requestId, sessionId, response.StatusCode, TrimForLog(message), intent, conversation.CurrentProductContext?.Name ?? "none", logBody);
                }
                else
                {
                    _logger.LogWarning("[KKSHOP_AI_ERROR] requestId={RequestId}; sessionId={SessionId}; status={StatusCode}; userMessage={UserMessage}; intent={Intent}; productContext={ProductContext}; body={Body}", requestId, sessionId, response.StatusCode, TrimForLog(message), intent, conversation.CurrentProductContext?.Name ?? "none", logBody);
                }

                LogDebugState(requestId, sessionId, message, intent, conversation, products, false, isProviderBusy ? "provider-busy" : $"provider-status-{(int)response.StatusCode}", fallbackReply.Length, 0, logBody);
                return new(true, fallbackReply, [], false, requestId);
            }
            using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(timeout.Token), cancellationToken: timeout.Token);
            var tokenUsage = ExtractTokenUsage(document.RootElement);
            var reply = ExtractReply(document.RootElement);
            if (string.IsNullOrWhiteSpace(reply)) reply = BuildFallbackReply(message, intent, conversation);
            if (string.IsNullOrWhiteSpace(reply)) reply = BuildGenericSalesFallback(message, analysis, conversation);
            if (!IsCompleteReply(reply)) reply = CompleteSentence(reply.Trim());
            else reply = CompleteSentence(reply.Trim());
            IReadOnlyList<AiProductContext> outgoingProducts = shouldAttachProductCards ? products : [];
            if (outgoingProducts.Count > 0) SaveRecentSuggestedProducts(conversation, outgoingProducts);
            conversation.RecentMessages.Add($"AI: {reply}");
            LogDebugState(requestId, sessionId, message, intent, conversation, products, shouldAttachProductCards, "ok", reply.Length, outgoingProducts.Count, null);
            LogAiTurnMetrics(requestId, intent, shouldSearchProducts, tokenUsage, Stopwatch.GetElapsedTime(startedAt));
            var result = new AiChatResponse(true, reply, outgoingProducts, shouldAttachProductCards, requestId);
            if (!isPcAdviceCacheDisabled) _cache.Set(cacheKey, result, TimeSpan.FromMinutes(7));
            return result;
        }
        catch (OperationCanceledException ex)
        {
            var fallback = BuildFallbackReply(message, intent, conversation);
            LogDebugState(requestId, sessionId, message, intent, conversation, products, false, "timeout", fallback.Length, 0, ex.ToString());
            _logger.LogError(ex, "[KKSHOP_AI_ERROR] requestId={RequestId}; sessionId={SessionId}; userMessage={UserMessage}; intent={Intent}; productContext={ProductContext}", requestId, sessionId, TrimForLog(message), intent, conversation.CurrentProductContext?.Name ?? "none");
            if (string.IsNullOrWhiteSpace(fallback)) fallback = BuildGenericSalesFallback(message, analysis, conversation);
            conversation.LastAIResponse = fallback;
            return new(true, fallback, [], false, requestId);
        }
        catch (Exception ex)
        {
            var fallback = BuildFallbackReply(message, intent, conversation);
            LogDebugState(requestId, sessionId, message, intent, conversation, products, false, "exception", fallback.Length, 0, ex.ToString());
            _logger.LogError(ex, "[KKSHOP_AI_ERROR] requestId={RequestId}; sessionId={SessionId}; exceptionType={ExceptionType}; exceptionMessage={ExceptionMessage}; userMessage={UserMessage}; intent={Intent}; productContext={ProductContext}", requestId, sessionId, ex.GetType().FullName, ex.Message, TrimForLog(message), intent, conversation.CurrentProductContext?.Name ?? "none");
            if (string.IsNullOrWhiteSpace(fallback)) fallback = BuildGenericSalesFallback(message, analysis, conversation);
            conversation.LastAIResponse = fallback;
            return new(true, fallback, [], false, requestId);
        }
    }




    private sealed record ChatTurnAnalysis(AiChatIntent Intent, string NormalizedMessage, decimal? BudgetTarget, string? Game, string? Purpose, string? ProductType, string PriceMode);

    private static string NormalizeUserMessage(string message)
    {
        var value = (message ?? string.Empty).Trim();
        if (value.Length == 0) return string.Empty;
        value = Regex.Replace(value, @"\s+", " ");
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["pc20tr"] = "pc 20 triệu", ["pc30tr"] = "pc 30 triệu", ["pc50tr"] = "pc 50 triệu",
            ["chuot"] = "chuột", ["chuot re"] = "chuột rẻ", ["mouse"] = "chuột",
            ["ban phim"] = "bàn phím", ["keyboard"] = "bàn phím", ["ban phim co"] = "bàn phím cơ",
            ["man hinh"] = "màn hình", ["tai nghe"] = "tai nghe", ["bao hanh"] = "bảo hành",
            ["con hang"] = "còn hàng", ["ton dien"] = "tốn điện", ["loi ich"] = "lợi ích"
        };
        foreach (var item in replacements)
            value = Regex.Replace(value, $@"(?<!\p{{L}}){Regex.Escape(item.Key)}(?!\p{{L}})", item.Value, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        value = Regex.Replace(value, @"\b(pc)\s*(\d{1,3})\s*(tr|trieu|m)\b", "$1 $2 triệu", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        value = Regex.Replace(value, @"\b(\d{1,3})\s*(tr|trieu|m)\b", "$1 triệu", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return value.Trim();
    }

    private static ChatTurnAnalysis AnalyzeChatTurn(string message, AiConversationContext? conversation = null)
    {
        var n = RemoveDiacritics((message ?? string.Empty).ToLowerInvariant());
        var productType = DetectChatProductType(n);
        var priceMode = ContainsAny(n, "dat nhat", "cao nhat", "dat tien nhat", "gia cao nhat") ? "highest" : ContainsAny(n, "re nhat", "thap nhat", "gia thap nhat") ? "lowest" : "normal";
        var budget = TryExtractChatBudget(n, out var money) ? money : (decimal?)null;
        var game = ContainsAny(n, "valorant") ? "Valorant" : ContainsAny(n, "gta") ? "GTA V" : ContainsAny(n, "cs2") ? "CS2" : ContainsAny(n, "pubg") ? "PUBG" : ContainsAny(n, " lol", " lien minh") ? "LOL" : null;
        var purpose = ContainsAny(n, "choi", "game", "gaming", "valorant", "gta", "cs2", "pubg") ? "Gaming" : ContainsAny(n, "stream") ? "Stream" : ContainsAny(n, "do hoa", "render") ? "Đồ họa / render" : null;
        var hasContextProductReference = conversation != null && HasContextReference(message, conversation);
        var hasSpecificProductSignal = HasConcreteProductSignal(message);
        var hasProductQuestion = IsProductQuestion(n);
        AiChatIntent intent;
        if (ContainsAny(n, "con mau khac", "mau khac", "san pham khac", "xem them", "goi y them") && conversation?.RecentSuggestedProducts.Count > 0)
            intent = conversation.LastIntent is AiChatIntent.ComponentAdvice ? AiChatIntent.ComponentAdvice : AiChatIntent.PcBuildAdvice;
        else if ((hasContextProductReference || hasSpecificProductSignal) && hasProductQuestion) intent = AiChatIntent.ProductQuestion;
        else if (priceMode != "normal" && ContainsAny(n, "san pham", "pc", "ram", "vga", "gpu", "cpu", "linh kien", "chuot", "ban phim", "man hinh", "tai nghe")) intent = AiChatIntent.ProductExtremeQuery;
        else if (ContainsAny(n, "cai nao dang mua", "san pham ban de xuat", "trong may cai", "nen chon cai nao", "con nao ngon", "tot nhat trong danh sach", "mau nao dang mua") || (ContainsAny(n, "loi ich cua no", "loi ich") && ContainsAny(n, "cai nao", "san pham ban de xuat", "may cai tren"))) intent = AiChatIntent.CompareRecommendation;
        else if (productType != null || ContainsAny(n, "linh kien ma", "khong phai pc", "thanh ram")) intent = AiChatIntent.ComponentAdvice;
        else if (!hasContextProductReference && !hasSpecificProductSignal && (ContainsAny(n, "tu van cau hinh", "build pc", "pc choi", "may choi", "may gaming", "pc ", "cau hinh stream", "cau hinh") || (budget.HasValue && (purpose == "Gaming" || ContainsAny(n, "pc", "may tinh", "cau hinh", "build"))))) intent = AiChatIntent.PcBuildAdvice;
        else intent = DetectIntent(message);
        return new(intent, n, budget, game, purpose, productType, priceMode);
    }

    private static string? DetectChatProductType(string n)
    {
        if (ContainsAny(n, "thanh ram", " ram", "ram ", "bo nho trong")) return "RAM";
        if (ContainsAny(n, "vga", "gpu", "card man hinh")) return "VGA";
        if (ContainsAny(n, "cpu", "chip", "vi xu ly")) return "CPU";
        if (ContainsAny(n, "mainboard", "bo mach chu", " main")) return "Mainboard";
        if (ContainsAny(n, "ssd", "hdd", "o cung")) return "Storage";
        if (ContainsAny(n, "nguon", "psu")) return "PSU";
        if (ContainsAny(n, "vo case", " case")) return "Case";
        if (ContainsAny(n, "tan nhiet", "cooler")) return "Cooler";
        if (ContainsAny(n, "man hinh", "monitor")) return "Monitor";
        if (ContainsAny(n, "chuot", "mouse")) return "Mouse";
        if (ContainsAny(n, "ban phim", "keyboard")) return "Keyboard";
        if (ContainsAny(n, "tai nghe", "headset", "headphone")) return "Headphone";
        return null;
    }

    private static AiSalesSearchPlan BuildSalesSearchPlan(string message, ChatTurnAnalysis analysis, AiConversationContext conversation)
    {
        var budget = analysis.BudgetTarget ?? conversation.LastBudgetTarget;
        var purpose = analysis.Purpose ?? conversation.LastPurpose ?? InferPurpose(analysis.NormalizedMessage);
        var game = analysis.Game ?? conversation.LastGame;
        var productType = analysis.ProductType ?? (analysis.Intent == AiChatIntent.ComponentAdvice ? conversation.LastProductType : null);
        var scope = analysis.Intent == AiChatIntent.ComponentAdvice ? "COMPONENT" : "PC";
        var signals = new List<string>();
        var excludePrevious = ContainsAny(analysis.NormalizedMessage, "con mau khac", "mau khac", "san pham khac", "xem them", "goi y them");
        if (!string.IsNullOrWhiteSpace(game)) signals.Add(game);
        if (!string.IsNullOrWhiteSpace(productType)) signals.Add(productType);
        if (string.Equals(purpose, "Gaming", StringComparison.OrdinalIgnoreCase)) signals.AddRange(["rtx", "16gb", "ssd", "gaming"]);
        if (ContainsAny(analysis.NormalizedMessage, "rgb", "led")) signals.AddRange(["rgb", "led", "argb"]);
        foreach (Match match in Regex.Matches(message, @"\b(?:rtx|gtx|rx|ddr4|ddr5|b650|x670|h610|b760|z790|g304|g502|akko|logitech|razer|asus|msi|gigabyte)\w*\b", RegexOptions.IgnoreCase))
            signals.Add(match.Value);
        return new AiSalesSearchPlan(IntentName(analysis.Intent), scope, productType, budget, null, budget, purpose, game, signals.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), scope == "PC", analysis.PriceMode, excludePrevious ? conversation.RecentSuggestedProducts.Select(p => p.Id).ToList() : null);
    }

    private static string? InferPurpose(string normalized)
        => ContainsAny(normalized, "ai", "machine learning", "hoc ai") ? "AI / GPU VRAM lớn"
        : ContainsAny(normalized, "render", "do hoa", "edit", "video") ? "Đồ họa / render"
        : ContainsAny(normalized, "van phong", "office", "hoc tap") ? "Văn phòng"
        : ContainsAny(normalized, "game", "gaming", "choi", "valorant", "gta", "cs2", "pubg") ? "Gaming"
        : null;

    private void LogPlanner(string requestId, string? sessionId, string message, AiSalesSearchPlan plan, IReadOnlyList<AiProductContext> products)
        => _logger.LogInformation("[KKSHOP_AI_PLANNER] requestId={RequestId}; sessionId={SessionId}; userMessage={Message}; intent={Intent}; scope={Scope}; component={Component}; budgetTarget={BudgetTarget}; purpose={Purpose}; game={Game}; signals={Signals}; allowBuildFallback={BuildFallback}; productsFound={Count}; finalProducts={Products}",
            requestId, sessionId ?? "none", TrimForLog(message), plan.Intent, plan.CategoryScope, plan.ComponentType ?? "none", plan.BudgetTarget, plan.Purpose ?? "none", plan.Game ?? "none", string.Join(",", plan.SearchSignals), plan.AllowBuildFallback, products.Count, string.Join(" | ", products.Select(p => $"{p.Id}:{p.Name}:{p.Price:N0}")));

    private static bool TryChatMoney(string value, out decimal result)
    {
        result = 0;
        if (!decimal.TryParse(value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)) return false;
        result = amount < 1000 ? amount * 1_000_000 : amount;
        return true;
    }

    private static string IntentName(AiChatIntent intent) => intent switch { AiChatIntent.PcBuildAdvice => "PC_BUILD_ADVICE", AiChatIntent.ComponentAdvice => "COMPONENT_ADVICE", AiChatIntent.CompareRecommendation => "COMPARE_RECOMMENDATION", AiChatIntent.ProductExtremeQuery => "PRODUCT_EXTREME_QUERY", _ => intent.ToString() };

    private static AiProductContext? PickBestProduct(IReadOnlyList<AiProductContext> products, decimal? budget) => products.Count == 0 ? null : products.OrderByDescending(p => ScoreRecommendation(p, budget)).First();

    private static IReadOnlyList<AiProductContext> PickBestProducts(IReadOnlyList<AiProductContext> products, decimal? budget)
        => products.OrderByDescending(p => ScoreRecommendation(p, budget)).Take(3).ToList();

    private static decimal ScoreRecommendation(AiProductContext p, decimal? budget)
    {
        var text = RemoveDiacritics($"{p.Name} {p.Specifications} {p.Description}".ToLowerInvariant());
        decimal score = p.StockQuantity > 0 ? 20 : 0;
        if (text.Contains("rtx 4090")) score += 60; else if (text.Contains("rtx 4080")) score += 55; else if (text.Contains("rtx 4070")) score += 48; else if (text.Contains("rtx 4060")) score += 40; else if (text.Contains("rtx 3060")) score += 32; else if (text.Contains("rtx 3050")) score += 24;
        if (text.Contains("32gb")) score += 12; else if (text.Contains("16gb")) score += 8;
        if (text.Contains("1tb")) score += 8; else if (text.Contains("512")) score += 5;
        if (budget.HasValue) score += Math.Max(0, 25 - Math.Abs(p.Price - budget.Value) / Math.Max(1, budget.Value) * 25);
        return score;
    }

    private static string BuildCompareRecommendationReply(AiProductContext product, string? game, string? purpose)
        => $"Trong các mẫu vừa đề xuất, mẫu đáng mua nhất là: {product.Name}\n\nLý do:\n- Cân bằng cấu hình/giá tốt hơn trong danh sách vừa tư vấn.\n- Phù hợp {(purpose ?? "nhu cầu bạn nêu")}{(string.IsNullOrWhiteSpace(game) ? string.Empty : $" / {game}")}.\n- {product.StockStatus}, bảo hành {product.Warranty}.\n- Thông số nổi bật: {product.Specifications}\n\nĐiểm cần lưu ý: nếu bạn thường mở thêm Discord/Chrome hoặc chơi game nặng, nên ưu tiên nâng RAM/SSD khi cấu hình hiện tại còn thấp.";


    private static string BuildBestPickReply(IReadOnlyList<AiProductContext> products, string? game, string? purpose, decimal? budget)
    {
        var labels = new[] { "🥇 Đáng mua nhất", "🥈 Hiệu năng / giá tốt nhất", "🥉 Tiết kiệm nhất" };
        var lines = new List<string> { "Mình sẽ chọn trong danh sách vừa đề xuất, không tìm thêm sản phẩm mới:", string.Empty };
        for (var i = 0; i < products.Count && i < labels.Length; i++)
        {
            var p = products[i];
            lines.Add($"{labels[i]}\n{p.Name} — {p.Price:N0} đ");
            lines.Add($"Lý do: {p.StockStatus.ToLowerInvariant()}, bảo hành {p.Warranty}, phù hợp {(purpose ?? "nhu cầu bạn nêu")}{(string.IsNullOrWhiteSpace(game) ? string.Empty : $" / {game}")}.");
            if (!string.IsNullOrWhiteSpace(p.Specifications)) lines.Add($"Điểm nổi bật: {p.Specifications}");
            lines.Add(string.Empty);
        }
        lines.Add("Nếu bạn muốn, mình có thể phân tích kỹ mẫu hạng 1 về điện năng, nhiệt độ, hiệu năng game và khả năng nâng cấp.");
        return string.Join("\n", lines).Trim();
    }

    private static string BuildRulePipelineReply(ChatTurnAnalysis a, IReadOnlyList<AiProductContext> products)
    {
        if (a.Intent == AiChatIntent.ProductExtremeQuery)
            return products.Count == 0 ? "Hiện KKSHOP chưa có sản phẩm phù hợp để xếp theo giá." : $"Sản phẩm có giá {(a.PriceMode == "lowest" ? "thấp nhất" : "cao nhất")} hiện tại tại KKSHOP là:";
        if (a.Intent == AiChatIntent.ComponentAdvice)
            return $"✓ Đã nhận nhu cầu\n\nLoại sản phẩm: {ProductTypeDisplay(a.ProductType)}\nMục đích: {a.Purpose ?? "Chưa nêu rõ"}\nGame: {a.Game ?? "Chưa nêu rõ"}\n\nGợi ý nhanh:\n- Chỉ lọc đúng nhóm {ProductTypeDisplay(a.ProductType)}, không đề xuất PC nguyên bộ.\n- Nếu là RAM chơi game, tối thiểu nên có 8GB và khuyến nghị 16GB để mở thêm Discord/Chrome.\n- Chuẩn DDR4/DDR5 cần phụ thuộc mainboard đang dùng.\n\n{(products.Count == 0 ? "Mình chưa thấy mẫu khớp tuyệt đối; dưới đây là các lựa chọn gần nhất nếu hệ thống tìm được." : $"{ProductTypeDisplay(a.ProductType)} đề xuất từ dữ liệu KKSHOP:")}";
        if (products.Count == 0)
            return $"✓ Đã nhận nhu cầu\n\nNgân sách: {(a.BudgetTarget.HasValue ? a.BudgetTarget.Value.ToString("N0") + " đ" : "Chưa nêu rõ")}\nMục đích: {a.Purpose ?? "Tư vấn cấu hình"}\nGame: {a.Game ?? "Chưa nêu rõ"}\n\n{BuildAdvisorFallback(a.BudgetTarget, a.Purpose, a.Game)}";
        return $"✓ Đã nhận nhu cầu\n\nNgân sách: {(a.BudgetTarget.HasValue ? a.BudgetTarget.Value.ToString("N0") + " đ" : "Chưa nêu rõ")}\nMục đích: {a.Purpose ?? "Tư vấn cấu hình"}\nGame: {a.Game ?? "Chưa nêu rõ"}\n\nPC đề xuất từ dữ liệu KKSHOP:";
    }

    private static string BuildAdvisorFallback(decimal? budget, string? purpose, string? game)
    {
        var label = budget.HasValue ? budget.Value.ToString("N0") : "mức bạn yêu cầu";
        var intro = $"Hiện KKSHOP chưa có PC nguyên bộ đúng khoảng {label} đ trong dữ liệu. Mình có thể gợi ý cấu hình build theo ngân sách này và ưu tiên linh kiện đang có trong shop.";
        if (!budget.HasValue) return intro;
        if (budget.Value >= 45_000_000m)
            return intro + "\n\nGợi ý build 50 triệu gaming" + (string.IsNullOrWhiteSpace(game) ? string.Empty : $"/{game}") + ":\n- CPU i7/Ryzen 7\n- VGA RTX 4070 Super/4070 Ti Super hoặc tương đương\n- RAM 32GB\n- SSD 1TB NVMe\n- PSU 750W Gold\n- Mainboard B760/B650 hoặc phù hợp CPU";
        if (budget.Value >= 28_000_000m)
            return intro + "\n\nGợi ý build 30 triệu gaming:\n- CPU i5/Ryzen 5 hoặc i7/Ryzen 7\n- VGA RTX 4060 Ti/4070 hoặc tương đương\n- RAM 32GB\n- SSD 1TB\n- PSU 650W-750W";
        if (budget.Value >= 18_000_000m)
            return intro + "\n\nGợi ý build 20 triệu gaming:\n- CPU i5/Ryzen 5\n- VGA RTX 3060/4060 hoặc tương đương\n- RAM 16GB\n- SSD 512GB/1TB\n- PSU 550W-650W";
        return intro;
    }

    private static bool TryExtractChatBudget(string normalized, out decimal money)
    {
        money = 0;
        var match = BudgetDebugRegex().Match(normalized);
        if (!match.Success) return false;
        return !LooksLikeHardwareModel(normalized, match.Index) && TryChatMoney(Regex.Match(match.Value, @"\d+(?:[\.,]\d+)?").Value, out money);
    }

    private static string TryExtractChatBudgetText(string normalized)
    {
        var match = BudgetDebugRegex().Match(normalized);
        return match.Success && !LooksLikeHardwareModel(normalized, match.Index) ? match.Value : string.Empty;
    }

    private static bool LooksLikeHardwareModel(string normalized, int matchIndex)
    {
        var start = Math.Max(0, matchIndex - 16);
        var length = Math.Min(normalized.Length - start, 36);
        var window = normalized.Substring(start, length);
        return Regex.IsMatch(window, @"\b(?:rtx|gtx|rx)\s*\d{3,4}\b|\bi[3579][-\s]*\d{4,5}[a-z]*\b|\bryzen\s*[3579]\s*\d{3,5}[a-z]*\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool IsPowerQuestion(string message) => ContainsAny(RemoveDiacritics((message ?? string.Empty).ToLowerInvariant()), "ton dien", "hao dien", "an dien", "dien nang", "cong suat", "nguon bao nhieu", "psu bao nhieu");

    private static bool IsThermalQuestion(string message) => ContainsAny(RemoveDiacritics((message ?? string.Empty).ToLowerInvariant()), "nong khong", "nhiet do", "co nong", "mat khong");

    private static bool IsProductQuestion(string normalized)
        => ContainsAny(normalized, "ton dien", "hao dien", "an dien", "dien nang", "cong suat", "nguon bao nhieu", "psu bao nhieu", "nong khong", "on khong", "co tot khong", "tot khong", "co nen mua", "dang mua khong", "uu diem", "nhuoc diem", "loi ich", "danh gia", "phan tich", "bao hanh", "bao lau", "con hang", "ton kho", "gia bao nhieu", "gia", "rgb", "led", "choi duoc khong", "choi game duoc khong", "nang cap duoc khong", "phu hop sinh vien");

    private static string ProductTypeDisplay(string? t) => t switch { "RAM" => "RAM", "VGA" => "VGA", "CPU" => "CPU", "Mainboard" => "Mainboard", "Storage" => "SSD/HDD", "PSU" => "Nguồn", "Case" => "Vỏ case", "Cooler" => "Tản nhiệt", "Monitor" => "Màn hình", "Mouse" => "Chuột", "Keyboard" => "Bàn phím", "Headphone" => "Tai nghe", _ => "Linh kiện" };

    private static void UpdateConversationAfterRule(AiConversationContext c, string message, ChatTurnAnalysis a, IReadOnlyList<AiProductContext> products)
    {
        c.LastIntent = a.Intent; c.LastUserMessage = message; c.LastSearchKeyword = message; c.LastBudgetTarget = a.BudgetTarget ?? c.LastBudgetTarget; c.LastPurpose = a.Purpose ?? c.LastPurpose; c.LastGame = a.Game ?? c.LastGame; c.LastProductType = a.ProductType ?? c.LastProductType; c.CurrentTopic = a.ProductType ?? a.Purpose ?? a.Game ?? a.Intent.ToString(); c.LastComponent = a.ProductType ?? c.LastComponent; c.LastRenderedCards.Clear(); c.LastRenderedCards.AddRange(products.Take(3));
        if (a.BudgetTarget.HasValue && products.Any(p => p.Price < a.BudgetTarget.Value * 0.50m))
        {
            c.CurrentProductContext = null;
            c.LastReferencedProduct = null;
            c.RecentSuggestedProducts.Clear();
        }
        else if (products.Count > 0) { c.CurrentProductContext = products[0]; c.LastReferencedProduct = products[0]; }
    }

    private void LogPipelineDebug(string requestId, string? sessionId, string rawMessage, ChatTurnAnalysis a, AiConversationContext c, IReadOnlyList<AiProductContext> products, bool usedDb, bool usedGemini, string queryType, AiChatIntent? lastBefore, string? error)
        => _logger.LogInformation("[KKSHOP_AI_PIPELINE] requestId={RequestId}; sessionId={SessionId}; rawMessage={RawMessage}; normalizedMessage={NormalizedMessage}; detectedIntent={Intent}; extractedBudgetTarget={BudgetTarget}; extractedPurpose={Purpose}; extractedGame={Game}; extractedProductType={ProductType}; priceMode={PriceMode}; usedDatabaseFallback={UsedDb}; usedGemini={UsedGemini}; generatedQueryType={QueryType}; categoryFilter={CategoryFilter}; productTypeFilter={ProductTypeFilter}; resultCount={ResultCount}; returnedProductNames={Names}; returnedProductPrices={Prices}; lastIntentBefore={Before}; lastIntentAfter={After}; lastRecommendedProductCount={RecommendedCount}; renderCardType={RenderType}; errorMessage={Error}; recognizedProduct={RecognizedProduct}; routeReason={RouteReason}", requestId, sessionId ?? "none", TrimForLog(rawMessage), a.NormalizedMessage, IntentName(a.Intent), a.BudgetTarget, a.Purpose ?? "none", a.Game ?? "none", a.ProductType ?? "all", a.PriceMode, usedDb, usedGemini, queryType, a.Intent == AiChatIntent.PcBuildAdvice ? "PC" : "product", a.ProductType ?? "all", products.Count, string.Join(" | ", products.Select(p => p.Name)), string.Join(" | ", products.Select(p => p.Price.ToString("N0"))), lastBefore?.ToString() ?? "none", c.LastIntent?.ToString() ?? "none", c.RecentSuggestedProducts.Count, products.FirstOrDefault()?.ProductTypeLabel ?? "none", error ?? "none", c.LastReferencedProduct is null ? "none" : $"{c.LastReferencedProduct.Id}:{c.LastReferencedProduct.Name}", BuildRouteReason(a.Intent, c));

    private static bool IsConfigurationConsultation(string message)
    {
        var normalized = RemoveDiacritics((message ?? string.Empty).ToLowerInvariant());
        return ContainsAny(normalized, "build pc", "cau hinh", "gaming", "valorant", "cs2", " lol", "pubg", "gta v", "gta5", "stream", "render", "do hoa", "ngan sach")
            || Regex.IsMatch(normalized, @"\b\d+(?:[\.,]\d+)?\s*(?:trieu|tr|m)\b", RegexOptions.IgnoreCase);
    }

    private static string BuildConfigurationConsultationReply(string message, IReadOnlyList<AiProductContext> products)
    {
        var normalized = RemoveDiacritics((message ?? string.Empty).ToLowerInvariant());
        var budget = TryExtractChatBudgetText(normalized);
        var game = ContainsAny(normalized, "valorant") ? "Valorant"
            : ContainsAny(normalized, "cs2") ? "CS2"
            : ContainsAny(normalized, " lol") || normalized == "lol" ? "LOL"
            : ContainsAny(normalized, "pubg") ? "PUBG"
            : ContainsAny(normalized, "gta") ? "GTA V"
            : "Chưa nêu rõ";
        var purpose = ContainsAny(normalized, "render", "do hoa") ? "Đồ họa / render"
            : ContainsAny(normalized, "stream") ? "Stream / gaming"
            : ContainsAny(normalized, "gaming", "choi", "valorant", "cs2", "lol", "pubg", "gta") ? "Gaming"
            : "Tư vấn cấu hình";
        if (products.Count == 0)
        {
            return $"✓ Đã nhận nhu cầu\n\nNgân sách: {(string.IsNullOrWhiteSpace(budget) ? "Chưa nêu rõ" : budget)}\nMục đích: {purpose}\nGame: {game}\n\nHiện tại KKSHOP chưa có sản phẩm phù hợp với yêu cầu này.";
        }

        var pcCount = products.Count(p => string.Equals(p.CategoryScope, "PC", StringComparison.OrdinalIgnoreCase));
        var buildCount = products.Count(p => p.CategoryScope.StartsWith("BUILD_", StringComparison.OrdinalIgnoreCase));
        var source = pcCount > 0 ? "Ưu tiên PC Gaming có sẵn" : buildCount >= 7 ? "Tạo cấu hình từ linh kiện đang có" : "Sản phẩm phù hợp đang có";
        return $"✓ Đã nhận nhu cầu\n\nNgân sách: {(string.IsNullOrWhiteSpace(budget) ? "Chưa nêu rõ" : budget)}\nMục đích: {purpose}\nGame: {game}\n\nĐang tìm cấu hình phù hợp...\n\n{source}. Mình chỉ hiển thị sản phẩm có trong dữ liệu KKSHOP.";
    }

    private static bool ShouldAttachProductCards(AiChatIntent intent, string message)
        => IsExplicitProductListingRequest(message) && intent is (AiChatIntent.ProductSearch or AiChatIntent.ProductAdvice or AiChatIntent.PcBuildAdvice or AiChatIntent.ComponentAdvice or AiChatIntent.ProductExtremeQuery);

    private static bool IsSearchMode(AiChatIntent intent, string message, AiConversationContext conversation)
        => IsExplicitProductListingRequest(message) && intent is (AiChatIntent.ProductSearch or AiChatIntent.ProductAdvice or AiChatIntent.PcBuildAdvice or AiChatIntent.ComponentAdvice or AiChatIntent.ProductExtremeQuery);

    private static bool IsExplicitProductListingRequest(string message)
    {
        var n = RemoveDiacritics((message ?? string.Empty).ToLowerInvariant());
        if (ContainsAny(n, "xem them", "them san pham", "goi y them")) return true;
        if (IsProductQuestion(n) || ContainsAny(n, "so sanh", "danh gia", "uu diem", "nhuoc diem", "co nen mua", "ton dien", "nong khong", "choi duoc khong")) return false;
        return ContainsAny(n, "tu van cau hinh", "goi y pc", "tim pc", "mua pc", "pc duoi", "pc gaming", "build pc", "cau hinh", "ngan sach", "duoi", "toi da", "tam", "khoang", "tu ")
            || Regex.IsMatch(n, @"\b\d+(?:[\.,]\d+)?\s*(?:-|den|toi)\s*\d+(?:[\.,]\d+)?\s*(?:trieu|tr|m)\b", RegexOptions.IgnoreCase)
            || Regex.IsMatch(n, @"\b\d+(?:[\.,]\d+)?\s*(?:trieu|tr|m)\b", RegexOptions.IgnoreCase);
    }

    private static IReadOnlyList<AiProductContext> LimitProductCards(IReadOnlyList<AiProductContext> products) => products.Take(3).ToList();

    private static void SaveRecentSuggestedProducts(AiConversationContext conversation, IReadOnlyList<AiProductContext> products)
    {
        conversation.RecentSuggestedProducts.Clear();
        conversation.RecentSuggestedProducts.AddRange(products.Take(3));
        conversation.CurrentProductContext = conversation.RecentSuggestedProducts.FirstOrDefault();
        conversation.LastReferencedProduct = conversation.CurrentProductContext;
    }

    private static string BuildFallbackReply(string message, AiChatIntent intent, AiConversationContext conversation)
    {
        var product = ResolveContextProduct(message, conversation) ?? conversation.RecentSuggestedProducts.FirstOrDefault();
        if (product == null) return string.Empty;
        var n = RemoveDiacritics(message.ToLowerInvariant());
        if (ContainsAny(n, "gia", "bao nhieu")) return $"Mẫu {product.Name} hiện có giá {product.Price:N0} đ. {product.StockStatus}, bảo hành {product.Warranty}.";
        if (ContainsAny(n, "bao hanh", "bao lau")) return $"Mẫu {product.Name} đang được KKSHOP ghi nhận bảo hành {product.Warranty}. Khi mua bạn nên giữ thông tin đơn hàng để shop hỗ trợ nhanh hơn nhé.";
        if (ContainsAny(n, "con hang", "ton kho", "co hang")) return $"Mẫu {product.Name}: {product.StockStatus}. Nếu bạn muốn chốt mẫu này, mình khuyên đặt sớm vì tồn kho có thể thay đổi theo thời gian.";
        if (ContainsAny(n, "thong so", "cau hinh", "spec", "rgb", "led")) return $"Thông tin chính của {product.Name}: {product.Specifications}. Giá {product.Price:N0} đ, {product.StockStatus}, bảo hành {product.Warranty}.";
        return BuildRuleBasedProductAnalysis(message, product, conversation.RecentSuggestedProducts);
    }

    private static string BuildGenericSalesFallback(string message, ChatTurnAnalysis analysis, AiConversationContext conversation)
    {
        var topic = ProductTypeDisplay(analysis.ProductType ?? conversation.LastProductType);
        if (analysis.Intent == AiChatIntent.ComponentAdvice)
            return $"Mình đã hiểu bạn đang cần {topic.ToLowerInvariant()}. Nếu ưu tiên giá tốt, KKSHOP sẽ lọc mẫu còn hàng theo giá tăng dần; nếu ưu tiên gaming thì nên chọn mẫu bền, phản hồi tốt và đúng nhu cầu sử dụng. Bạn có thể cho mình thêm ngân sách để mình gợi ý sát hơn nhé.";
        if (analysis.Intent == AiChatIntent.PcBuildAdvice || analysis.BudgetTarget.HasValue || conversation.LastBudgetTarget.HasValue)
        {
            var budget = analysis.BudgetTarget ?? conversation.LastBudgetTarget;
            if (IsPowerQuestion(message) && budget.HasValue)
                return $"Bạn đang hỏi máy khoảng {budget.Value:N0} đ, hiện mình chưa có PC nguyên bộ phù hợp trong dữ liệu để đánh giá điện năng chính xác. Với cấu hình {budget.Value:N0} đ gaming, điện năng sẽ phụ thuộc chủ yếu vào VGA, thường cần PSU khoảng {(budget.Value >= 30_000_000m ? "650W-750W" : "550W-650W")}.";
            return $"Mình đã nhận nhu cầu PC{(budget.HasValue ? $" khoảng {budget.Value:N0} đ" : string.Empty)}. Nguyên tắc tư vấn của KKSHOP là ưu tiên cấu hình còn hàng, cân bằng CPU/GPU/RAM/SSD, sau đó mới xét RGB hoặc nâng cấp. Nếu không có đúng mức giá, mình sẽ ưu tiên mẫu gần nhất thay vì kết thúc cuộc tư vấn.";
        }
        return "Mình vẫn hỗ trợ bạn theo hướng tư vấn bán hàng của KKSHOP nhé. Bạn có thể gửi loại sản phẩm, ngân sách hoặc nhu cầu như gaming, văn phòng, học AI để mình lọc theo dữ liệu shop trước rồi mới diễn giải thêm.";
    }

    private static string BuildRuleBasedProductAnalysis(string message, AiProductContext product, IReadOnlyList<AiProductContext> recentProducts)
    {
        var intro = recentProducts.Count > 1 && recentProducts[0].Id == product.Id
            ? $"Mình hiểu bạn đang hỏi mẫu đầu tiên: {product.Name}."
            : $"Với mẫu {product.Name}, lợi ích chính là:";
        var source = $"{product.Name} {product.Description} {product.Specifications}";
        var normalized = RemoveDiacritics(source.ToLowerInvariant());
        var bullets = new List<string>();
        var cpu = Regex.Match(source, @"\b(?:i[3579]-?\d{4,5}[A-Z]*|i[3579]\s*\d{4,5}[A-Z]*|Ryzen\s*[3579]\s*\d{4,5}[A-Z]*)\b", RegexOptions.IgnoreCase).Value;
        var gpu = Regex.Match(source, @"\b(?:RTX|GTX|RX)\s*\d{3,4}\s*(?:\d+GB)?\b", RegexOptions.IgnoreCase).Value;
        var ram = Regex.Match(source, @"\b(?:8|16|32|64)\s*GB\s*(?:DDR\d)?\b", RegexOptions.IgnoreCase).Value;
        var askPower = IsPowerQuestion(message);
        var askThermal = IsThermalQuestion(message);
        var askRecommendation = RemoveDiacritics(message.ToLowerInvariant()).Contains("co nen mua") || RemoveDiacritics(message.ToLowerInvariant()).Contains("danh gia");
        if (askPower)
        {
            bullets.Add("Không quá tốn điện bạn nhé; mức thực tế còn tùy game, setting và hiệu suất bộ nguồn.");
            if (!string.IsNullOrWhiteSpace(cpu)) bullets.Add($"CPU {cpu}: thường là phần tiêu thụ đáng kể nhưng không phải lúc nào cũng chạy tối đa.");
            if (!string.IsNullOrWhiteSpace(gpu)) bullets.Add($"GPU {gpu}: khi chơi game sẽ là linh kiện ăn điện chính của bộ máy.");
            bullets.Add("Toàn bộ hệ thống gaming phổ thông thường tăng tiền điện không quá lớn nếu dùng khoảng 4-5 giờ/ngày.");
            bullets.Add("Đánh giá điện năng: ⭐ 8/10 nếu cấu hình không thuộc nhóm VGA cao cấp.");
        }
        else if (askThermal)
        {
            bullets.Add("Máy có thể ấm lên khi chơi game nặng, nhưng không đáng lo nếu case thoáng, quạt/tản hoạt động tốt và vệ sinh định kỳ.");
            if (!string.IsNullOrWhiteSpace(cpu)) bullets.Add($"CPU {cpu}: nên kiểm tra tản nhiệt đi kèm và airflow trong case.");
            if (!string.IsNullOrWhiteSpace(gpu)) bullets.Add($"GPU {gpu}: nhiệt độ phụ thuộc mẫu card, số quạt và thiết lập đồ họa.");
            bullets.Add("Nếu bạn chơi lâu nhiều giờ, nên đặt máy nơi thoáng và theo dõi nhiệt bằng phần mềm như HWInfo/MSI Afterburner.");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(cpu)) bullets.Add($"CPU {cpu} mạnh cho đa nhiệm, học tập/làm việc, mở nhiều tab và các tác vụ phổ thông đến bán chuyên.");
            if (!string.IsNullOrWhiteSpace(gpu)) bullets.Add($"GPU {gpu} phù hợp game eSports như Valorant, LOL, CS2 và GTA V ở Full HD ở mức tham khảo, tùy setting.");
            if (!string.IsNullOrWhiteSpace(ram)) bullets.Add($"RAM {ram} đủ dùng cho gaming phổ thông, học tập và làm việc hằng ngày.");
        }
        if (normalized.Contains("ssd")) bullets.Add("SSD giúp máy khởi động nhanh, mở game/ứng dụng mượt hơn HDD truyền thống.");
        if (normalized.Contains("pc") || normalized.Contains("may bo") || normalized.Contains("gaming")) bullets.Add("PC bộ sẵn giúp tiết kiệm thời gian chọn linh kiện, hạn chế rủi ro lệch cấu hình khi tự build.");
        bullets.Add($"Tình trạng {product.StockStatus.ToLowerInvariant()} và bảo hành {product.Warranty} giúp bạn yên tâm hơn khi mua tại KKSHOP.");
        if (normalized.Contains("rtx 3050")) bullets.Add("Lưu ý: RTX 3050 không phải lựa chọn tối ưu nếu bạn muốn chơi game AAA nặng ở 2K/4K hoặc bật setting rất cao.");
        if (askRecommendation) bullets.Add("Đánh giá tổng quan: 8/10 nếu giá bán hợp lý so với cấu hình và nhu cầu chính là 1080p gaming/làm việc hằng ngày.");
        return intro + "\n" + string.Join("\n", bullets.Select(x => "- " + x));
    }

    private static bool IsCompleteReply(string reply)
    {
        reply = (reply ?? string.Empty).Trim();
        if (reply.Length == 0) return false;
        if (reply.Length < 40) return true;
        if (reply.EndsWith("...", StringComparison.Ordinal)) return false;
        var last = reply[^1];
        return ".!?。…:)）]".Contains(last);
    }

    private static string CompleteSentence(string reply)
    {
        if (reply.Length <= 5000) return reply;
        var limit = 5000;
        var cut = reply.LastIndexOfAny(['.', '!', '?', '。', '…', '\n'], limit - 1);
        return cut > 800 ? reply[..(cut + 1)].Trim() : reply[..limit].Trim();
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

    private void LogDebugState(
        string requestId,
        string? sessionId,
        string message,
        AiChatIntent intent,
        AiConversationContext conversation,
        IReadOnlyList<AiProductContext> products,
        bool shouldAttachProductCards,
        string providerStatus,
        int finalResponseLength,
        int productCardsCount,
        string? error)
    {
        var normalized = RemoveDiacritics(message.ToLowerInvariant());
        var budget = TryExtractChatBudgetText(normalized);
        var category = ProductTerms.FirstOrDefault(normalized.Contains) ?? "none";
        _logger.LogInformation("[KKSHOP_AI_DEBUG] requestId={RequestId}; sessionId={SessionId}; userMessage={UserMessage}; detectedIntent={Intent}; currentProductContextId/name={CurrentProduct}; recentSuggestedProductIds={RecentProducts}; shouldAttachProductCards={ShouldAttachProductCards}; aiProviderStatus={ProviderStatus}; finalResponseLength={FinalResponseLength}; productCardsCount={ProductCardsCount}; error={Error}; budget={Budget}; category={Category}; matchedProducts={MatchedProducts}; recognizedProduct={RecognizedProduct}; routeReason={RouteReason}",
            requestId,
            sessionId ?? "none",
            TrimForLog(message),
            intent,
            conversation.CurrentProductContext is null ? "none" : $"{conversation.CurrentProductContext.Id}:{conversation.CurrentProductContext.Name}",
            conversation.RecentSuggestedProducts.Count == 0 ? "none" : string.Join(",", conversation.RecentSuggestedProducts.Select(p => p.Id)),
            shouldAttachProductCards,
            providerStatus,
            finalResponseLength,
            productCardsCount,
            string.IsNullOrWhiteSpace(error) ? "none" : TrimForLog(error),
            string.IsNullOrWhiteSpace(budget) ? "none" : budget,
            category,
            products.Count == 0 ? "none" : string.Join(" | ", products.Take(5).Select(p => $"{p.Id}:{p.Name}:{p.Price:N0}")),
            conversation.LastReferencedProduct is null ? "none" : $"{conversation.LastReferencedProduct.Id}:{conversation.LastReferencedProduct.Name}",
            BuildRouteReason(intent, conversation));
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
        if (IsFriendlySmallTalk(normalized)) return AiChatIntent.FriendlySmallTalk;
        if (ContainsAny(normalized, "thoi tiet", "bong da", "chung khoan", "nau an", "xem phim", "du lich", "lich su", "spam", "hack", "ma tuy", "bao luc")) return AiChatIntent.OutOfScope;
        if (ContainsAny(normalized, "bao hanh", "don hang", "thanh toan", "tra gop", "van chuyen", "ship", "nhan vien", "shop con hoat dong", "mo cua")) return AiChatIntent.PolicyOrSupport;

        var hasProductTerm = ProductTerms.Any(normalized.Contains) || Regex.IsMatch(normalized, @"\b(?:rtx|gtx|rx|i[3579]|ryzen|logitech|razer|akko|asus|msi|gigabyte|corsair|g304)\b", RegexOptions.IgnoreCase);
        var hasBudget = Regex.IsMatch(normalized, @"\b\d+(?:[\.,]\d+)?\s*(?:-|den|toi)?\s*\d*\s*(?:tr|trieu|m|million)\b", RegexOptions.IgnoreCase);
        var hasGame = ContainsAny(normalized, "gta", "gta5", "valorant", "cs2", "lol", "pubg", "black myth", "choi game", "gaming");
        var hasConfigIntent = ContainsAny(normalized, "cau hinh", "pc gaming", "may choi game", "may tinh", "build", "stream", "render", "do hoa", "ngan sach", "shop co san");
        var hasSearchIntent = SearchIntentWords.Any(normalized.Contains);
        var hasAdviceIntent = AdviceIntentWords.Any(normalized.Contains);
        if (ContainsAny(normalized, "so sanh", "compare", "khac nhau", "hon kem")) return AiChatIntent.ProductCompare;
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

    private static string BuildRouteReason(AiChatIntent intent, AiConversationContext conversation) => intent switch
    {
        AiChatIntent.ProductQuestion => conversation.LastReferencedProduct != null ? "PRODUCT_QUESTION_WITH_RECENT_PRODUCT_CONTEXT" : "PRODUCT_QUESTION_WITH_PRODUCT_SIGNAL",
        AiChatIntent.ProductCompare => "PRODUCT_COMPARISON_PRIORITY",
        AiChatIntent.ProductAdvice or AiChatIntent.ProductAnalysis or AiChatIntent.ProductBenefits or AiChatIntent.ProductProsCons or AiChatIntent.ProductRecommendation => "PRODUCT_DETAIL_OR_ADVICE_PRIORITY",
        AiChatIntent.PcBuildAdvice => "BUILD_PC_ONLY_AFTER_PRODUCT_ROUTES",
        AiChatIntent.ComponentAdvice => "COMPONENT_ADVICE",
        _ => intent.ToString().ToUpperInvariant()
    };

    private static bool HasConcreteProductSignal(string message)
    {
        var normalized = RemoveDiacritics(message.ToLowerInvariant());
        return ProductUrlRegex().IsMatch(message)
            || Regex.IsMatch(normalized, @"\b(?:rtx|gtx|rx|i[3579]|ryzen|logitech|razer|akko|asus|msi|gigabyte|corsair|g304|g502)\b", RegexOptions.IgnoreCase)
            || ProductTerms.Any(term => normalized.Contains(term) && term != "san pham");
    }

    private static bool IsFriendlySmallTalk(string normalized)
    {
        var compact = normalized.Trim(' ', '.', '!', '?');
        return ContainsAny(compact,
            "ban an com chua", "shop an com chua", "ai an com chua", "an com chua",
            "shop khoe khong", "ban khoe khong", "ai khoe khong", "khoe khong",
            "ai co met khong", "ban co met khong", "shop co met khong", "co met khong",
            "cam on", "thanks", "thank you")
            && compact.Length <= 80;
    }

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
            : intent == AiChatIntent.ProductCompare
                ? "- Nếu khách yêu cầu so sánh, lập bảng hoặc bullet so sánh theo giá, CPU/GPU/RAM/thông số đọc được, ưu/nhược điểm và nên chọn mẫu nào theo nhu cầu; nếu chỉ có 1 sản phẩm thì hỏi khách gửi thêm sản phẩm còn lại."
                : "- Nếu có sản phẩm, chọn tối đa 2-3 sản phẩm phù hợp nhất, nêu giá và lý do phù hợp.";
        var history = conversation.RecentMessages.Count == 0 ? "Không có." : string.Join("\n", conversation.RecentMessages.TakeLast(10));
        var currentProductLine = conversation.LastReferencedProduct == null ? "Chưa có" : $"{conversation.LastReferencedProduct.Name} (ID {conversation.LastReferencedProduct.Id})";
        return $"Ngữ cảnh 10 tin gần nhất:\n{history}\n\nSản phẩm đang được nhắc tới gần nhất: {currentProductLine}\n\nCâu hỏi khách hàng: {message}\n\nQuy tắc bắt buộc khi trả lời:\n- Không tự gợi ý hoặc liệt kê sản phẩm nếu câu hỏi thuộc tư vấn/đánh giá/điện năng/nhiệt độ/so sánh; các câu này chỉ trả lời, không đề xuất sản phẩm khác.\n- Chỉ dùng danh sách sản phẩm backend để nêu tên sản phẩm, giá, tồn kho, link, bảo hành và thông số cụ thể; không bịa sản phẩm ngoài danh sách.\n- Được phân tích tự nhiên dựa trên kiến thức phổ thông về PC khi đã có CPU/GPU/RAM/SSD/giá/tồn kho hoặc tên linh kiện trong dữ liệu; diễn đạt như nhân viên tư vấn, không trả lời quá cứng theo từng field.\n- Nếu có sản phẩm trong ngữ cảnh và khách dùng nó/máy này/con này/bộ này/sản phẩm này/em này thì hiểu là sản phẩm đang được nhắc tới gần nhất, không hỏi lại.\n- Các câu hỏi tự nhiên như lợi ích gì, có nên mua không, chơi Valorant/GTA V ổn không, ngân sách build PC, bàn phím giá rẻ, so sánh sản phẩm đều là câu hỏi hợp lệ cần tư vấn.\n- Khi phân tích sản phẩm, bắt buộc cố gắng suy luận từ tên, CPU, GPU, RAM, SSD, Mainboard, PSU, Case. Ví dụ Ryzen 7 5700X + RTX 3050 phù hợp eSports, GTA V, Valorant, CS2, học tập/làm việc và stream cơ bản ở mức tham khảo. Chỉ nói chưa đủ dữ liệu khi không đọc được cấu hình nào.\n{productRule}\n- Với FPS/game, không cam kết tuyệt đối; dùng các cụm như \"dự kiến\", \"phù hợp ở mức tham khảo\" vì FPS phụ thuộc setting và bản cập nhật game.\n- Nếu khách hỏi PC/cấu hình/gaming, chỉ dùng PC bộ/cấu hình có sẵn hoặc linh kiện build PC trong danh sách; tuyệt đối không biến chuột/bàn phím/tai nghe/màn hình thành PC đề xuất.\n- Với GTA V: 10-15 triệu chơi ổn Full HD thiết lập vừa/cao tùy linh kiện; 15-30 triệu rất ổn Full HD/2K tùy VGA; 50 triệu dư sức GTA V và có thể hướng tới game nặng hơn/2K/4K.\n- Không nói \"không tìm thấy\" nếu backend đã cung cấp bất kỳ sản phẩm hoặc linh kiện fallback nào.\n- Nếu khách hỏi chính sách, chỉ dùng nguồn sự thật chính sách bên dưới.\n\nNguồn sự thật chính sách KKSHOP:\n{policyKnowledge}\n\nDữ liệu sản phẩm KKSHOP được phép dùng:\n{productLines}";
    }

    private void LogAiTurnMetrics(string requestId, AiChatIntent intent, bool searchMode, string tokenUsage, TimeSpan elapsed)
        => _logger.LogInformation("[KKSHOP_AI_METRICS] requestId={RequestId}; mode={Mode}; intent={Intent}; tokenUsage={TokenUsage}; aiResponseTimeMs={ElapsedMs}", requestId, searchMode ? "SEARCH" : "CONSULT", intent, tokenUsage, (long)elapsed.TotalMilliseconds);

    private static string ExtractTokenUsage(JsonElement root)
    {
        if (!root.TryGetProperty("usageMetadata", out var usage)) return "unavailable";
        var prompt = usage.TryGetProperty("promptTokenCount", out var p) ? p.GetInt32() : 0;
        var candidates = usage.TryGetProperty("candidatesTokenCount", out var c) ? c.GetInt32() : 0;
        var total = usage.TryGetProperty("totalTokenCount", out var t) ? t.GetInt32() : prompt + candidates;
        return $"prompt={prompt}, candidates={candidates}, total={total}";
    }

    private static string ExtractReply(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0) return string.Empty;
        var first = candidates[0];
        if (!first.TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts)) return string.Empty;
        return string.Join("\n", parts.EnumerateArray().Select(p => p.TryGetProperty("text", out var text) ? text.GetString() : null).Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string TrimForLog(string text, int maxLength = 180) => text.Length <= maxLength ? text : text[..maxLength] + "...";

    [GeneratedRegex(@"/Products/Detail/(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ProductUrlRegex();

    [GeneratedRegex(@"(?:duoi|toi da|khoang|tam|tren|>=|>|<=|<)?\s*\d+(?:[\.,]\d+)?\s*(?:trieu|tr|m|million)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BudgetDebugRegex();

    private static string NormalizeCacheKey(string text) => text.Trim().ToLowerInvariant()[..Math.Min(text.Trim().Length, 160)];

    private static readonly string[] GreetingWords = ["chao", "chao ban", "hello", "hi", "alo", "shop oi", "ad oi"];
    private static readonly string[] ContextReferenceWords = ["no", "may nay", "con nay", "bo nay", "san pham nay", "em nay", "cai nay", "mau nay", "chiec nay", "con tren", "may tren", "bo tren", "cai tren"];
    private static readonly string[] SearchIntentWords = ["mua", "tim", "can", "gia", "bao nhieu", "con hang", "co hang", "goi y", "tu van cau hinh", "build", "ngan sach", "duoi", "tam", "khoang", "shop co san"];
    private static readonly string[] AdviceIntentWords = ["loi ich", "uu diem", "nhuoc diem", "phan tich", "co nen mua", "danh gia", "tot khong", "phu hop", "nen chon", "choi duoc", "choi duoc khong"];
    private static readonly string[] ProductTerms = ["pc", "may tinh", "may bo", "cau hinh", "linh kien", "chuot", "ban phim", "tai nghe", "man hinh", "cpu", "vga", "gpu", "ram", "ssd", "hdd", "nguon", "psu", "case", "tan nhiet", "main", "mainboard", "laptop", "gta", "gaming"];

    private const string SystemPrompt = "Bạn là KKSHOP AI, nhân viên tư vấn PC và linh kiện chuyên nghiệp của KKSHOP. Tone thân thiện, chuyên nghiệp như nhân viên shop: tư vấn tự nhiên, không cứng nhắc, không từ chối vô lý. Các câu hỏi về lợi ích sản phẩm, có nên mua, chơi Valorant/GTA V, build PC theo ngân sách, bàn phím giá rẻ, so sánh sản phẩm đều phải được hỗ trợ. Chỉ liệt kê/gửi sản phẩm khi khách có intent mua/tìm/tư vấn/giá/cấu hình/ngân sách/tên sản phẩm rõ ràng. Dùng dữ liệu backend cho sản phẩm, giá, tồn kho, link, bảo hành, chính sách; không bịa các dữ liệu này. Khi có CPU/GPU/RAM/giá/tồn kho hoặc tên linh kiện, được phân tích bằng kiến thức phổ thông PC ở mức tham khảo. Với hỏi ngoài lề nhẹ thì trả lời vui vẻ ngắn rồi kéo về hỗ trợ shop; chỉ giới hạn khi hoàn toàn không liên quan, nhạy cảm, spam hoặc không phù hợp. Khi tư vấn gaming/FPS chỉ nói dự kiến/phù hợp ở mức tham khảo, không cam kết FPS tuyệt đối. Trả lời tiếng Việt ngắn gọn, dễ hiểu.";
}
