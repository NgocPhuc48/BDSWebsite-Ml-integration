using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Services;

public sealed class CrawlImportService(NhaTotDbContext context, CrawlExtractionService extraction)
{
    private static string Text(JsonElement item, string key) =>
        item.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";

    private static string FirstText(JsonElement item, params string[] keys)
    {
        foreach (var key in keys)
        {
            var val = Text(item, key).Trim();
            if (val.Length > 0) return val;
        }
        return "";
    }

    public static string CanonicalUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme != "https" || (uri.Host != "facebook.com" && uri.Host != "www.facebook.com" && uri.Host != "m.facebook.com")) return "";
        // profile.php dùng query id làm định danh; không gộp các profile khác nhau.
        var identityQuery = uri.AbsolutePath.Equals("/profile.php", StringComparison.OrdinalIgnoreCase)
            ? Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query).TryGetValue("id", out var id)
                ? "?id=" + Uri.EscapeDataString(id.ToString()) : uri.Query
            : "";
        return "https://www.facebook.com" + uri.AbsolutePath.TrimEnd('/') + identityQuery;
    }

    public async Task<(int Saved, int Duplicate, int Skipped)> ImportAsync(JsonElement items,
        IReadOnlyDictionary<string, int> sources, string runId, CancellationToken ct)
    {
        var saved = 0; var duplicate = 0; var skipped = 0;
        var locations = await extraction.LoadLocationsAsync(ct);
        // Serializable chống hai phiên cùng kiểm tra và chèn một bài đồng thời.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var seenIds = new HashSet<string>(); var seenUrls = new HashSet<string>(); var seenHashes = new HashSet<string>();
        foreach (var item in items.EnumerateArray())
        {
            if (Text(item, "type") != "post") continue;
            var id = Text(item, "id");
            var url = Text(item, "permalink");
            if (url.Length == 0) url = Text(item, "url");
            var text = Text(item, "text");
            var sourceUrl = CanonicalUrl(Text(item, "input"));
            if (!sources.TryGetValue(sourceUrl, out var sourceId) || id.Length > 200 || url.Length > 1500
                || (id.Length == 0 && url.Length == 0) || string.IsNullOrWhiteSpace(text))
            { skipped++; continue; }
            if (!await context.NguonDuLieus.AnyAsync(x => x.MaNguon == sourceId, ct)) { skipped++; continue; }
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.Normalize().Trim())));
            if ((id.Length > 0 && seenIds.Contains(id)) || (url.Length > 0 && seenUrls.Contains(url)) || seenHashes.Contains(hash)
                || await context.DuLieuThuThaps.AnyAsync(x => (id != "" && x.MaTinNguon == id)
                    || (url != "" && x.Urltin == url) || x.HashNoiDung == hash, ct))
            { duplicate++; continue; }
            var entity = new DuLieuThuThap
            {
                MaNguon = sourceId, MaTinNguon = id.Length > 0 ? id : null, Urltin = url.Length > 0 ? url : null,
                TieuDe = text.Length > 500 ? text[..500] : text,
                NoiDungGoc = text, DuLieuGocJson = item.GetRawText(), HashNoiDung = hash,
                NgayDangNguon = DateTimeOffset.TryParse(Text(item, "postedAt"), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var posted) ? posted.UtcDateTime : null,
                NgayThuThap = DateTime.UtcNow, TrangThaiXuLy = "NEW"
            };
            entity.LichSuXuLyDuLieus.Add(new LichSuXuLyDuLieu
            {
                BuocXuLy = "CRAWL_IMPORT", KetQua = "SUCCESS", ThoiDiem = DateTime.UtcNow,
                ChiTiet = $"Lưu bài gốc từ Apify run {runId}; chờ bóc tách thông tin."
            });
            CrawlExtractionService.Apply(entity, locations.Provinces, locations.Wards);
            context.DuLieuThuThaps.Add(entity);
            seenIds.Add(id); seenUrls.Add(url); seenHashes.Add(hash); saved++;
        }
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return (saved, duplicate, skipped);
    }

    public async Task<(int Saved, int Duplicate, int Skipped)> ImportKeywordPostsAsync(
        JsonElement items, string keyword, string? runId, CancellationToken ct)
    {
        var saved = 0; var duplicate = 0; var skipped = 0;
        if (items.ValueKind != JsonValueKind.Array) return (0, 0, 0);

        var cleanKeyword = string.IsNullOrWhiteSpace(keyword) ? "Facebook" : keyword.Trim();

        var locations = await extraction.LoadLocationsAsync(ct);
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var seenIds = new HashSet<string>(); var seenUrls = new HashSet<string>(); var seenHashes = new HashSet<string>();

        foreach (var item in items.EnumerateArray())
        {
            var text = FirstText(item, "text", "postText", "content", "description", "message");
            if (string.IsNullOrWhiteSpace(text)) { skipped++; continue; }

            var id = FirstText(item, "postId", "post_id", "id");
            var url = FirstText(item, "postUrl", "post_url", "postLink", "post_link",
                "permalink", "permalinkUrl", "permalink_url", "webUrl", "url", "link", "facebookUrl", "fbUrl", "shareUrl", "topLevelUrl", "postPermalink");
            if (url.Length == 0 && id.Length > 0)
            {
                var authorId = FirstText(item, "author_user_id", "authorUserId", "authorId", "userId");
                url = authorId.Length > 0
                    ? $"https://www.facebook.com/{authorId}/posts/{id}"
                    : $"https://www.facebook.com/{id}";
            }
            if (id.Length > 200) id = id[..200];
            if (url.Length > 1500) url = url[..1500];

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.Normalize().Trim())));
            if ((id.Length > 0 && seenIds.Contains(id)) || (url.Length > 0 && seenUrls.Contains(url)) || seenHashes.Contains(hash)
                || await context.DuLieuThuThaps.AnyAsync(x => (id != "" && x.MaTinNguon == id)
                    || (url != "" && x.Urltin == url) || x.HashNoiDung == hash, ct))
            { duplicate++; continue; }

            var rawDate = FirstText(item, "postedAt", "createTime", "createdAt", "time");
            DateTime? postedAt = DateTimeOffset.TryParse(rawDate, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var parsed) ? parsed.UtcDateTime : null;

            var cleanTitle = RealEstateTextExtractor.CleanIcons(text);
            if (string.IsNullOrWhiteSpace(cleanTitle)) cleanTitle = text.Trim();

            var entity = new DuLieuThuThap
            {
                MaNguon = null,
                MaTinNguon = id.Length > 0 ? id : null,
                Urltin = url.Length > 0 ? url : null,
                TieuDe = cleanTitle.Length > 500 ? cleanTitle[..500] : cleanTitle,
                NoiDungGoc = text,
                DuLieuGocJson = item.GetRawText(),
                HashNoiDung = hash,
                NgayDangNguon = postedAt,
                NgayThuThap = DateTime.UtcNow,
                TrangThaiXuLy = "NEW"
            };
            entity.LichSuXuLyDuLieus.Add(new LichSuXuLyDuLieu
            {
                BuocXuLy = "CRAWL_IMPORT",
                KetQua = "SUCCESS",
                ThoiDiem = DateTime.UtcNow,
                ChiTiet = $"Lưu nóng từ Tool Cào Dữ Liệu (từ khóa: {cleanKeyword}, run: {runId ?? "N/A"})."
            });
            CrawlExtractionService.Apply(entity, locations.Provinces, locations.Wards);
            if (cleanKeyword.Contains("thuê", StringComparison.OrdinalIgnoreCase) || cleanKeyword.Contains("thue", StringComparison.OrdinalIgnoreCase))
            {
                entity.LoaiGiaoDichDaDuyet ??= "CHO_THUE";
            }
            if (string.IsNullOrWhiteSpace(entity.TinhNguon))
            {
                var groupMeta = $"{FirstText(item, "groupName", "inputUrl")} {cleanKeyword}";
                if (groupMeta.Contains("Hà Tĩnh", StringComparison.OrdinalIgnoreCase) || groupMeta.Contains("Ha Tinh", StringComparison.OrdinalIgnoreCase))
                    entity.TinhNguon = "Hà Tĩnh";
                else if (groupMeta.Contains("Nghệ An", StringComparison.OrdinalIgnoreCase) || groupMeta.Contains("Nghe An", StringComparison.OrdinalIgnoreCase))
                    entity.TinhNguon = "Nghệ An";
            }
            context.DuLieuThuThaps.Add(entity);
            if (id.Length > 0) seenIds.Add(id);
            if (url.Length > 0) seenUrls.Add(url);
            seenHashes.Add(hash);
            saved++;
        }

        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return (saved, duplicate, skipped);
    }
}
