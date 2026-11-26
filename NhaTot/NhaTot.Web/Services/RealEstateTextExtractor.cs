using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NhaTot.Web.Services;

public sealed record WardCandidate(string Name, string? Province);
public sealed record ExtractedProperty(decimal? ExactPrice, decimal? PriceMin, decimal? PriceMax, decimal? Area, string? Type, string? Province, string? Ward)
{
    public bool Complete => (ExactPrice.HasValue || (PriceMin.HasValue && PriceMax.HasValue)) && Area.HasValue && Type != null && Province != null && Ward != null;
}

/// <summary>Tiền xử lý văn bản BĐS: làm sạch icon/emoji, trích xuất địa chỉ, khoảng giá (GiaMinVND - GiaMaxVND), diện tích và loại BĐS.</summary>
public static class RealEstateTextExtractor
{
    private const string Number = @"\d+(?:[.,]\d+)*";

    private static readonly Regex PinEmojiRegex = new(@"(?:\uD83D\uDCCD|\uD83D\uDCCC)", RegexOptions.Compiled);
    private static readonly Regex IconStripRegex = new(@"[\p{Cs}\u200B-\u200D\uFE00-\uFE0F\u2190-\u21FF\u2300-\u27BF\u2B00-\u2BFF]+", RegexOptions.Compiled);
    private static readonly Regex HorizontalSpacesRegex = new(@"[ \t]{2,}", RegexOptions.Compiled);

    private static readonly Regex NonAddressCutoffRegex = new(
        @"(?i)(?:\b(?:giá\s*bán|giá\s*chào|giá\s*chỉ|giá\s*rẻ|giá\s*[:=\d]|tài\s*chính|diện\s*tích|dt\s*[:=\d]|rộng\s*\d|ngang\s*\d|dài\s*\d|mặt\s*tiền\s*\d|đường\s*(?:rộng\s*)?\d+\s*m\b|đường\s*trước\s*(?:nhà|đất)|hướng\s*:|pháp\s*lý|công\s*năng|thiết\s*kế|tiện\s*ích|phù\s*hợp|lh\b|liên\s*hệ|sđt|sdt|zalo|alo\b|hotline)\b|[–\-—.,;]\s*vị\s*trí\s*(?:kinh\s*doanh|đẹp|đắc\s*địa|vàng|trung\s*tâm)|\b\d+(?:[.,]\d+)?\s*(?:m2|m²|tỷ|tỉ|triệu|tr\b))",
        RegexOptions.Compiled);

    private static readonly Regex LabeledAddressRegex = new(
        @"(?im)(?:__PIN__|(?:địa\s*chỉ|vị\s*trí|khu\s*vực|tọa\s*lạc)\s*[:\-])\s*([^|\r\n]+)",
        RegexOptions.Compiled);

    private static readonly Regex GenericFluffAddressRegex = new(
        @"(?i)^(?:kinh\s*doanh|đẹp|đắc\s*địa|trung\s*tâm|siêu\s*đẹp|vàng|tiềm\s*năng|thuận\s*tiện)\b",
        RegexOptions.Compiled);

    private static readonly Regex InlineAddressRegex = new(
        @"(?i)\b(?:KĐT|KDC|Khu\s+đô\s+thị|Khu\s+dân\s+cư|Chung\s+cư(?:\s+cao\s+cấp)?|Trung\s+tâm\s+(?:TP|thành\s+phố)|(?:Mặt\s+)?Đường(?!\s+(?:\d+\s*m\b|rộng\b|trước\b|nhựa\b|bê\s*tông\b|ô\s*tô\b|oto\b|thông\b))|Quốc\s+lộ|QL\s*\d+|Đại\s+lộ|Phố|Ngõ|Ngách|Hẻm|Phường|Xã|Thị\s+trấn|Khối|Xóm|Thôn)\s+[^|\r\n.;!?]{2,90}",
        RegexOptions.Compiled);

    private static MatchCollection Matches(string text, string pattern) => Regex.Matches(text, pattern,
        RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    /// <summary>Xóa toàn bộ emoji/icon trang trí khỏi chuỗi hiển thị, giữ nguyên chữ tiếng Việt và xuống dòng.</summary>
    public static string CleanIcons(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var noIcons = IconStripRegex.Replace(raw, " ");
        var lines = noIcons
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => HorizontalSpacesRegex.Replace(line, " ").Trim(' ', '\t', '|', '•', '▪', '▫'))
            .Where(line => line.Length > 0);
        return string.Join("\n", lines).Trim();
    }

