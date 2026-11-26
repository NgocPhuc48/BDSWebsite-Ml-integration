using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using NhaTot.Domain.Entities;
using NhaTot.Web.Services;

// 1. Synthetic non-linear real-estate test for GradientBoostedPriceModel (XGBoost-style GBDT)
var rng = new Random(42);
var synth = Enumerable.Range(0, 240).Select(i =>
{
    var type = (i % 3) + 1;
    var dist = (i % 4) + 1;
    var ward = dist * 10 + (i % 2);
    var area = 50.0 + (i * 19 % 180);
    // Non-linear log price with interaction between district and property type
    var logUnit = 16.8 + 0.18 * type + 0.22 * dist + (type == 2 && dist >= 3 ? 0.25 : 0.0);
    var price = Math.Exp(logUnit) * area * (0.96 + rng.NextDouble() * 0.08);
    return new ValuationSample($"s:{i}", type, dist, ward, area, price, new DateTime(2025, 1, 1).AddDays(i));
}).ToArray();

var gbt = GradientBoostedPriceModel.Train(synth);
if (gbt.Metrics.Mape >= 0.15 || gbt.Metrics.R2 <= 0.85 || gbt.Metrics.Mae >= gbt.Metrics.BaselineMae)
    throw new Exception($"GBDT synthetic accuracy check failed: MAPE={gbt.Metrics.Mape:P2}, R2={gbt.Metrics.R2:0.000}, MAE={gbt.Metrics.Mae:N0}, BaseMAE={gbt.Metrics.BaselineMae:N0}");

Console.WriteLine($"[PASS] Synthetic GBDT: trees={gbt.Trees.Count}, MAPE={gbt.Metrics.Mape:P2}, R2={gbt.Metrics.R2:0.000}, MAE={gbt.Metrics.Mae:N0} < Baseline={gbt.Metrics.BaselineMae:N0}");

// 2. Price deviation moderation classification boundary tests
var pred = 2_000_000_000m;
var boundaryTests = new (decimal? Listed, ModerationRiskLevel Expected, string Label)[]
{
    (null, ModerationRiskLevel.Unverified, "null price"),
    (0m, ModerationRiskLevel.Unverified, "zero price"),
    (2_300_000_000m, ModerationRiskLevel.Normal, "+15% normal"),
    (3_800_000_000m, ModerationRiskLevel.Normal, "+90% normal"),
    (4_000_000_000m, ModerationRiskLevel.Warning, "+100% warning"),
    (5_000_000_000m, ModerationRiskLevel.Warning, "+150% warning"),
    (5_998_000_000m, ModerationRiskLevel.Warning, "+199.9% warning"),
    (6_000_000_000m, ModerationRiskLevel.Critical, "+200% critical"),
    (8_400_000_000m, ModerationRiskLevel.Critical, "+320% critical"),
    (900_000_000m, ModerationRiskLevel.Warning, "-55% bait cheap warning"),
    (600_000_000m, ModerationRiskLevel.Critical, "-70% extreme cheap critical")
};

foreach (var (listed, expected, label) in boundaryTests)
{
    var res = ListingModerationService.ClassifyPriceDeviation(listed, pred);
    if (res.Level != expected)
        throw new Exception($"Boundary test '{label}' failed: expected {expected}, got {res.Level} ({res.DeviationPercent}%)");
}
Console.WriteLine($"[PASS] All {boundaryTests.Length} moderation risk classification boundary tests passed.");

// 3. Real SQL Server database benchmark (--db)
if (args.Contains("--db"))
{
    var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "NhaTot.Web");
    var config = new ConfigurationBuilder()
        .SetBasePath(webRoot)
        .AddJsonFile("appsettings.json", optional: false)
        .Build();
    var services = new ServiceCollection();
    services.AddDbContext<NhaTotDbContext>(o => o.UseSqlServer(config.GetConnectionString("DefaultConnection")!));
    services.AddSingleton<IWebHostEnvironment>(new TestWebEnv(webRoot));
    services.AddScoped<ValuationTrainingService>();
    using var sp = services.BuildServiceProvider();
    using var scope = sp.CreateScope();
    var training = scope.ServiceProvider.GetRequiredService<ValuationTrainingService>();
    var realData = await training.DataAsync();
    var realGbt = GradientBoostedPriceModel.Train(realData);
    Console.WriteLine($"[PASS] Real DB GBDT: total={realGbt.SampleCount}, train={realGbt.TrainCount}, test={realGbt.TestCount}, MAPE={realGbt.Metrics.Mape:P1}, R2={realGbt.Metrics.R2:0.000}, MAE={realGbt.Metrics.Mae:N0} vs Baseline={realGbt.Metrics.BaselineMae:N0}");

    var realSample = realData[^1];
    var est = (decimal)Math.Round(realGbt.Predict(realSample));
    var cNorm = ListingModerationService.ClassifyPriceDeviation(est, est);
    var cWarn = ListingModerationService.ClassifyPriceDeviation(est * 2.4m, est); // +140% (100-200%)
    var cCrit = ListingModerationService.ClassifyPriceDeviation(est * 3.5m, est); // +250% (>=200%)
    if (cNorm.Level != ModerationRiskLevel.Normal || cWarn.Level != ModerationRiskLevel.Warning || cCrit.Level != ModerationRiskLevel.Critical)
        throw new Exception("Real DB moderation classification check failed.");
    Console.WriteLine($"[PASS] Real sample est={est:N0} đ -> normal={cNorm.BadgeText}, +140%={cWarn.BadgeText} ({cWarn.DeviationPercent}%), +250%={cCrit.BadgeText} ({cCrit.DeviationPercent}%)");
}

sealed class TestWebEnv(string root) : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "NhaTot.Web";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = Path.Combine(root, "wwwroot");
    public string EnvironmentName { get; set; } = "Production";
    public string ContentRootPath { get; set; } = root;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
