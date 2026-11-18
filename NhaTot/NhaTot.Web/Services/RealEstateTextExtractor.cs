using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NhaTot.Web.Services;

public sealed record WardCandidate(string Name, string? Province);
public sealed record ExtractedProperty(decimal? Price, decimal? Area, string? Type, string? Province, string? Ward)
{
    public bool Complete => Price.HasValue && Area.HasValue && Type != null && Province != null && Ward != null;
}

/// <summary>Quy tắc bảo thủ; các con số là giá rao, không phải giá giao dịch hay kết quả định giá.</summary>
public static class RealEstateTextExtractor
{
    private const string Number = @"\d+(?:[.,]\d+)*";
    private static MatchCollection Matches(string text, string pattern) => Regex.Matches(text, pattern,
        RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static string Normalize(string text) => string.Concat(text.ToLowerInvariant().Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)).Replace('đ', 'd')
        .Normalize(NormalizationForm.FormC);

    private static decimal? ParseNumber(string value)
    {
        // 1.500 / 1,500 là phân nhóm nghìn; 1,5 / 1.5 là thập phân.
        if (Regex.IsMatch(value, @"^\d{1,3}([.,]\d{3})+$", RegexOptions.None, TimeSpan.FromSeconds(1)))
            value = value.Replace(".", "").Replace(",", "");
        else if (value.Count(c => c is '.' or ',') <= 1) value = value.Replace(',', '.');
        else return null;
        return decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var n) && n > 0 ? n : null;
    }

    private static bool Contains(string text, string phrase) => Matches(text, @"(?<!\w)" + Regex.Escape(phrase) + @"(?!\w)").Count > 0;

    public static ExtractedProperty Extract(string content, IReadOnlyList<string> provinces, IReadOnlyList<WardCandidate> wards)
    {
        var text = Normalize(content).Replace('²', '2');
        // Khoảng giá/diện tích, nhiều lô hoặc nhiều giá trị đều cần người kiểm tra.
        var range = Matches(text, Number + @"\s*(?:-|–|den|toi)\s*" + Number).Count > 0;
        var areas = Matches(text, @"(?<![\w.,])(?<n>" + Number + @")\s*(?:m2|met vuong)(?!\w)")
            .Select(m => ParseNumber(m.Groups["n"].Value)).Where(n => n is > 0 and <= 9999999999.99m).Distinct().ToArray();
        decimal? area = !range && areas.Length == 1 ? areas[0] : null;
        var priceMatches = Matches(text, @"(?<![\w.,])(?<n>" + Number + @")\s*(?<unit>ty|ti|trieu|tr|vnd|dong)(?!\w)");
        var prices = priceMatches.Select(m =>
        {
            var n = ParseNumber(m.Groups["n"].Value);
            var multiplier = m.Groups["unit"].Value switch { "ty" or "ti" => 1000000000m, "trieu" or "tr" => 1000000m, _ => 1m };
            return n <= 9999999999999999.99m / multiplier ? n * multiplier : null;
        }).Distinct().ToArray();
        var unitPriceOrRent = Matches(text, @"/\s*(?:m2|met|thang|nam|ngay)|\b(?:moi|tren)\s+(?:m2|thang)|\b(?:thue|thang)\b").Count > 0;
        var uncertainPrice = Matches(text, @"\b(?:tu|khoang|tam|hon|duoi|tren)\s+" + Number + @"\s*(?:ty|ti|trieu|tr)\b|\b(?:ty|ti|trieu|tr)\s+\d").Count > 0;
        decimal? price = !range && !unitPriceOrRent && !uncertainPrice && priceMatches.Count == 1 && prices.Length == 1 ? prices[0] : null;
        var types = new[]
        {
            ("Căn hộ", new[] { "can ho", "chung cu" }), ("Đất", new[] { "ban dat", "mua dat", "lo dat", "dat nen", "dat tho cu" }),
            ("Nhà", new[] { "ban nha", "mua nha", "thue nha", "nha pho", "nha rieng" }),
            ("Biệt thự", new[] { "biet thu" }), ("Kho xưởng", new[] { "nha xuong", "kho xuong" })
        }.Where(t => t.Item2.Any(p => Contains(text, p))).Select(t => t.Item1).ToArray();
        var foundProvinces = provinces.Where(p => Contains(text, Regex.Replace(Normalize(p),
            @"^(?:tinh|thanh pho)\s+", "", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))).Distinct().ToArray();
        var province = foundProvinces.Length == 1 ? foundProvinces[0] : null;
        var foundWards = wards.Where(w => province != null && w.Province == province)
            .Where(w => {
                var name = Normalize(w.Name);
                return Contains(text, name.StartsWith("phuong ") || name.StartsWith("xa ") || name.StartsWith("thi tran ")
                    ? name : "phuong " + name) || Contains(text, "xa " + name);
            }).Distinct().ToArray();
        var ward = foundWards.Length == 1 ? foundWards[0].Name : null;
        return new(price, area, types.Length == 1 ? types[0] : null, province, ward);
    }
}
