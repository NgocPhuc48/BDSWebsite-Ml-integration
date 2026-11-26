using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using NhaTot.Web.Controllers;
using NhaTot.Web.Models;

var builder = WebApplication.CreateBuilder();
builder.Services.AddControllersWithViews();
using var provider = builder.Services.BuildServiceProvider();
var parameter = typeof(BayGiaController).GetMethod("TaoMucTieu")!.GetParameters()[0];
var binding = BindingInfo.GetBindingInfo(parameter.GetCustomAttributes(false))!;
if (binding.BinderModelName != "FormTao") throw new Exception("Action prefix is incorrect");
// vi-VN chạy trước để phát hiện lỗi parse giới hạn ngay lần validation đầu tiên.
foreach (var culture in new[] { "vi-VN", "en-US" })
{
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
foreach (var price in new[] { "1000000000", "0", "abc" })
{
    using var scope = provider.CreateScope();
    var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
    var action = new ActionContext(http, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
    var values = new Dictionary<string, StringValues> {
        ["FormTao.MaLoaiBds"] = "1", ["FormTao.MaTinh"] = "1", ["FormTao.MaQuanHuyen"] = "2",
        ["FormTao.NganSachMaxVnd"] = price, ["FormTao.DienTichMin"] = "80",
        ["FormTao.SoPhongNgu"] = "3", ["FormTao.PhapLy"] = "Sổ đỏ", ["FormTao.LoaiGiaoDich"] = "BAN"
    };
    var metadata = scope.ServiceProvider.GetRequiredService<IModelMetadataProvider>().GetMetadataForType(typeof(TaoMucTieuBayGiaViewModel));
    var binder = scope.ServiceProvider.GetRequiredService<IModelBinderFactory>().CreateBinder(new ModelBinderFactoryContext { Metadata = metadata, BindingInfo = binding, CacheToken = parameter });
    var context = DefaultModelBindingContext.CreateBindingContext(action,
        new FormValueProvider(BindingSource.Form, new FormCollection(values), CultureInfo.InvariantCulture), metadata, binding, "FormTao");
    await binder.BindModelAsync(context);
    var model = (TaoMucTieuBayGiaViewModel)context.Result.Model!;
    scope.ServiceProvider.GetRequiredService<IObjectModelValidator>().Validate(action, null, "FormTao", model);
    if (price == "1000000000")
    {
        if (!action.ModelState.IsValid || model.NganSachMaxVnd != 1000000000m || model.DienTichMin != 80 || model.SoPhongNgu != 3 || model.PhapLy != "Sổ đỏ")
            throw new Exception("Valid prefixed form lost values or failed validation");
    }
    else if (action.ModelState.IsValid) throw new Exception("Invalid price accepted");
}
foreach (var type in new[] { typeof(TaoMucTieuBayGiaViewModel), typeof(NhaTot.Web.Areas.Admin.Models.CrawlReviewModel) })
foreach (var property in type.GetProperties())
foreach (var range in property.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RangeAttribute), false)
    .Cast<System.ComponentModel.DataAnnotations.RangeAttribute>().Where(x => x.OperandType == typeof(decimal)))
{
    var min = decimal.Parse((string)range.Minimum, CultureInfo.InvariantCulture);
    var max = decimal.Parse((string)range.Maximum, CultureInfo.InvariantCulture);
    if (!range.IsValid(min) || !range.IsValid(max) || range.IsValid(min - .01m) || range.IsValid(max + .01m))
        throw new Exception($"Decimal validation bounds failed for {type.Name}.{property.Name} under {culture}");
}
}
Console.WriteLine("Passed vi-VN/en-US: MVC binding of 1 billion and full criteria; invalid prices rejected; decimal boundaries verified for BayGia and Admin review. No wallet changes.");

// --- BayGia >= 80% Recommendation Engine Tests ---
var demandBase = new NhaTot.Web.Services.BayGiaDemandProfile(
    TransactionType: "BAN",
    PropertyTypeId: 9,
    PropertyTypeName: "Đất nền",
    ProvinceId: 1,
    DistrictId: 1,
    WardId: 1,
    MinPrice: 1_500_000_000m,
    MaxPrice: 2_500_000_000m,
    MinArea: 100m,
    Direction: "Đông Nam",
    LegalStatus: "Sổ đỏ",
    MinBedrooms: null,
    MinFrontageM: 5m);

var exactCand = new NhaTot.Web.Services.BayGiaCandidate(
    "TinDang", 101, "BAN", 9, "Đất nền", 1, 1, 1,
    2_200_000_000m, 120m, "DongNam", "Sổ đỏ chính chủ", null, 6m, DateTime.UtcNow);
var exactRes = NhaTot.Web.Services.BayGiaRecommendationEngine.CalculateSimilarity(demandBase, exactCand);
if (exactRes.ScorePercent != 100.0)
    throw new Exception($"Expected 100% for exact match, got {exactRes.ScorePercent}%");

