using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using NhaTot.Web.Models;
using NhaTot.Web.Services;

// Chỉ dùng dữ liệu tổng hợp cho kiểm tra thuật toán, không ghi vào database hoặc triển khai làm model thật.
var data = Enumerable.Range(0, 180).Select(i => {
    var area = 40 + (i*37)%250; var district = i%2+1;
    return new ValuationSample("test:"+i, 1, district, district*10, area,
        Math.Exp(18 + .8*Math.Log(area) + district*.3 + i*.0002), new DateTime(2024,1,1).AddDays(i*2));
}).ToArray();
var model = ValuationRegression.Train(data);
if (!model.Accepted || model.Metrics.Mape > .15 || !double.IsFinite(model.Metrics.R2)) throw new Exception("Regression did not beat baseline on held-out data");
if (model.Support.Any(x => x.Date >= data[108].Date.Date)) throw new Exception("Temporal training leakage");
if (model.Supports(data[0] with { Type = 99 }) || model.Supports(data[0] with { Area = 100000 })) throw new Exception("Unsupported request accepted");
var restored = JsonSerializer.Deserialize<ValuationArtifact>(JsonSerializer.Serialize(model))!;
if (Math.Abs(restored.Predict(data[150]) - model.Predict(data[150])) > .01) throw new Exception("Artifact roundtrip changed prediction");
if (ValuationRegression.Clean(data.Concat(data)).Length != data.Length) throw new Exception("Duplicate samples leaked");
if (ValuationRegression.Clean([data[0] with { Price = double.NaN }, data[1] with { Area = -1 }]).Length != 0) throw new Exception("Invalid samples accepted");
try { ValuationRegression.Train(data.Take(20)); throw new Exception("Insufficient dataset accepted"); }
catch (InvalidOperationException) { }
try { ValuationRegression.Train(data.Select(x => x with { Date = new DateTime(2024,1,1) })); throw new Exception("Same-day split accepted"); }
catch (InvalidOperationException) { }
Console.WriteLine($"Valuation checks passed; synthetic holdout MAPE {model.Metrics.Mape:P2}, MAE/baseline {model.Metrics.Mae/model.Metrics.BaselineMae:F3}.");

// 6-month forecast sanity checks
var sampleForecast = data[150];
for (var m = 1; m <= 6; m++)
{
    var p = model.Predict(sampleForecast with { Date = sampleForecast.Date.AddMonths(m) });
    var low = p * Math.Exp(-model.LogError90);
    var high = p * Math.Exp(model.LogError90);
    if (!double.IsFinite(p) || p <= 0 || !(low < p && p < high))
        throw new Exception($"6-month forecast sanity check failed at month +{m}.");
}

// DinhGiaInputModel validation checks
var validInput = new DinhGiaInputModel { MaLoaiBds = 9, MaPhuongXa = 1, DienTich = 100 };
if (!Validator.TryValidateObject(validInput, new ValidationContext(validInput), null, validateAllProperties: true))
    throw new Exception("Valid DinhGiaInputModel failed validation.");

foreach (var invalidArea in new[] { 0m, 100001m })
{
    var badInput = new DinhGiaInputModel { MaLoaiBds = 9, MaPhuongXa = 1, DienTich = invalidArea };
    if (Validator.TryValidateObject(badInput, new ValidationContext(badInput), null, validateAllProperties: true))
        throw new Exception($"Invalid DienTich={invalidArea} unexpectedly passed validation.");
}

// DinhGia view contract check (when executed from NhaTot root)
var dinhGiaViewPath = Path.Combine(Directory.GetCurrentDirectory(), "NhaTot.Web", "Views", "DinhGia", "Index.cshtml");
if (File.Exists(dinhGiaViewPath))
{
    var viewHtml = File.ReadAllText(dinhGiaViewPath);
    foreach (var token in new[] { "id=\"formDinhGia\"", "addEventListener('submit'", "id=\"chartDuBao\"", "id=\"tblDuBao6Thang\"", "RequestVerificationToken", "/DinhGia/TinhGia" })
    {
        if (!viewHtml.Contains(token, StringComparison.Ordinal))
            throw new Exception($"DinhGia view missing expected contract token: {token}");
    }
}

Console.WriteLine("Passed additional DinhGia checks: 6-month forecast bounds, DinhGiaInputModel validation, and view contract.");