    private static string CleanAddressCandidate(string candidate)
    {
        var cut = NonAddressCutoffRegex.Match(candidate);
        if (cut.Success)
            candidate = candidate[..cut.Index];

        candidate = candidate.Replace("__PIN__", " ");
        candidate = IconStripRegex.Replace(candidate, " ");
        candidate = HorizontalSpacesRegex.Replace(candidate, " ")
            .Trim(' ', '\t', '-', '–', '—', '.', ',', ':', ';', '/', '|', '(', ')');

        if (candidate.Length > 120)
            candidate = candidate[..120].TrimEnd() + "…";

        return candidate;
    }

    /// <summary>Trích xuất địa chỉ sạch (đã loại bỏ icon, diện tích, độ rộng đường và phần giá/SĐT theo sau).</summary>
    public static string ExtractAddress(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return "—";

        var segmented = PinEmojiRegex.Replace(content, "\n__PIN__ ");
        segmented = IconStripRegex.Replace(segmented, " | ");

        foreach (Match m in LabeledAddressRegex.Matches(segmented))
        {
            var cleaned = CleanAddressCandidate(m.Groups[1].Value);
            if (cleaned.Length >= 4 && !GenericFluffAddressRegex.IsMatch(cleaned))
                return cleaned;
        }

        foreach (Match m in InlineAddressRegex.Matches(segmented))
        {
            var cleaned = CleanAddressCandidate(m.Value);
            if (cleaned.Length >= 5 && !GenericFluffAddressRegex.IsMatch(cleaned))
                return cleaned;
        }

        return "—";
    }

    /// <summary>Định dạng số tiền VNĐ sang chuỗi tiếng Việt dễ đọc, ví dụ 1,701,000,000 -> "1 tỷ 701".</summary>
    public static string FormatVndReadable(decimal vnd)
    {
        if (vnd >= 1_000_000_000m)
        {
            var billions = Math.Floor(vnd / 1_000_000_000m);
            var remMillions = Math.Round((vnd - billions * 1_000_000_000m) / 1_000_000m);
            if (remMillions >= 1000m) { billions += 1m; remMillions = 0m; }
            if (remMillions <= 0m) return $"{billions:0} tỷ";
            return $"{billions:0} tỷ {remMillions:000}";
        }
        if (vnd >= 1_000_000m)
        {
            var millions = vnd / 1_000_000m;
            return millions == Math.Floor(millions) ? $"{millions:0} triệu" : $"{millions:0.##} triệu";
        }
        return $"{vnd:N0} đ";
    }

    /// <summary>Hiển thị khoảng giá "1 tỷ 701 - 1 tỷ 799" (nếu min != max) hoặc một mức giá nếu cố định.</summary>
    public static string FormatPriceRange(decimal? minVnd, decimal? maxVnd)
    {
        if (!minVnd.HasValue && !maxVnd.HasValue) return "—";
        var lo = minVnd ?? maxVnd!.Value;
        var hi = maxVnd ?? minVnd!.Value;
        if (lo <= 0 && hi <= 0) return "—";
        if (lo == hi) return FormatVndReadable(lo);
        return $"{FormatVndReadable(lo)} - {FormatVndReadable(hi)}";
    }

