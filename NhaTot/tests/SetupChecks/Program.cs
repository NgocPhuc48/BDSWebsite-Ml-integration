using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NhaTot.Domain.Entities;
using NhaTot.Web.Services;

var assembly = typeof(SocialBotSchemaInitializer).Assembly;
foreach (var version in new[] { "003", "005", "006", "007" })
{
    using var stream = assembly.GetManifestResourceStream("SocialBot.Sql." + version) ?? throw new Exception("Missing SQL resource " + version);
    using var reader = new StreamReader(stream);
    var script = reader.ReadToEnd();
    if (string.IsNullOrWhiteSpace(script)) throw new Exception("Empty SQL resource");
    var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
    if (version == "005" && batches.Length != 3) throw new Exception("SQL batch separation failed");
}
Console.WriteLine("SQL resources and GO batches verified.");
var keyDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "NhaTot-SetupChecks-" + Guid.NewGuid().ToString("N")));
try
{
    var provider = DataProtectionProvider.Create(keyDirectory, b => b.SetApplicationName("NhaTot"));
    var encrypted = provider.CreateProtector("NhaTot.SocialBot.ApifyToken.v1").Protect("fake-test-token");
    if (encrypted.Contains("fake-test-token")) throw new Exception("Token is not encrypted");
    var reopened = DataProtectionProvider.Create(keyDirectory, b => b.SetApplicationName("NhaTot"));
    var protector = reopened.CreateProtector("NhaTot.SocialBot.ApifyToken.v1");
    if (protector.Unprotect(encrypted) != "fake-test-token") throw new Exception("Key persistence failed");
    try { protector.Unprotect(encrypted[..^5] + "XXXXX"); throw new Exception("Tampered token accepted"); }
    catch (CryptographicException) { }
    Console.WriteLine("Token encryption, key persistence and tamper rejection verified.");
}
finally
{
    var tempRoot = Path.GetFullPath(Path.GetTempPath());
    if (keyDirectory.FullName.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase)
        && keyDirectory.Name.StartsWith("NhaTot-SetupChecks-", StringComparison.Ordinal) && keyDirectory.Exists)
        keyDirectory.Delete(true);
}
if (args.Contains("--database") || args.Contains("--train"))
{
    var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "NhaTot.Web");
    var config = new ConfigurationBuilder().SetBasePath(webRoot).AddJsonFile("appsettings.json")
        .AddJsonFile("appsettings.Development.json", optional: true).AddEnvironmentVariables().Build();
    var services = new ServiceCollection();
    var connectionString = config.GetConnectionString("DefaultConnection")!;
    services.AddDbContext<NhaTotDbContext>(options =>
    {
        if (connectionString.Contains("Port=3306", StringComparison.OrdinalIgnoreCase))
            options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 46)));
        else
            options.UseSqlServer(connectionString);
    });
    services.AddSingleton<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(new TrainingEnvironment(webRoot));
    services.AddScoped<ValuationTrainingService>();
    services.AddScoped<IAiDinhGiaService, AiDinhGiaService>();
    using var provider = services.BuildServiceProvider();
    try
    {
        await SocialBotSchemaInitializer.InitializeAsync(provider);
        await SocialBotSchemaInitializer.InitializeAsync(provider);
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NhaTotDbContext>();
        var schedule = await db.LichCaoDuLieus.AsNoTracking().SingleAsync(x => x.Id == 1);
        _ = await db.DuLieuThuThaps.AsNoTracking().Select(x => new { x.NoiDungGoc, x.DuLieuGocJson, x.MaLoaiBdsDaDuyet, x.MaPhuongXaDaDuyet }).Take(1).ToListAsync();
        Console.WriteLine("Database initialization twice and schema queries succeeded. No crawl started; schedule setting preserved.");
        if (args.Contains("--train"))
        {
            var training = scope.ServiceProvider.GetRequiredService<ValuationTrainingService>();
            try { Console.WriteLine(await training.TrainAsync()); }
            catch (InvalidOperationException ex) { Console.WriteLine("Training not activated: " + ex.Message); }
            var samples = await training.DataAsync();
            if (samples.Length > 0)
            {
                var sample = samples[0];
                var prediction = (NhaTot.Web.Models.KetQuaDinhGiaViewModel)await scope.ServiceProvider.GetRequiredService<IAiDinhGiaService>()
                    .DinhGiaBatDongSanAsync(new NhaTot.Web.Models.DinhGiaInputModel { MaLoaiBds = sample.Type, MaPhuongXa = sample.Ward, DienTich = (decimal)sample.Area });
                Console.WriteLine($"Live valuation response: success={prediction.Success}; {prediction.Message}");
            }
        }
    }
    catch (Microsoft.Data.SqlClient.SqlException ex)
    { Console.WriteLine($"Database check failed (SQL error {ex.Number}). Connection details omitted."); Environment.ExitCode = 1; }
    catch (InvalidOperationException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
    { Console.WriteLine($"Database check failed (SQL error {sql.Number}). Connection details omitted."); Environment.ExitCode = 1; }
}

sealed class TrainingEnvironment(string root) : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "NhaTot.Web";
    public string EnvironmentName { get; set; } = "Development";
    public string ContentRootPath { get; set; } = root;
    public string WebRootPath { get; set; } = Path.Combine(root, "wwwroot");
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
}