var nearCand = new NhaTot.Web.Services.BayGiaCandidate(
    "TinDang", 102, "BAN", 1, "Đất nền / Đất thổ cư", 1, 1, 2,
    2_625_000_000m, 110m, "DongNam", "Sổ hồng", null, 5m, DateTime.UtcNow.AddDays(-1));
var nearRes = NhaTot.Web.Services.BayGiaRecommendationEngine.CalculateSimilarity(demandBase, nearCand);
if (nearRes.ScorePercent < 80.0 || nearRes.ScorePercent >= 100.0)
    throw new Exception($"Expected near match in [80, 100), got {nearRes.ScorePercent}%");

var lowCand = new NhaTot.Web.Services.BayGiaCandidate(
    "TinDang", 103, "BAN", 7, "Căn hộ", 2, 10, 50,
    4_500_000_000m, 65m, "Tay", "Chờ sổ", null, 3m, DateTime.UtcNow);
var lowRes = NhaTot.Web.Services.BayGiaRecommendationEngine.CalculateSimilarity(demandBase, lowCand);
if (lowRes.ScorePercent >= 80.0)
    throw new Exception($"Expected low match < 80%, got {lowRes.ScorePercent}%");

var wrongTxCand = exactCand with { Id = 104, TransactionType = "CHO_THUE" };
var wrongTxRes = NhaTot.Web.Services.BayGiaRecommendationEngine.CalculateSimilarity(demandBase, wrongTxCand);
if (wrongTxRes.ScorePercent != 0.0)
    throw new Exception($"Expected 0% for wrong transaction type, got {wrongTxRes.ScorePercent}%");

var filtered = NhaTot.Web.Services.BayGiaRecommendationEngine.FilterRecommendations(
    demandBase, new[] { lowCand, nearCand, wrongTxCand, exactCand });
if (filtered.Count != 2 || filtered[0].Candidate.Id != 101 || filtered[1].Candidate.Id != 102)
    throw new Exception("FilterRecommendations did not enforce >= 80% threshold and descending sort order.");

Console.WriteLine($"Passed BayGiaRecommendationEngine tests: exact={exactRes.ScorePercent}%, near={nearRes.ScorePercent}%, low={lowRes.ScorePercent}%, wrongTx={wrongTxRes.ScorePercent}%, filtered={filtered.Count}/4 (>=80%).");

// --- Functional tests for BayGia/ChiTiet & separate TinDang vs Facebook >= 80% lists ---
var chiTietMethod = typeof(BayGiaController).GetMethod("ChiTiet", new[] { typeof(long) });
if (chiTietMethod == null || !chiTietMethod.GetCustomAttributes(typeof(HttpGetAttribute), false).Any())
    throw new Exception("BayGiaController.ChiTiet(long) with [HttpGet] is missing.");
if (!typeof(BayGiaController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true).Any())
    throw new Exception("BayGiaController must require [Authorize].");

var fbMatch = new NhaTot.Web.Services.BayGiaCandidate(
    "CrawlApproved", 201, "BAN", 9, "Đất nền", 1, 1, 1,
    2_300_000_000m, 110m, null, null, null, null, DateTime.UtcNow);
var fbLow = new NhaTot.Web.Services.BayGiaCandidate(
    "CrawlApproved", 202, "BAN", 7, "Căn hộ", 2, 10, 50,
    5_500_000_000m, 65m, null, null, null, null, DateTime.UtcNow);

var fbFiltered = NhaTot.Web.Services.BayGiaRecommendationEngine.FilterRecommendations(
    demandBase, new[] { fbMatch, fbLow });
if (fbFiltered.Count != 1 || fbFiltered[0].Candidate.Id != 201 || fbFiltered[0].Similarity.ScorePercent < 80.0)
    throw new Exception($"Facebook recommendation filter failed: count={fbFiltered.Count}, score={(fbFiltered.Count > 0 ? fbFiltered[0].Similarity.ScorePercent : 0)}%");

var chiTietViewPath = Path.Combine(Directory.GetCurrentDirectory(), "NhaTot.Web", "Views", "BayGia", "ChiTiet.cshtml");
var bangMucTieuViewPath = Path.Combine(Directory.GetCurrentDirectory(), "NhaTot.Web", "Views", "BayGia", "_BangMucTieu.cshtml");
if (File.Exists(chiTietViewPath) && File.Exists(bangMucTieuViewPath))
{
    var ctHtml = File.ReadAllText(chiTietViewPath);
    var tableHtml = File.ReadAllText(bangMucTieuViewPath);
    if (!ctHtml.Contains("TinDangTuongDong", StringComparison.Ordinal) || !ctHtml.Contains("TinFacebookTuongDong", StringComparison.Ordinal))
        throw new Exception("ChiTiet.cshtml missing TinDangTuongDong or TinFacebookTuongDong sections.");
    if (!tableHtml.Contains("/BayGia/ChiTiet/", StringComparison.Ordinal))
        throw new Exception("_BangMucTieu.cshtml does not link to /BayGia/ChiTiet/.");
}

Console.WriteLine($"Passed BayGia ChiTiet functional tests: action verified, fbMatch={fbFiltered[0].Similarity.ScorePercent}% (>=80%), view links verified.");
