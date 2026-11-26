using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using NhaTot.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
var builder = WebApplication.CreateBuilder(args);
var protection = builder.Services.AddDataProtection().SetApplicationName("NhaTot")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys")));
if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();

// Đăng ký dịch vụ MVC và AI Định giá
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IAiDinhGiaService, AiDinhGiaService>();
builder.Services.AddScoped<ValuationTrainingService>();
builder.Services.AddScoped<ListingModerationService>();
builder.Services.AddHostedService<ValuationTrainingWorker>();
builder.Services.AddHttpClient<ApifyCrawlService>();
builder.Services.AddScoped<ApifyTokenStore>();
builder.Services.AddHttpClient("ApifyConnectionCheck", client => client.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddScoped<CrawlImportService>();
builder.Services.AddScoped<CrawlExtractionService>();
builder.Services.AddScoped<CrawlSuggestionService>();
builder.Services.AddHostedService<CrawlScheduleWorker>();
builder.Services.AddMemoryCache();

// Đăng ký DbContext kết nối SQL Server (hoặc MySQL nếu connection string cấu hình Port=3306)
builder.Services.AddDbContext<NhaTotDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=localhost;Database=NhaTot;User Id=sa;Password=123456;TrustServerCertificate=True;";
    if (connectionString.Contains("Port=3306", StringComparison.OrdinalIgnoreCase))
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 46)));
    else
        options.UseSqlServer(connectionString);
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Đổi đường dẫn mặc định từ /Account/Login thành /TaiKhoan/DangNhap
        options.LoginPath = "/TaiKhoan/DangNhap"; 
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });
var app = builder.Build();
// Cập nhật trước khi nhận request hoặc khởi động worker; chỉ thêm schema và chạy mỗi phiên bản một lần.
await SocialBotSchemaInitializer.InitializeAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Route Client đặt sau Area route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