    public static string Normalize(string text) => string.Concat(text.ToLowerInvariant().Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)).Replace('đ', 'd')
        .Normalize(NormalizationForm.FormC);

    private static decimal? ParseNumber(string value)
    {
        if (Regex.IsMatch(value, @"^\d{1,3}([.,]\d{3})+$", RegexOptions.None, TimeSpan.FromSeconds(1)))
            value = value.Replace(".", "").Replace(",", "");
        else if (value.Count(c => c is '.' or ',') <= 1) value = value.Replace(',', '.');
        else return null;
        return decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var n) && n > 0 ? n : null;
    }

    private static bool Contains(string text, string phrase) => Matches(text, @"(?<!\w)" + Regex.Escape(phrase) + @"(?!\w)").Count > 0;

    private static (decimal Min, decimal Max) BuildMaskedBillionRange(decimal billions, string knownSubDigits)
    {
        var baseVnd = billions * 1_000_000_000m;
        if (string.IsNullOrEmpty(knownSubDigits))
        {
            // Ví dụ: "4 tỷ xxx", "4 tỷ xx", "4.x tỷ" -> 4 tỷ 001 - 4 tỷ 999
            return (baseVnd + 1_000_000m, baseVnd + 999_000_000m);
        }
        if (knownSubDigits.Length == 1)
        {
            // Ví dụ: "1 tỷ 7xx", "1.7xx tỷ", "1 tỷ 7x", "1.7x tỷ" -> 1 tỷ 701 - 1 tỷ 799
            var sub = decimal.Parse(knownSubDigits, CultureInfo.InvariantCulture) * 100_000_000m;
            return (baseVnd + sub + 1_000_000m, baseVnd + sub + 99_000_000m);
        }
        // Ví dụ: "1 tỷ 75x", "1.75x tỷ" -> 1 tỷ 751 - 1 tỷ 759
        var sub2 = decimal.Parse(knownSubDigits[..2], CultureInfo.InvariantCulture) * 10_000_000m;
        return (baseVnd + sub2 + 1_000_000m, baseVnd + sub2 + 9_000_000m);
    }

    public static (decimal? ExactPrice, decimal? PriceMin, decimal? PriceMax) ExtractPriceInfo(string? rawContent)
    {
        var (lo, hi) = ExtractPriceRange(rawContent);
        if (!lo.HasValue || !hi.HasValue) return (null, null, null);
        return lo.Value == hi.Value ? (lo.Value, null, null) : (null, lo.Value, hi.Value);
    }

    public static (decimal? PriceMin, decimal? PriceMax) ExtractPriceRange(string? rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent)) return (null, null);
        var clean = CleanIcons(rawContent);
        var text = Normalize(clean).Replace('²', '2');
        return ExtractPriceRangeFromNormalized(text);
    }

    private static (decimal? PriceMin, decimal? PriceMax) ExtractPriceRangeFromNormalized(string normalizedText)
    {
        var isPerArea = Matches(normalizedText, @"/\s*(?:m2|met)\b|\b(?:moi|tren)\s+m2\b").Count > 0;
        if (isPerArea) return (null, null);

        var isPerPeriod = Matches(normalizedText, @"/\s*(?:thang|th|nam|ngay)\b|\b(?:moi|tren)\s+thang\b").Count > 0;
        var isRentalContext = Matches(normalizedText, @"\b(cho\s+thue|can\s+thue|tim\s+thue|thue\s+nha|thue\s+dat|thue\s+can\s+ho|thue\s+chung\s+cu|thue\s+mat\s+bang|phong\s+tro|nha\s+tro)\b").Count > 0;
        if (isPerPeriod && !isRentalContext) return (null, null);

        // 1. Dạng "1 tỷ 7xx", "4 tỷ xxx", "1 tỷ 75x", "4 tỷ xx triệu" (có chữ x sau tỷ)
        var maskedAfterTy = Matches(normalizedText, @"(?<![\w.,])(?<maj>\d{1,4})\s*(?:ty|ti)\s*(?<sub>\d{0,2})x+(?:\s*(?:trieu|tr))?(?!\w)");
        if (maskedAfterTy.Count >= 1)
        {
            var maj = decimal.Parse(maskedAfterTy[0].Groups["maj"].Value, CultureInfo.InvariantCulture);
            var sub = maskedAfterTy[0].Groups["sub"].Value;
            return BuildMaskedBillionRange(maj, sub);
        }

        // 2. Dạng "1.7xx tỷ", "1,7x tỷ", "4.x tỷ" (chữ x nằm trước chữ tỷ)
        var maskedBeforeTy = Matches(normalizedText, @"(?<![\w.,])(?<maj>\d{1,4})[.,](?<sub>\d{0,2})x+\s*(?:ty|ti)(?!\w)");
        if (maskedBeforeTy.Count >= 1)
        {
            var maj = decimal.Parse(maskedBeforeTy[0].Groups["maj"].Value, CultureInfo.InvariantCulture);
            var sub = maskedBeforeTy[0].Groups["sub"].Value;
            return BuildMaskedBillionRange(maj, sub);
        }

        // 3. Dạng "8xx triệu", "85x tr" (chữ x với đơn vị triệu)
        var maskedTrieu = Matches(normalizedText, @"(?<![\w.,])(?<known>\d{1,2})(?<xs>x{1,2})\s*(?:trieu|tr)(?!\w)");
        if (maskedTrieu.Count >= 1)
        {
            var known = maskedTrieu[0].Groups["known"].Value;
            if (known.Length == 1)
            {
                var baseVal = decimal.Parse(known, CultureInfo.InvariantCulture) * 100_000_000m;
                return (baseVal + 1_000_000m, baseVal + 99_000_000m);
            }
            var baseVal2 = decimal.Parse(known, CultureInfo.InvariantCulture) * 10_000_000m;
            return (baseVal2 + 1_000_000m, baseVal2 + 9_000_000m);
        }

        // 4. Dạng viết tắt sau nhãn: "giá bán: 4.x", "tài chính 1.7x" (ngầm hiểu đơn vị tỷ)
        var shortLabeledTy = Matches(normalizedText, @"\b(?:gia\s*ban|gia\s*chao|tai\s*chinh|gia)\s*[:\-]?\s*(?<maj>\d{1,2})[.,](?<sub>\d{0,2})x+\b");
        if (shortLabeledTy.Count >= 1)
        {
            var maj = decimal.Parse(shortLabeledTy[0].Groups["maj"].Value, CultureInfo.InvariantCulture);
            var sub = shortLabeledTy[0].Groups["sub"].Value;
            return BuildMaskedBillionRange(maj, sub);
        }

        // 5. Khoảng giá tường minh: "1.7 - 1.8 tỷ", "800 - 900 triệu"
        var explicitRange = Matches(normalizedText, @"(?<![\w.,])(?<n1>" + Number + @")\s*(?:-|–|den|toi)\s*(?<n2>" + Number + @")\s*(?<unit>ty|ti|trieu|tr)\b");
        if (explicitRange.Count == 1)
        {
            var n1 = ParseNumber(explicitRange[0].Groups["n1"].Value);
            var n2 = ParseNumber(explicitRange[0].Groups["n2"].Value);
            var mult = explicitRange[0].Groups["unit"].Value is "ty" or "ti" ? 1_000_000_000m : 1_000_000m;
            if (n1.HasValue && n2.HasValue && n1.Value <= n2.Value)
                return (n1.Value * mult, n2.Value * mult);
        }

        // 6. Dạng chính xác gộp: "1 tỷ 750", "4ty2"
        var compoundMatches = Matches(normalizedText, @"(?<![\w.,])(?<maj>\d{1,4})\s*(?:ty|ti)\s*(?<min>\d{1,3})(?:\s*(?:trieu|tr))?(?!\w)");
        if (compoundMatches.Count == 1)
        {
            var maj = decimal.Parse(compoundMatches[0].Groups["maj"].Value, CultureInfo.InvariantCulture);
            var minStr = compoundMatches[0].Groups["min"].Value;
            var minVal = decimal.Parse(minStr.PadRight(3, '0'), CultureInfo.InvariantCulture);
            var exact = maj * 1_000_000_000m + minVal * 1_000_000m;
            return (exact, exact);
        }

        // 7. Dạng chuẩn một con số: "4.5 tỷ", "850 triệu", "4,000,000,000"
        var priceMatches = Matches(normalizedText, @"(?<![\w.,])(?<n>" + Number + @")\s*(?<unit>ty|ti|trieu|tr|vnd|dong)(?!\w)");
        var prices = priceMatches.Select(m =>
        {
            var n = ParseNumber(m.Groups["n"].Value);
            var multiplier = m.Groups["unit"].Value switch { "ty" or "ti" => 1_000_000_000m, "trieu" or "tr" => 1_000_000m, _ => 1m };
            return n <= 9999999999999999.99m / multiplier ? n * multiplier : null;
        }).Where(p => p.HasValue).Select(p => p!.Value).Distinct().ToArray();

        if (prices.Length == 1) return (prices[0], prices[0]);

        var labeledPrice = Matches(normalizedText, @"\b(?:gia\s*ban|gia\s*chao|tai\s*chinh|gia)\s*[:\-]?\s*(?<n>" + Number + @")\s*(?<unit>ty|ti|trieu|tr)\b");
        if (labeledPrice.Count >= 1)
        {
            var n = ParseNumber(labeledPrice[0].Groups["n"].Value);
            var mult = labeledPrice[0].Groups["unit"].Value is "ty" or "ti" ? 1_000_000_000m : 1_000_000m;
            if (n.HasValue)
            {
                var exact = n.Value * mult;
                return (exact, exact);
            }
        }

        return (null, null);
    }

    public static ExtractedProperty Extract(string content, IReadOnlyList<string> provinces, IReadOnlyList<WardCandidate> wards)
    {
        var cleanContent = CleanIcons(content);
        var text = Normalize(cleanContent).Replace('²', '2');
        var areas = Matches(text, @"(?<![\w.,])(?<n>" + Number + @")\s*(?:m2|met vuong)(?!\w)")
            .Select(m => ParseNumber(m.Groups["n"].Value)).Where(n => n is > 0 and <= 9999999999.99m).Distinct().ToArray();
        decimal? area = areas.Length >= 1 ? areas[0] : null;
        var (rawLo, rawHi) = ExtractPriceRangeFromNormalized(text);
        decimal? exactPrice = rawLo.HasValue && rawHi.HasValue && rawLo.Value == rawHi.Value ? rawLo.Value : null;
        decimal? priceMin = rawLo.HasValue && rawHi.HasValue && rawLo.Value != rawHi.Value ? rawLo.Value : null;
        decimal? priceMax = rawLo.HasValue && rawHi.HasValue && rawLo.Value != rawHi.Value ? rawHi.Value : null;

        string? detectedType = new[]
        {
            ("Biệt thự", new[] { "biet thu" }),
            ("Chung cư", new[] { "chung cu" }),
            ("Căn hộ", new[] { "can ho" }),
            ("Đất nền", new[] { "ban dat", "mua dat", "lo dat", "dat nen", "dat tho cu", "dat dau gia" }),
            ("Nhà", new[] { "ban nha", "mua nha", "thue nha", "nha pho", "nha rieng", "nha 2 tang", "nha 3 tang", "nha cap 4" })
        }.Where(t => t.Item2.Any(p => Contains(text, p))).Select(t => t.Item1).FirstOrDefault();

        var foundProvinces = provinces.Where(p => Contains(text, Regex.Replace(Normalize(p),
            @"^(?:tinh|thanh pho)\s+", "", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))).Distinct().ToArray();
        var province = foundProvinces.Length == 1 ? foundProvinces[0] : null;
        if (province == null)
        {
            if (Contains(text, "nghe an") || Contains(text, "tp vinh") || Contains(text, "thanh pho vinh") || Contains(text, "cua lo"))
                province = "Nghệ An";
            else if (Contains(text, "ha tinh") || Contains(text, "hong linh") || Contains(text, "ky anh"))
                province = "Hà Tĩnh";
        }

        var foundWards = wards.Where(w => province != null && w.Province == province)
            .Where(w =>
            {
                var name = Normalize(w.Name);
                return Contains(text, name.StartsWith("phuong ") || name.StartsWith("xa ") || name.StartsWith("thi tran ")
                    ? name : "phuong " + name) || Contains(text, "xa " + name);
            }).Distinct().ToArray();
        var ward = foundWards.Length == 1 ? foundWards[0].Name : null;
        return new(exactPrice, priceMin, priceMax, area, detectedType, province, ward);
    }

    /// <summary>Chuyển GiaRaoVND dạng chuỗi (số hoặc mô tả ngắn như "2.5 tỷ") về số VNĐ nếu phân tích được.</summary>
    public static decimal? TryParseGiaRaoVnd(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var numeric) && numeric > 0)
            return numeric;
        if (trimmed.Contains("x", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("liên hệ", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("thỏa thuận", StringComparison.OrdinalIgnoreCase))
            return null;
        var (exact, _, _) = ExtractPriceInfo(trimmed);
        return exact is > 0 ? exact : null;
    }
}
