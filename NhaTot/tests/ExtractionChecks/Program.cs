using NhaTot.Web.Services;

var provinces = new[] { "Nghệ An", "Hà Tĩnh" };
var wards = new[] { new WardCandidate("Hưng Bình", "Nghệ An"), new WardCandidate("Hưng Bình", "Hà Tĩnh") };
var cases = new (string Text, decimal? Price, decimal? Area, string? Type, string? Province, string? Ward)[]
{
    ("Bán nhà phường Hưng Bình, Nghệ An. Diện tích 100m² giá 2,5 tỷ.", 2500000000m, 100m, "Nhà", "Nghệ An", "Hưng Bình"),
    ("Bán đất 1.000 m2 giá 1.500 triệu", 1500000000m, 1000m, "Đất", null, null),
    ("Chung cư 65,5 m2 giá 1.8 tỷ", 1800000000m, 65.5m, "Căn hộ", null, null),
    ("Bán đất 100m2 giá 25 triệu/m2", null, 100m, "Đất", null, null),
    ("Cho thuê nhà 80 m2 giá 5 triệu/tháng", null, 80m, "Nhà", null, null),
    ("Bán nhà 80 m2 giá 5 triệu mỗi tháng", null, 80m, "Nhà", null, null),
    ("Bán đất 100-120 m2 giá 2-3 tỷ", null, null, "Đất", null, null),
    ("Bán đất 100 m2 hoặc 120 m2 giá 2 tỷ hoặc 3 tỷ", null, null, "Đất", null, null),
    ("Bán nhà 100m2 giá 1 tỷ 500 triệu", null, 100m, "Nhà", null, null),
    ("Bán nhà và bán đất, liên hệ 0987654321", null, null, null, null, null),
    ("Nghệ An và Hà Tĩnh, phường Hưng Bình", null, null, null, null, null),
    ("Phường Hưng Bình", null, null, null, null, null),
    ("Ban nha phuong Hung Binh, Nghe An. Gia 2 ty, 100 m2", 2000000000m, 100m, "Nhà", "Nghệ An", "Hưng Bình"),
    ("Bán nhà giá thương lượng, ngang 5m dài 20m", null, null, "Nhà", null, null),
    ("Bán đất 0m2 giá 0 đồng", null, null, "Đất", null, null),
    ("Bán đất giá 999999999999999999999999999999999999 tỷ", null, null, "Đất", null, null)
    ,("Bán đất 100m2 giá từ 2 tỷ", null, 100m, "Đất", null, null)
    ,("Bán nhà 80m2 giá 2 tỷ 5", null, 80m, "Nhà", null, null)
};
foreach (var c in cases)
{
    var r = RealEstateTextExtractor.Extract(c.Text, provinces, wards);
    if (r != new ExtractedProperty(c.Price, c.Area, c.Type, c.Province, c.Ward))
        throw new Exception($"Failed: {c.Text}\nActual: {r}");
}
Console.WriteLine($"Passed {cases.Length} extraction cases.");
var matches = new (decimal? Price, decimal? Area, decimal? Min, decimal? Max, decimal? MinArea, bool Expected)[]
{
    (1100, 100, 500, 1000, 100, true),
    (1101, 100, 500, 1000, 100, false),
    (499, 100, 500, 1000, 100, false),
    (1000, 99, 500, 1000, 100, false),
    (null, 100, null, 1000, null, false),
    (1000, null, null, 1000, null, false),
    (0, 100, null, 1000, null, false),
    (1000, 100, null, 1000, null, true)
};
foreach (var c in matches)
    if (CrawlSuggestionPolicy.Fits(c.Price, c.Area, c.Min, c.Max, c.MinArea) != c.Expected)
        throw new Exception($"Suggestion policy failed: {c}");
Console.WriteLine($"Passed {matches.Length} suggestion boundary cases.");
var now = new DateTime(2026, 11, 18, 0, 0, 0, DateTimeKind.Utc);
if (CrawlScheduleRules.Due(false, null, "IDLE", null, now)
    || CrawlScheduleRules.Due(true, "running-id", "RUNNING", now, now)
    || CrawlScheduleRules.Due(true, null, "STARTING", now, now)
    || CrawlScheduleRules.Due(true, null, "IDLE", now.AddMinutes(1), now)
    || !CrawlScheduleRules.Due(true, null, "IDLE", now, now)
    || !CrawlScheduleRules.Due(true, null, "IDLE", null, now)) throw new Exception("Schedule due checks failed");
if (!CrawlScheduleRules.Valid(15, 1, 1) || !CrawlScheduleRules.Valid(1440, 100, 30)
    || CrawlScheduleRules.Valid(14, 10, 1) || CrawlScheduleRules.Valid(60, 101, 1)
    || CrawlScheduleRules.Valid(60, 10, 2)) throw new Exception("Schedule configuration checks failed");
Console.WriteLine("Passed 11 schedule checks.");
