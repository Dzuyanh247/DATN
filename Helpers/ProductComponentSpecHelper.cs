using System.Text.Json;
using System.Text.RegularExpressions;
using Datn.PcStore.ViewModels;

namespace Datn.PcStore.Helpers;

public static class ProductComponentSpecHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex WhitespaceSpecRegex = new(
        @"^\s*(\d+)\s+(.+?)\s+(\d+)\s+([0-9]+th|[0-9]+\s*tháng|[0-9]+)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Serialize(List<ProductComponentSpecViewModel>? specs)
    {
        var normalized = Normalize(specs);
        return normalized.Any() ? JsonSerializer.Serialize(normalized, JsonOptions) : string.Empty;
    }

    public static List<ProductComponentSpecViewModel> ParseStored(string? storedText)
    {
        if (string.IsNullOrWhiteSpace(storedText)) return new List<ProductComponentSpecViewModel>();

        var jsonSpecs = TryDeserialize(storedText);
        return jsonSpecs.Count > 0 ? jsonSpecs : ParseFallbackText(storedText);
    }

    public static List<ProductComponentSpecViewModel> TryDeserialize(string? storedText)
    {
        if (string.IsNullOrWhiteSpace(storedText)) return new List<ProductComponentSpecViewModel>();

        var text = storedText.Trim();
        if (!text.StartsWith('[')) return new List<ProductComponentSpecViewModel>();

        try
        {
            return Normalize(JsonSerializer.Deserialize<List<ProductComponentSpecViewModel>>(text, JsonOptions));
        }
        catch (JsonException)
        {
            return new List<ProductComponentSpecViewModel>();
        }
    }

    public static List<ProductComponentSpecViewModel> ParseFallbackText(string? rawText)
    {
        var specs = new List<ProductComponentSpecViewModel>();
        if (string.IsNullOrWhiteSpace(rawText)) return specs;

        var lines = Regex.Split(rawText, @"\r?\n")
            .Select(CleanLine)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !IsMeaninglessValue(line) && !IsTableHeaderLine(line))
            .ToList();

        if (LooksLikeLaptopSpecTable(lines))
        {
            var laptopSpecs = ParseLaptopSpecTable(lines);
            if (laptopSpecs.Count > 0) return Normalize(MergeSimilarSpecs(laptopSpecs));
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var columns = SplitComponentColumns(line);
            if (columns.Length >= 4 && IsHeaderLine(string.Join(' ', columns))) continue;

            ProductComponentSpecViewModel? spec = null;
            if (columns.Length >= 4)
            {
                spec = new ProductComponentSpecViewModel
                {
                    Stt = ParsePositiveInt(columns[0], specs.Count + 1),
                    Description = columns[1],
                    Quantity = ParsePositiveInt(columns[2], 1),
                    Warranty = columns[3]
                };
            }
            else
            {
                var match = WhitespaceSpecRegex.Match(line);
                if (match.Success)
                {
                    spec = new ProductComponentSpecViewModel
                    {
                        Stt = ParsePositiveInt(match.Groups[1].Value, specs.Count + 1),
                        Description = CleanLine(match.Groups[2].Value),
                        Quantity = ParsePositiveInt(match.Groups[3].Value, 1),
                        Warranty = CleanLine(match.Groups[4].Value)
                    };
                }
                else
                {
                    var classified = ClassifySpec(line);
                    if (classified == null && i + 1 < lines.Count && IsAttributeLabel(line))
                    {
                        var nextValue = lines[i + 1];
                        if (!IsGroupHeader(nextValue) && !IsAttributeLabel(nextValue))
                        {
                            classified = BuildSpec(MapAttributeToName(line), nextValue);
                            i++;
                        }
                    }
                    else if (classified == null && i + 2 < lines.Count && IsGroupHeader(line) && IsAttributeLabel(lines[i + 1]))
                    {
                        var nextValue = lines[i + 2];
                        classified = BuildSpec(MapGroupToName(line), nextValue);
                        i += 2;
                    }

                    spec = classified ?? BuildSpec("Thông số khác", Regex.Replace(line, @"^\s*\d+\s+", string.Empty).Trim());
                }
            }

            if (spec != null && !string.IsNullOrWhiteSpace(spec.Description)) specs.Add(spec);
        }

        return Normalize(MergeSimilarSpecs(specs));
    }

    public static bool IsComponentSpecJson(string? storedText) => TryDeserialize(storedText).Count > 0;

    private static List<ProductComponentSpecViewModel> Normalize(List<ProductComponentSpecViewModel>? specs)
    {
        if (specs == null) return new List<ProductComponentSpecViewModel>();

        var normalized = new List<ProductComponentSpecViewModel>();
        foreach (var spec in specs)
        {
            var description = CleanLine(spec.Description);
            if (string.IsNullOrWhiteSpace(description) || IsHeaderLine(description)) continue;

            normalized.Add(new ProductComponentSpecViewModel
            {
                Stt = spec.Stt.GetValueOrDefault() > 0 ? spec.Stt : normalized.Count + 1,
                Description = description,
                Quantity = spec.Quantity.GetValueOrDefault() > 0 ? spec.Quantity : 1,
                Warranty = CleanLine(spec.Warranty)
            });
        }

        return normalized;
    }

    private static bool IsHeaderLine(string line)
        => IsTableHeaderLine(line) || IsGroupHeader(line);

    private static bool IsTableHeaderLine(string line)
    {
        var normalized = Regex.Replace(line, @"\s+", " ").Trim().ToLowerInvariant();
        return normalized == "stt mô tả thiết bị sl bh"
            || normalized == "stt mo ta thiet bi sl bh"
            || (normalized.StartsWith("stt ") && normalized.Contains("mô tả") && normalized.Contains(" sl") && normalized.EndsWith("bh"));
    }

    private static ProductComponentSpecViewModel? ClassifySpec(string line)
    {
        if (IsGroupHeader(line) || IsAttributeLabel(line) || IsMeaninglessValue(line)) return null;

        var name = line switch
        {
            _ when Regex.IsMatch(line, @"\b(ryzen|intel\s+core|core\s+ultra|i[3579][-\s]?\d{4,5}|cpu|processor|hx|hs)\b", RegexOptions.IgnoreCase) => "CPU",
            _ when Regex.IsMatch(line, @"\b(ram|ddr[345]|so-?dimm|\d+\s*gb\s+ddr)\b", RegexOptions.IgnoreCase) => "RAM",
            _ when Regex.IsMatch(line, @"\b(ssd|hdd|m\.?2|nvme|pcie|sata)\b", RegexOptions.IgnoreCase) => "SSD",
            _ when Regex.IsMatch(line, @"\b(vga|gpu|rtx|gtx|mx\d+|radeon|arc|gddr)\b", RegexOptions.IgnoreCase) => "GPU",
            _ when Regex.IsMatch(line, @"\b(\d{2}(?:\.\d)?\s*(?:inch|""|inches)|fhd|qhd|uhd|ips|oled|mini\s*led|\d{2,3}\s*hz|srgb|dci-p3|g-sync|freesync)\b", RegexOptions.IgnoreCase) => "Màn hình",
            _ when Regex.IsMatch(line, @"\b(wi-?fi|802\.11|wireless)\b", RegexOptions.IgnoreCase) => "WiFi",
            _ when Regex.IsMatch(line, @"\bbluetooth\b", RegexOptions.IgnoreCase) => "Bluetooth",
            _ when Regex.IsMatch(line, @"\b(\d+(?:[\.,]\d+)?\s*wh|\d+\s*mah|cell)\b", RegexOptions.IgnoreCase) => "Pin",
            _ when Regex.IsMatch(line, @"\b(\d+\s*w)\b", RegexOptions.IgnoreCase) && Regex.IsMatch(line, @"\b(adapter|sạc|sac|charger|power)\b", RegexOptions.IgnoreCase) => "Sạc",
            _ when Regex.IsMatch(line, @"\b(windows|linux|ubuntu|dos|macos|hệ điều hành|he dieu hanh|os)\b", RegexOptions.IgnoreCase) => "OS",
            _ when Regex.IsMatch(line, @"\b\d+(?:[\.,]\d+)?\s*kg\b", RegexOptions.IgnoreCase) => "Trọng lượng",
            _ when Regex.IsMatch(line, @"\b\d+(?:[\.,]\d+)?\s*x\s*\d+(?:[\.,]\d+)?\s*x\s*\d+(?:[\.,]\d+)?\s*(mm|cm)\b", RegexOptions.IgnoreCase) => "Kích thước",
            _ when Regex.IsMatch(line, @"\b(black|white|gray|grey|silver|blue|red|đen|trắng|xám|bạc|xanh|đỏ|màu)\b", RegexOptions.IgnoreCase) => "Màu sắc",
            _ when Regex.IsMatch(line, @"\b(fingerprint|tpm|privacy|bảo mật|bao mat|khuôn mặt|khuon mat)\b", RegexOptions.IgnoreCase) => "Bảo mật",
            _ when Regex.IsMatch(line, @"\b(audio|speaker|loa|dolby|nahimic|microphone|mic)\b", RegexOptions.IgnoreCase) => "Audio",
            _ when Regex.IsMatch(line, @"\b(camera|webcam|fhd webcam|hd webcam|ir camera)\b", RegexOptions.IgnoreCase) => "Camera",
            _ => null
        };

        return name == null ? null : BuildSpec(name, CleanLaptopValue(name, line));
    }

    private static List<ProductComponentSpecViewModel> MergeSimilarSpecs(List<ProductComponentSpecViewModel> specs)
    {
        var merged = new List<ProductComponentSpecViewModel>();
        foreach (var spec in specs)
        {
            var description = CleanLine(spec.Description);
            var separator = description.IndexOf(" | ", StringComparison.Ordinal);
            var name = separator > 0 ? description[..separator] : "Thông số khác";
            var value = separator > 0 ? description[(separator + 3)..] : description;
            var existing = merged.FirstOrDefault(x => CleanLine(x.Description).StartsWith(name + " | ", StringComparison.OrdinalIgnoreCase));
            if (existing == null || name == "Thông số khác")
            {
                merged.Add(spec);
                continue;
            }

            if (!existing.Description!.Contains(value, StringComparison.OrdinalIgnoreCase))
                existing.Description = $"{existing.Description}; {value}";
        }

        for (var i = 0; i < merged.Count; i++) merged[i].Stt = i + 1;
        return merged;
    }

    private static ProductComponentSpecViewModel BuildSpec(string name, string value)
        => new() { Stt = 1, Description = $"{name} | {CleanLaptopValue(name, value)}", Quantity = 1, Warranty = string.Empty };

    private static string CleanLaptopValue(string name, string value)
    {
        var cleaned = CleanLine(value);
        return CleanLine(cleaned.Trim(',', '-', ':', ' '));
    }

    private static bool LooksLikeLaptopSpecTable(List<string> lines)
        => lines.Count(IsLaptopGroupHeader) >= 2;

    private static List<ProductComponentSpecViewModel> ParseLaptopSpecTable(List<string> lines)
    {
        var specs = new List<ProductComponentSpecViewModel>();
        string? currentGroup = null;
        string? pendingName = null;
        var pendingValues = new List<string>();

        void Flush()
        {
            if (string.IsNullOrWhiteSpace(pendingName)) return;
            var value = string.Join("; ", pendingValues.Select(CleanLine).Where(x => !string.IsNullOrWhiteSpace(x) && !IsMeaninglessValue(x)));
            if (!string.IsNullOrWhiteSpace(value)) specs.Add(BuildSpec(pendingName, value));
            pendingName = null;
            pendingValues.Clear();
        }

        foreach (var line in lines)
        {
            if (IsLaptopGroupHeader(line))
            {
                Flush();
                currentGroup = line;
                continue;
            }

            var mapped = TryMapLaptopLabel(line, currentGroup, out var inlineValue);
            if (!string.IsNullOrWhiteSpace(mapped))
            {
                Flush();
                pendingName = mapped;
                if (!string.IsNullOrWhiteSpace(inlineValue) && !IsMeaninglessValue(inlineValue)) pendingValues.Add(inlineValue);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(pendingName))
            {
                if (!IsMeaninglessValue(line)) pendingValues.Add(line);
                continue;
            }

            var classified = ClassifySpec(line);
            if (classified != null) specs.Add(classified);
        }

        Flush();
        return specs;
    }

    private static bool IsGroupHeader(string line) => IsLaptopGroupHeader(line);

    private static bool IsLaptopGroupHeader(string line)
        => Regex.IsMatch(line, @"^(bộ vi xử lý(?: \(cpu\))?|bo vi xu ly|cpu|bộ nhớ trong(?: \(ram laptop\))?|ram laptop|bo nho trong|ổ cứng(?: \(ssd laptop\))?|o cung|ssd laptop|ổ đĩa quang(?: \(odd\))?|hiển thị(?: \(màn hình\))?|hien thi|màn hình|man hinh|đồ họa(?: \(vga\))?|do hoa|vga|gpu|kết nối(?: \(network\))?|ket noi|network|keyboard(?: \(bàn phím\))?|bàn phím|ban phim|mouse(?: \(chuột\))?|chuột|chuot|giao tiếp mở rộng|pin laptop|sạc pin laptop|sac pin laptop|hệ điều hành(?: \(operating system\))?|he dieu hanh|thông tin khác|thong tin khac)$", RegexOptions.IgnoreCase);

    private static bool IsAttributeLabel(string line)
        => !string.IsNullOrWhiteSpace(TryMapLaptopLabel(line, null, out _));

    private static string? TryMapLaptopLabel(string line, string? group, out string inlineValue)
    {
        inlineValue = string.Empty;
        var cleaned = CleanLine(line);
        var label = cleaned;
        var match = Regex.Match(cleaned, @"^(.+?)(?:\s*[:|]\s+|\s{2,})(.+)$");
        if (match.Success)
        {
            label = CleanLine(match.Groups[1].Value);
            inlineValue = CleanLine(match.Groups[2].Value);
        }
        else
        {
            var inlinePrefixes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Wireless"] = "WiFi", ["Lan"] = "LAN", ["Bluetooth"] = "Bluetooth", ["Camera"] = "Camera",
                ["Audio"] = "Audio", ["Trọng lượng"] = "Trọng lượng", ["Kích thước"] = "Kích thước",
                ["Chất liệu"] = "Chất liệu", ["Màu sắc"] = "Màu sắc", ["Bảo mật"] = "Bảo mật",
                ["Dung lượng pin"] = "Pin", ["Sạc Pin Laptop"] = "Sạc", ["Đi kèm"] = "Sạc",
                ["Hệ điều hành đi kèm"] = "OS", ["Hệ điều hành tương thích"] = "OS tương thích",
                ["Phần mềm Office"] = "Office", ["Tính năng đặc biệt"] = "Tính năng đặc biệt"
            };
            foreach (var prefix in inlinePrefixes.Keys.OrderByDescending(x => x.Length))
            {
                if (!cleaned.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase)) continue;
                inlineValue = CleanLine(cleaned[prefix.Length..]);
                return inlinePrefixes[prefix];
            }
        }

        if (Regex.IsMatch(label, @"^tên bộ vi xử lý$|^ten bo vi xu ly$|^tốc độ$|^toc do$", RegexOptions.IgnoreCase)) return "CPU";
        if (Regex.IsMatch(label, @"^bộ nhớ đệm$|^bo nho dem$|^cache$", RegexOptions.IgnoreCase)) return "Cache";
        if (Regex.IsMatch(label, @"^dung lượng$|^dung luong$", RegexOptions.IgnoreCase))
        {
            if (Regex.IsMatch(group ?? string.Empty, @"ram|bộ nhớ|bo nho", RegexOptions.IgnoreCase)) return "RAM";
            if (Regex.IsMatch(group ?? string.Empty, @"ssd|ổ cứng|o cung", RegexOptions.IgnoreCase)) return "SSD";
            if (Regex.IsMatch(group ?? string.Empty, @"pin", RegexOptions.IgnoreCase)) return "Pin";
        }
        if (Regex.IsMatch(label, @"^số khe cắm$|^so khe cam$", RegexOptions.IgnoreCase)) return "Khe RAM";
        if (Regex.IsMatch(label, @"^khả năng nâng cấp$|^kha nang nang cap$", RegexOptions.IgnoreCase)) return "Khả năng nâng cấp";
        if (Regex.IsMatch(label, @"^màn hình$|^man hinh$", RegexOptions.IgnoreCase)) return "Màn hình";
        if (Regex.IsMatch(label, @"^độ phân giải$|^do phan giai$", RegexOptions.IgnoreCase)) return "Độ phân giải";
        if (Regex.IsMatch(label, @"^bộ xử lý$|^bo xu ly$", RegexOptions.IgnoreCase) && Regex.IsMatch(group ?? string.Empty, @"vga|đồ họa|do hoa|gpu", RegexOptions.IgnoreCase)) return "GPU";
        if (Regex.IsMatch(label, @"^công nghệ$|^cong nghe$", RegexOptions.IgnoreCase) && Regex.IsMatch(group ?? string.Empty, @"vga|đồ họa|do hoa|gpu", RegexOptions.IgnoreCase)) return "Công nghệ GPU";
        if (Regex.IsMatch(label, @"^wireless$", RegexOptions.IgnoreCase)) return "WiFi";
        if (Regex.IsMatch(label, @"^lan$", RegexOptions.IgnoreCase)) return "LAN";
        if (Regex.IsMatch(label, @"^bluetooth$", RegexOptions.IgnoreCase)) return "Bluetooth";
        if (Regex.IsMatch(label, @"^kiểu bàn phím$|^kieu ban phim$", RegexOptions.IgnoreCase)) return "Bàn phím";
        if (Regex.IsMatch(label, @"^cảm ứng đa điểm$|^cam ung da diem$", RegexOptions.IgnoreCase)) return "Chuột/Touchpad";
        if (Regex.IsMatch(label, @"^kết nối usb$|^ket noi usb$", RegexOptions.IgnoreCase)) return "Cổng USB";
        if (Regex.IsMatch(label, @"^kết nối hdmi/vga$|^ket noi hdmi/vga$", RegexOptions.IgnoreCase)) return "HDMI/VGA";
        if (Regex.IsMatch(label, @"^card reader$", RegexOptions.IgnoreCase)) return "Card reader";
        if (Regex.IsMatch(label, @"^tai nghe$", RegexOptions.IgnoreCase)) return "Jack âm thanh";
        if (Regex.IsMatch(label, @"^camera$", RegexOptions.IgnoreCase)) return "Camera";
        if (Regex.IsMatch(label, @"^sạc pin laptop$|^sac pin laptop$|^đi kèm$|^di kem$", RegexOptions.IgnoreCase)) return "Sạc";
        if (Regex.IsMatch(label, @"^hệ điều hành đi kèm$|^he dieu hanh di kem$", RegexOptions.IgnoreCase)) return "OS";
        if (Regex.IsMatch(label, @"^hệ điều hành tương thích$|^he dieu hanh tuong thich$", RegexOptions.IgnoreCase)) return "OS tương thích";
        if (Regex.IsMatch(label, @"^phần mềm office$|^phan mem office$", RegexOptions.IgnoreCase)) return "Office";
        if (Regex.IsMatch(label, @"^tính năng đặc biệt$|^tinh nang dac biet$", RegexOptions.IgnoreCase)) return "Tính năng đặc biệt";
        if (Regex.IsMatch(label, @"^audio$|^âm thanh$|^am thanh$", RegexOptions.IgnoreCase)) return "Audio";
        if (Regex.IsMatch(label, @"^trọng lượng$|^trong luong$", RegexOptions.IgnoreCase)) return "Trọng lượng";
        if (Regex.IsMatch(label, @"^kích thước$|^kich thuoc$", RegexOptions.IgnoreCase)) return "Kích thước";
        if (Regex.IsMatch(label, @"^chất liệu$|^chat lieu$", RegexOptions.IgnoreCase)) return "Chất liệu";
        if (Regex.IsMatch(label, @"^màu sắc$|^mau sac$", RegexOptions.IgnoreCase)) return "Màu sắc";
        if (Regex.IsMatch(label, @"^bảo mật$|^bao mat$", RegexOptions.IgnoreCase)) return "Bảo mật";
        return null;
    }

    private static string MapAttributeToName(string line) => TryMapLaptopLabel(line, null, out _) ?? MapGroupToName(line);

    private static string MapGroupToName(string line)
    {
        if (Regex.IsMatch(line, @"cpu|vi xử lý|vi xu ly", RegexOptions.IgnoreCase)) return "CPU";
        if (Regex.IsMatch(line, @"ram|bộ nhớ|bo nho|dung lượng|dung luong", RegexOptions.IgnoreCase)) return "RAM";
        if (Regex.IsMatch(line, @"ssd|hdd|ổ cứng|o cung", RegexOptions.IgnoreCase)) return "SSD";
        if (Regex.IsMatch(line, @"vga|gpu|đồ họa|do hoa|card", RegexOptions.IgnoreCase)) return "GPU";
        if (Regex.IsMatch(line, @"màn hình|man hinh|hiển thị|hien thi|phân giải|phan giai|tần số|tan so", RegexOptions.IgnoreCase)) return "Màn hình";
        return Regex.IsMatch(line, @"hệ điều hành|he dieu hanh", RegexOptions.IgnoreCase) ? "OS" : "Thông số khác";
    }

    private static bool IsMeaninglessValue(string line)
        => Regex.IsMatch(CleanLine(line), @"^(none|--|n/a|không có|khong co|trống|trong)$", RegexOptions.IgnoreCase);

    private static string[] SplitComponentColumns(string rawLine)
    {
        var separator = rawLine.Contains('\t') ? '\t' : rawLine.Contains('|') ? '|' : '\0';
        if (separator == '\0') return Array.Empty<string>();

        return rawLine.Split(separator)
            .Select(CleanLine)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
    }

    private static string CleanLine(string? value)
        => Regex.Replace(value ?? string.Empty, @"[ \t\r\n]+", " ").Trim();

    private static int ParsePositiveInt(string? value, int fallback)
        => int.TryParse((value ?? string.Empty).Trim(), out var result) && result > 0 ? result : fallback;
}
