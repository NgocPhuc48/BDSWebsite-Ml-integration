using System.Text.Json;
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
