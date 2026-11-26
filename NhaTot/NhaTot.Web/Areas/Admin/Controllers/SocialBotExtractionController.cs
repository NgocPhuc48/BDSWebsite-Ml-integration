using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using NhaTot.Web.Models;
using NhaTot.Web.Services;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace NhaTot.Web.Areas.Admin.Controllers;

public partial class SocialBotController
{
    [HttpGet]
    public async Task<IActionResult> DuLieu(int page = 1, CancellationToken ct = default)
    {
        const int pageSize = 25;
        var query = _context.DuLieuThuThaps.AsNoTracking();
        var totalItems = await query.CountAsync(ct);
        page = PaginationViewModel.NormalizePage(page, pageSize, totalItems);
        ViewBag.Pagination = new PaginationViewModel(page, pageSize, totalItems);

        var items = await query.Include(x => x.MaNguonNavigation)
            .OrderByDescending(x => x.MaDuLieu)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ExtractPending([FromServices] CrawlExtractionService extraction, CancellationToken ct)
    {
        try
        {
            var count = await extraction.ProcessPendingAsync(ct);
            TempData["ExtractionMessage"] = $"Đã bóc tách {count} bài NEW. Mỗi lần xử lý tối đa 100 bài; các bài thiếu thông tin cần kiểm tra lại.";
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            TempData["ExtractionMessage"] = "Không xử lý được dữ liệu. Kiểm tra kết nối database rồi thử lại.";
        }
        catch (DbUpdateException) { TempData["ExtractionMessage"] = "Không lưu được kết quả bóc tách. Hãy thử lại."; }
        return RedirectToAction(nameof(DuLieu));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAllDuLieu(CancellationToken ct)
    {
        try
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            await _context.LichSuXuLyDuLieus.Where(x => x.MaDuLieu != null).ExecuteDeleteAsync(ct);
            await _context.LeadKhachHangNhuCaus.Where(x => x.MaDuLieu != null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.MaDuLieu, (long?)null), ct);
            var deleted = await _context.DuLieuThuThaps.ExecuteDeleteAsync(ct);
            await tx.CommitAsync(ct);

            var provider = _context.Database.ProviderName ?? "";
            if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('DuLieuThuThap', RESEED, 0)", ct);
            }
            else
            {
                await _context.Database.ExecuteSqlRawAsync("ALTER TABLE `DuLieuThuThap` AUTO_INCREMENT = 1", ct);
            }

            TempData["ExtractionMessage"] = $"Đã xóa toàn bộ {deleted} bài viết đã thu thập và đặt lại bộ đếm về 0.";
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            TempData["ExtractionMessage"] = "Không xóa được dữ liệu. Kiểm tra kết nối database rồi thử lại.";
        }
        catch (DbUpdateException)
        {
            TempData["ExtractionMessage"] = "Không xóa được dữ liệu do ràng buộc khóa ngoại.";
        }
        return RedirectToAction(nameof(DuLieu));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDuLieu(long id, CancellationToken ct)
    {
        try
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            await _context.LichSuXuLyDuLieus.Where(x => x.MaDuLieu == id).ExecuteDeleteAsync(ct);
            await _context.LeadKhachHangNhuCaus.Where(x => x.MaDuLieu == id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.MaDuLieu, (long?)null), ct);
            var deleted = await _context.DuLieuThuThaps.Where(x => x.MaDuLieu == id).ExecuteDeleteAsync(ct);
            await tx.CommitAsync(ct);

            if (deleted > 0 && !await _context.DuLieuThuThaps.AnyAsync(ct))
            {
                var provider = _context.Database.ProviderName ?? "";
                if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
                    await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('DuLieuThuThap', RESEED, 0)", ct);
                else
                    await _context.Database.ExecuteSqlRawAsync("ALTER TABLE `DuLieuThuThap` AUTO_INCREMENT = 1", ct);
            }

