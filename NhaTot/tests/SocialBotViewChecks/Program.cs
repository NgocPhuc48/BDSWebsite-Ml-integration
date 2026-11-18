using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NhaTot.Domain.Entities;
using NhaTot.Web.Areas.Admin.Controllers;
using NhaTot.Web.Areas.Admin.Models;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(SocialBotController).Assembly.GetName().Name,
    ContentRootPath = Path.Combine(Directory.GetCurrentDirectory(), "NhaTot.Web") });
builder.Services.AddControllersWithViews().AddApplicationPart(typeof(SocialBotController).Assembly);
builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
// Render dùng khóa tạm; không làm nóng key ring Windows của ứng dụng trong bài kiểm tra.
foreach (var registration in builder.Services.Where(x => x.ServiceType == typeof(IHostedService)
    && x.ImplementationType?.Name == "DataProtectionHostedService").ToArray()) builder.Services.Remove(registration);
builder.Logging.ClearProviders();
await using var app = builder.Build();
app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.Urls.Add("http://127.0.0.1:0");
await app.StartAsync();
using var scope = app.Services.CreateScope();

async Task<string> Render<T>(string action, T model)
{
    var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
    http.Request.Scheme = "https"; http.Request.Host = new HostString("localhost");
    http.Request.RouteValues = new RouteValueDictionary(new { area = "Admin", controller = "SocialBot", action });
    http.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "view-check"));
    var route = new RouteData(); route.Values["area"] = "Admin"; route.Values["controller"] = "SocialBot"; route.Values["action"] = action;
    var descriptor = new ControllerActionDescriptor { ControllerName = "SocialBot", ActionName = action,
        RouteValues = new Dictionary<string, string?> { ["area"] = "Admin", ["controller"] = "SocialBot", ["action"] = action } };
    var context = new ActionContext(http, route, descriptor);
    var engine = scope.ServiceProvider.GetRequiredService<IRazorViewEngine>();
    var found = engine.GetView(null, $"/Areas/Admin/Views/SocialBot/{action}.cshtml", true);
    if (!found.Success) throw new Exception("View missing: " + action);
    var data = new ViewDataDictionary<T>(scope.ServiceProvider.GetRequiredService<IModelMetadataProvider>(), new ModelStateDictionary()) { Model = model };
    data["TongSoNguon"] = 1; data["SoNguonHoatDong"] = 1; data["TongSoBaiCao"] = 0;
    data["CurrentKetQua"] = "ALL";
    data["Schedule"] = new LichCaoDuLieu { Id = 1 };
    using var writer = new StringWriter();
    await found.View.RenderAsync(new ViewContext(context, found.View, data,
        new TempDataDictionary(http, scope.ServiceProvider.GetRequiredService<ITempDataProvider>()), writer, new HtmlHelperOptions()));
    return writer.ToString();
}
var index = await Render("Index", new[] { new NguonDuLieu { MaNguon = 1, TenNguon = "Nhà O'Brien", TrangThai = "ACTIVE" } }.AsEnumerable());
if (index.Contains("asp-action=") || index.Contains("asp-append-version=") || index.Contains("src=\"~/")) throw new Exception("Tag helpers were not applied");
if (!index.Contains("href=\"/Admin/SocialBot/KetNoi\"") || !index.Contains("src=\"/js/socialbot-crawl.js?v=")
    || !index.Contains("src=\"/lib/bootstrap/dist/js/bootstrap.bundle.min.js\""))
{
    foreach (System.Text.RegularExpressions.Match tag in System.Text.RegularExpressions.Regex.Matches(index, "<(?:a|script)\\b[^>]*>"))
        if (tag.Value.Contains("KetNoi") || tag.Value.Contains("socialbot-crawl") || tag.Value.Contains("bootstrap.bundle")) Console.WriteLine(tag.Value);
    throw new Exception("Admin links/scripts unresolved");
}
if (index.Contains("onclick=\"confirmDelete(1, '") || !index.Contains("data-source-name=")) throw new Exception("Source name embedded in JavaScript");
var connection = await Render("KetNoi", new ApifyConnectionModel());
if (!connection.Contains("action=\"/Admin/SocialBot/KetNoi\"") || !connection.Contains("name=\"__RequestVerificationToken\"")) throw new Exception("Connection form action/token missing");
var logs = await Render("NhatKyXuLy", Array.Empty<LichSuXuLyDuLieu>().AsEnumerable());
if (!logs.Contains("action=\"/Admin/SocialBot/NhatKyXuLy\"")) throw new Exception("Log filter action missing");
var schedule = await Render("LichCao", new CrawlScheduleModel());
if (!schedule.Contains("action=\"/Admin/SocialBot/LichCao\"") || !schedule.Contains("name=\"IntervalMinutes\"")) throw new Exception("Schedule form not bound");
var review = await Render("Duyet", new CrawlReviewModel { Id = 1, Title = "Test" });
if (!review.Contains("action=\"/Admin/SocialBot/Duyet\"") || !review.Contains("name=\"PropertyTypeId\"")) throw new Exception("Review form not bound");
Console.WriteLine("SocialBot rendered HTML checks passed: links, local scripts, token form, antiforgery, quote-safe delete and log view.");
