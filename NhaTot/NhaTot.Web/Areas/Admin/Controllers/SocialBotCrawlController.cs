using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NhaTot.Web.Services;
using System.Security.Claims;
using System.Text.Json;

namespace NhaTot.Web.Areas.Admin.Controllers;

public partial class SocialBotController
{
    [HttpGet]
    public IActionResult ToolCaoDuLieu() => View();

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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StartKeywordCrawl([FromServices] ApifyCrawlService service,
        [FromServices] IMemoryCache cache, string keyword, bool strictSearch, bool filterRecent,
        string timeInterval = "1d", int limit = 10, string region = "Nghệ An")
    {
        keyword = (keyword ?? "").Trim();
        if (keyword.Length < 2) return BadRequest(new { message = "Vui lòng chọn một từ khóa hợp lệ." });
        if (limit is < 1 or > 100) return BadRequest(new { message = "Số bài mục tiêu không hợp lệ." });
        var location = region?.Trim() == "Hà Tĩnh" ? "Hà Tĩnh" : "Nghệ An";
        // Chuẩn hóa từ khóa thực tế người đăng dùng trên Facebook (ví dụ "thuê bất động sản" -> "cho thuê nhà đất")
        var effectiveKeyword = keyword.ToLowerInvariant() switch
        {
            "thuê nhà" => "cho thuê nhà",
            "thuê đất" => "cho thuê đất mặt bằng",
            "thuê bất động sản" => "cho thuê nhà phòng trọ căn hộ",
            "mua bất động sản" => "mua bán nhà đất",
            _ => keyword
        };
        // Với từ khóa tổng hợp nhiều loại hình, tắt exact phrase để Facebook không đòi khớp từng chữ một
        var effectiveStrict = strictSearch && keyword.ToLowerInvariant() is not ("thuê bất động sản" or "thuê đất" or "mua bất động sản");
        var mappedInterval = timeInterval switch
        {
            "24h" or "1d" or "Since 24 Hours" => "Since 24 Hours",
            "7d" or "Past Week" => "Past Week",
            "30d" or "Past Month" => "Past Month",
            _ => "All Time"
        };

        try
        {
            var run = await service.StartKeywordAsync(effectiveKeyword, location, effectiveStrict, filterRecent, mappedInterval, limit, CancellationToken.None);
            var id = run.GetProperty("id").GetString()!;
            cache.Set("crawl:" + id, CrawlOwner, TimeSpan.FromHours(24));
            cache.Set("crawl-keyword:" + id, $"{keyword} ({location})", TimeSpan.FromHours(24));
            cache.Set("crawl-sources:" + id, new Dictionary<string, int>(), TimeSpan.FromHours(24));
            return Json(new { runId = id, keyword, effectiveKeyword, location, timeInterval = mappedInterval, resultsLimit = limit });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (HttpRequestException) { return StatusCode(502, new { message = "Không kết nối được Apify." }); }
        catch (TaskCanceledException) { return StatusCode(504, new { message = "Apify phản hồi quá lâu." }); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> StartGroupSearchCrawl(
        [FromServices] ApifyCrawlService service,
        [FromServices] IMemoryCache cache,
        string searchTerms,
        string region = "Nghệ An",
        int groupsPerSearch = 5,
        int minMembers = 1000,
        int maxPosts = 10,
        string onlyPostsNewerThan = "7 days",
        bool newestFirst = true,
        bool includeComments = true,
        bool includeReplies = true)
    {
        var location = region?.Trim() == "Hà Tĩnh" ? "Hà Tĩnh" : "Nghệ An";
        var rawTerms = (searchTerms ?? "")
            .Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();

        if (rawTerms.Length == 0)
            return BadRequest(new { message = "Vui lòng nhập hoặc chọn ít nhất 1 từ khóa chủ đề." });
        if (groupsPerSearch is < 1 or > 50)
            return BadRequest(new { message = "Số Group tối đa mỗi từ khóa phải từ 1 đến 50." });
        if (minMembers < 0)
            return BadRequest(new { message = "Số thành viên tối thiểu không hợp lệ." });
        if (maxPosts is < 1 or > 100)
            return BadRequest(new { message = "Số bài tối đa mỗi Group phải từ 1 đến 100." });

        var allowedNewerThan = onlyPostsNewerThan switch
        {
            "1 day" or "7 days" or "2 weeks" or "30 days" => onlyPostsNewerThan,
            _ => ""
        };

        var scopedQueries = rawTerms
            .SelectMany(t =>
            {
                var lower = t.ToLowerInvariant();
                if (lower == "thuê nhà")
                    return location == "Nghệ An"
                        ? new[] { "cho thuê nhà Vinh Nghệ An", "phòng trọ nhà trọ TP Vinh" }
                        : new[] { "cho thuê nhà Hà Tĩnh", "phòng trọ nhà trọ Hà Tĩnh" };
                if (lower == "thuê đất")
                    return new[] { $"cho thuê mặt bằng {location}", $"cho thuê nhà đất {location}" };
                if (lower == "thuê bất động sản")
                    return location == "Nghệ An"
                        ? new[] { "cho thuê nhà đất Vinh Nghệ An", "cho thuê mặt bằng căn hộ TP Vinh" }
                        : new[] { "cho thuê nhà đất Hà Tĩnh", "cho thuê mặt bằng phòng trọ Hà Tĩnh" };
                if (lower == "mua bất động sản")
                    return new[] { $"mua bán nhà đất {location}", $"bất động sản {location}" };
                return new[] { t.Contains(location, StringComparison.OrdinalIgnoreCase) ? t : $"{t} {location}" };
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();

        try
        {
            var run = await service.StartGroupSearchAsync(
                scopedQueries, groupsPerSearch, minMembers, maxPosts,
                allowedNewerThan, newestFirst, includeComments, includeReplies,
                CancellationToken.None);
            var id = run.GetProperty("id").GetString()!;
            var label = $"Group: {string.Join(", ", rawTerms)} ({location})";
            cache.Set("crawl:" + id, CrawlOwner, TimeSpan.FromHours(24));
            cache.Set("crawl-keyword:" + id, label, TimeSpan.FromHours(24));
            cache.Set("crawl-sources:" + id, new Dictionary<string, int>(), TimeSpan.FromHours(24));
            return Json(new { runId = id, queries = scopedQueries, location, groupsPerSearch, minMembers, maxPosts });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (HttpRequestException) { return StatusCode(502, new { message = "Không kết nối được Apify." }); }
        catch (TaskCanceledException) { return StatusCode(504, new { message = "Apify phản hồi quá lâu." }); }
    }

    public sealed record SaveKeywordPostsRequest(string? Keyword, string? RunId, JsonElement Items);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveKeywordPosts([FromBody] SaveKeywordPostsRequest payload,
        [FromServices] CrawlImportService importer, [FromServices] IMemoryCache cache)
    {
        if (payload is null || payload.Items.ValueKind != JsonValueKind.Array || payload.Items.GetArrayLength() == 0)
            return BadRequest(new { message = "Không có bài viết hợp lệ để lưu." });

        var keyword = payload.Keyword;
        if (string.IsNullOrWhiteSpace(keyword) && !string.IsNullOrWhiteSpace(payload.RunId))
            cache.TryGetValue("crawl-keyword:" + payload.RunId, out keyword);

        try
        {
            var result = await importer.ImportKeywordPostsAsync(payload.Items, keyword ?? "Facebook", payload.RunId, HttpContext.RequestAborted);
            return Json(new { saved = result.Saved, duplicate = result.Duplicate, skipped = result.Skipped });
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            return StatusCode(503, new { message = "Không lưu được vào database. Kiểm tra kết nối hoặc thử lại." });
        }
        catch (DbUpdateException)
        {
            return StatusCode(503, new { message = "Không lưu được dữ liệu. Kiểm tra schema database rồi thử lại." });
        }
    }
}