            TempData["ExtractionMessage"] = deleted > 0
                ? $"Đã xóa bài #{id}."
                : $"Không tìm thấy bài #{id} để xóa.";
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            TempData["ExtractionMessage"] = "Không xóa được dữ liệu. Kiểm tra kết nối database rồi thử lại.";
        }
        catch (DbUpdateException)
        {
            TempData["ExtractionMessage"] = "Không xóa được bài viết do ràng buộc khóa ngoại.";
        }
        return RedirectToAction(nameof(DuLieu));
    }

    public sealed record ResolveProvinceRequest(long Id, string? Address);

    private static string? DetectNgheAnHaTinh(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var norm = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(norm.Length);
        foreach (var ch in norm)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch == 'đ' || ch == 'Đ' ? 'd' : char.ToLowerInvariant(ch));
        }
        var s = sb.ToString();
        if (s.Contains("nghe an") || s.Contains("vinh,")) return "Nghệ An";
        if (s.Contains("ha tinh")) return "Hà Tĩnh";
        return null;
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResolveProvince([FromBody] ResolveProvinceRequest req,
        [FromServices] IConfiguration config, [FromServices] IHttpClientFactory httpFactory,
        [FromServices] IMemoryCache cache, CancellationToken ct)
    {
        var address = (req?.Address ?? "").Trim();
        if (string.IsNullOrWhiteSpace(address) || address == "—")
            return Json(new { province = (string?)null });

        var cacheKey = "geo-province:" + address.ToLowerInvariant();
        if (!cache.TryGetValue<string?>(cacheKey, out var province))
        {
            province = DetectNgheAnHaTinh(address);
            var http = httpFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(10);

            if (province is null)
            {
                var apiKey = config["GoogleMaps:ApiKey"] ?? Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY");
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    try
                    {
                        var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address + ", Vietnam")}&language=vi&region=vn&key={Uri.EscapeDataString(apiKey)}";
                        using var res = await http.GetAsync(url, ct);
                        if (res.IsSuccessStatusCode)
                        {
                            using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                            if (doc.RootElement.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var item in results.EnumerateArray())
                                {
                                    if (item.TryGetProperty("address_components", out var comps))
                                    {
                                        foreach (var c in comps.EnumerateArray())
                                        {
                                            var longName = c.TryGetProperty("long_name", out var ln) ? ln.GetString() : null;
                                            province = DetectNgheAnHaTinh(longName);
                                            if (province != null) break;
                                        }
                                    }
                                    if (province == null && item.TryGetProperty("formatted_address", out var fa))
                                        province = DetectNgheAnHaTinh(fa.GetString());
                                    if (province != null) break;
                                }
                            }
                        }
                    }
                    catch { /* Fallback bên dưới nếu Google Maps lỗi */ }
                }

                if (province is null)
                {
                    try
                    {
                        using var nomReq = new HttpRequestMessage(HttpMethod.Get,
                            $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address + ", Vietnam")}&format=json&addressdetails=1&countrycodes=vn&limit=1");
                        nomReq.Headers.UserAgent.ParseAdd("NhaTot-SocialBot/1.0");
                        using var nomRes = await http.SendAsync(nomReq, ct);
                        if (nomRes.IsSuccessStatusCode)
                        {
                            using var doc = await JsonDocument.ParseAsync(await nomRes.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                            {
                                var first = doc.RootElement[0];
                                if (first.TryGetProperty("display_name", out var dn))
                                    province = DetectNgheAnHaTinh(dn.GetString());
                            }
                        }
                    }
                    catch { /* Bỏ qua nếu không gọi được geocoder */ }
                }
            }

            cache.Set(cacheKey, province, TimeSpan.FromHours(12));
        }

        if (province != null && req!.Id > 0)
        {
            await _context.DuLieuThuThaps
                .Where(x => x.MaDuLieu == req.Id && (x.TinhNguon == null || x.TinhNguon == ""))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TinhNguon, province), ct);
        }

        return Json(new { province });
    }
}
