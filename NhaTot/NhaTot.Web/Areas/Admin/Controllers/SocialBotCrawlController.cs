using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NhaTot.Web.Services;
using System.Security.Claims;

namespace NhaTot.Web.Areas.Admin.Controllers;

public partial class SocialBotController
{
    private string CrawlOwner => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? throw new InvalidOperationException("Không xác định được tài khoản.");

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StartCrawl([FromServices] ApifyCrawlService service,
        [FromServices] IMemoryCache cache, int limit = 10, int days = 1)
    {
        if (limit < 1 || limit > 100 || (days != 1 && days != 7 && days != 30))
            return BadRequest(new { message = "Bộ lọc không hợp lệ." });
        var sources = await _context.NguonDuLieus.Where(n => n.TrangThai == "ACTIVE")
            .Select(n => new { n.MaNguon, n.DiaChiWebsite }).ToListAsync();
        var sourceMap = sources.Where(s => Uri.TryCreate(s.DiaChiWebsite, UriKind.Absolute, out var uri)
                && uri.Scheme == "https" && (uri.Host == "facebook.com" || uri.Host == "www.facebook.com" || uri.Host == "m.facebook.com"))
            .GroupBy(s => CrawlImportService.CanonicalUrl(s.DiaChiWebsite!))
            .ToDictionary(g => g.Key, g => g.First().MaNguon);
        var urls = sourceMap.Keys.ToArray();
        if (urls.Length == 0 || urls.Length > 10)
            return BadRequest(new { message = "Cần từ 1 đến 10 nguồn Facebook công khai đang bật. Hãy cấu hình danh sách nguồn bên dưới." });
        try
        {
            // Không gắn RequestAborted khi tạo run: cần nhận được ID để quản lý run đã tạo.
            var run = await service.StartAsync(urls, limit, days, CancellationToken.None);
            var id = run.GetProperty("id").GetString()!;
            cache.Set("crawl:" + id, CrawlOwner, TimeSpan.FromHours(24));
            cache.Set("crawl-sources:" + id, sourceMap, TimeSpan.FromHours(24));
            return Json(new { runId = id, sourceCount = urls.Length });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (HttpRequestException) { return StatusCode(502, new { message = "Không kết nối được Apify." }); }
        catch (TaskCanceledException) { return StatusCode(504, new { message = "Apify phản hồi quá lâu. Kiểm tra Apify Console trước khi chạy lại." }); }
    }

    [HttpGet]
    public async Task<IActionResult> CrawlStatus(string runId, [FromServices] ApifyCrawlService service, [FromServices] IMemoryCache cache)
    {
        if (string.IsNullOrWhiteSpace(runId) || !cache.TryGetValue<string>("crawl:" + runId, out var owner) || owner != CrawlOwner) return NotFound();
        try
        {
            var run = await service.StatusAsync(runId, HttpContext.RequestAborted);
            var status = run.GetProperty("status").GetString();
            object? items = null;
            if (status is "SUCCEEDED" or "ABORTED")
                items = await service.ResultsAsync(run.GetProperty("defaultDatasetId").GetString()!, HttpContext.RequestAborted);
            return Json(new { status, items });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (HttpRequestException) { return StatusCode(502, new { message = "Không kết nối được Apify." }); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCrawl(string runId, [FromServices] ApifyCrawlService service,
        [FromServices] CrawlImportService importer, [FromServices] IMemoryCache cache)
    {
        if (string.IsNullOrWhiteSpace(runId) || !cache.TryGetValue<string>("crawl:" + runId, out var owner) || owner != CrawlOwner)
            return NotFound(new { message = "Phiên đã hết hạn hoặc không thuộc tài khoản này." });
        if (!cache.TryGetValue<Dictionary<string, int>>("crawl-sources:" + runId, out var sources) || sources == null)
            return BadRequest(new { message = "Không tìm thấy nguồn của phiên. Hãy chạy phiên mới." });
        try
        {
            var run = await service.StatusAsync(runId, HttpContext.RequestAborted);
            var status = run.GetProperty("status").GetString();
            if (status is not ("SUCCEEDED" or "ABORTED"))
                return BadRequest(new { message = "Chỉ lưu được phiên đã hoàn tất hoặc đã dừng." });
            var items = await service.ResultsAsync(run.GetProperty("defaultDatasetId").GetString()!, HttpContext.RequestAborted);
            var result = await importer.ImportAsync(items, sources, runId, HttpContext.RequestAborted);
            return Json(new { saved = result.Saved, duplicate = result.Duplicate, skipped = result.Skipped });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (HttpRequestException) { return StatusCode(502, new { message = "Không tải được kết quả từ Apify." }); }
        catch (Microsoft.Data.SqlClient.SqlException) { return StatusCode(503, new { message = "Không lưu được vào database. Kiểm tra kết nối hoặc thử lại khi database hết bận." }); }
        catch (DbUpdateException) { return StatusCode(503, new { message = "Không lưu được dữ liệu. Kiểm tra schema database rồi thử lại." }); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StopCrawl(string runId, [FromServices] ApifyCrawlService service, [FromServices] IMemoryCache cache)
    {
        if (string.IsNullOrWhiteSpace(runId) || !cache.TryGetValue<string>("crawl:" + runId, out var owner) || owner != CrawlOwner) return NotFound();
        try { await service.AbortAsync(runId, CancellationToken.None); return Json(new { success = true }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (HttpRequestException) { return StatusCode(502, new { message = "Không gửi được lệnh dừng. Kiểm tra Apify Console." }); }
    }
}
