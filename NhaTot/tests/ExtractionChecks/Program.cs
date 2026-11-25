using NhaTot.Web.Services;

var provinces = new[] { "Nghệ An", "Hà Tĩnh" };
var wards = new[] { new WardCandidate("Hưng Bình", "Nghệ An"), new WardCandidate("Hưng Bình", "Hà Tĩnh") };
var cases = new (string Text, decimal? ExactPrice, decimal? PriceMin, decimal? PriceMax, decimal? Area, string? Type, string? Province, string? Ward)[]
{
    // Giá cụ thể -> ExactPrice có giá trị, PriceMin & PriceMax = null
    ("Bán nhà phường Hưng Bình, Nghệ An. Diện tích 100m² giá 2,5 tỷ.", 2500000000m, null, null, 100m, "Nhà", "Nghệ An", "Hưng Bình"),
    ("Bán đất 1.000 m2 giá 1.500 triệu", 1500000000m, null, null, 1000m, "Đất nền", null, null),
    ("Chung cư 65,5 m2 giá 1.8 tỷ", 1800000000m, null, null, 65.5m, "Chung cư", null, null),
    // Khoảng giá ước tính -> ExactPrice = null, PriceMin & PriceMax có giá trị
    ("Bán đất 100m2 giá 1 tỷ 7xx", null, 1701000000m, 1799000000m, 100m, "Đất nền", null, null),
    ("Bán nhà 90m2 giá 4 tỷ xxx", null, 4001000000m, 4999000000m, 90m, "Nhà", null, null),
    ("Bán đất 120m2 giá 8xx triệu", null, 801000000m, 899000000m, 120m, "Đất nền", null, null),
    // Không phải tổng giá bán -> cả hai đều null
    ("Bán đất 100m2 giá 25 triệu/m2", null, null, null, 100m, "Đất nền", null, null),
    // Bài cho thuê có giá thuê theo tháng -> lấy đúng giá thuê
    ("Cho thuê nhà 80 m2 giá 5 triệu/tháng", 5000000m, null, null, 80m, "Nhà", null, null),
    // Bài bán nhưng ghi trả góp mỗi tháng -> không coi là tổng giá bán
    ("Bán nhà 80 m2 giá 5 triệu mỗi tháng", null, null, null, 80m, "Nhà", null, null),
};
foreach (var c in cases)
{
    var r = RealEstateTextExtractor.Extract(c.Text, provinces, wards);
    if (r != new ExtractedProperty(c.ExactPrice, c.PriceMin, c.PriceMax, c.Area, c.Type, c.Province, c.Ward))
        throw new Exception($"Failed: {c.Text}\nActual: {r}");
}
Console.WriteLine($"Passed {cases.Length} exact-vs-range extraction cases.");
