namespace Datn.PcStore.Services;

public class AiChatOptions
{
    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "Gemini";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-2.5-flash";
    public int TimeoutSeconds { get; set; } = 15;
    public int MaxProductsContext { get; set; } = 8;
}
