using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ToolCaoData.Models;

namespace ToolCaoData.Services;

public record CrawlOptions(string Keyword, bool StrictSearch, bool FilterRecent, string TimeInterval, int TargetItems);

public enum CrawlStatus { Success, Stopped, Error }

public record CrawlResult(CrawlStatus Status, string Message, List<PostRecord> Records);

/// <summary>Port của startCrawling / abortApifyRun / normalizeData trong App.jsx.</summary>
public class ApifyService
{
    private const string ActorId = "alien_force~facebook-scraper-pro-rental";
    private const string ApiBase = "https://api.apify.com/v2";

    private readonly HttpClient _http;
    private readonly ILogger<ApifyService> _log;
    private readonly TimeZoneInfo _tz;

    public ApifyService(HttpClient http, IConfiguration config, ILogger<ApifyService> log)
    {
        _http = http;
        _log = log;
        try { _tz = TimeZoneInfo.FindSystemTimeZoneById(config["TimeZone"] ?? "Asia/Ho_Chi_Minh"); }
        catch { _tz = TimeZoneInfo.Local; }
    }

    // ------------------------------------------------------------------ HTTP
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, HttpContent? content, CancellationToken ct)
    {
        // Dùng header Authorization thay vì ?token=... để token không lọt vào log / thông báo lỗi
        using var req = new HttpRequestMessage(method, url) { Content = content };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _http.SendAsync(req, ct);
    }

    /// <summary>Gửi lệnh ABORT tới Apify, thử tối đa 3 lần. Trả về trạng thái run hoặc null nếu thất bại.</summary>
    public async Task<string?> AbortRunAsync(string runId, string token)
    {
        string[] okStates = { "ABORTING", "ABORTED", "SUCCEEDED", "FAILED", "TIMED-OUT" };
        for (int i = 0; i < 3; i++)
        {
            try
            {
                using var res = await SendAsync(HttpMethod.Post, $"{ApiBase}/actor-runs/{runId}/abort", token, null, CancellationToken.None);
                if (res.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
                    var st = doc.RootElement.GetProperty("data").GetProperty("status").GetString();
                    if (st != null && okStates.Contains(st)) return st;
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning("Lỗi gửi lệnh abort, thử lại... {Message}", ex.Message);
            }
            await Task.Delay(1000);
        }
        return null;
    }

    private async Task<CrawlResult> StoppedAsync(string? runId, string? token)
    {
        string? abortResult = null;
        if (runId != null && token != null) abortResult = await AbortRunAsync(runId, token);

        var msg = runId == null ? "Đã dừng trước khi actor được khởi chạy."
            : abortResult != null ? $"Đã dừng và gửi lệnh ABORT tới actor thành công (trạng thái: {abortResult})."
            : "Đã dừng nhưng KHÔNG gửi được lệnh abort tới Apify. Hãy vào Apify Console dừng run thủ công.";
        return new CrawlResult(CrawlStatus.Stopped, msg, new());
    }

    // ----------------------------------------------------------------- Crawl
    public async Task<CrawlResult> CrawlAsync(
        CrawlOptions opt, IReadOnlyList<string> tokens, int startTokenIndex,
        Action<string> onStatus, Action<int> onTokenChanged, CancellationToken ct)
    {
        int idx = startTokenIndex;

        while (idx < tokens.Count)
        {
            if (ct.IsCancellationRequested) return await StoppedAsync(null, null);

            var token = tokens[idx];
            onTokenChanged(idx);
            string? runId = null;

            try
            {
                var payload = new Dictionary<string, object>
                {
                    ["scraperMode"] = "facebookSearchScraper",
                    ["keyword"] = opt.Keyword,
                    ["enableStrictSearch"] = opt.StrictSearch,
                    ["filterByRecent"] = opt.FilterRecent,
                    ["fetchPostsInTimeInterval"] = opt.TimeInterval,
                    ["resultsLimit"] = opt.TargetItems,
                    ["maxItems"] = opt.TargetItems,
                    ["maxResults"] = opt.TargetItems,
                    ["function"] = "facebook_search_scraper",
                };
                using var body = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                // Không truyền ct ở đây: nếu huỷ giữa chừng, run có thể đã được tạo trên Apify
                // mà ta chưa biết runId để abort -> actor chạy ngầm.
                using var startRes = await SendAsync(HttpMethod.Post, $"{ApiBase}/acts/{ActorId}/runs", token, body, CancellationToken.None);
                var startJson = await startRes.Content.ReadAsStringAsync(CancellationToken.None);
                if (!startRes.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Token lỗi hoặc hết hạn: {ExtractApifyError(startJson) ?? ((int)startRes.StatusCode).ToString()}");
                }

                using (var startDoc = JsonDocument.Parse(startJson))
                {
                    runId = startDoc.RootElement.GetProperty("data").GetProperty("id").GetString();
                }

                // Người dùng bấm dừng đúng lúc đang khởi tạo run
                if (ct.IsCancellationRequested) return await StoppedAsync(runId, token);

                string runStatus = "RUNNING";
                string? datasetId = null;

                while (runStatus is "RUNNING" or "READY")
                {
                    await Task.Delay(4000, ct);

                    using var checkRes = await SendAsync(HttpMethod.Get, $"{ApiBase}/actor-runs/{runId}", token, null, ct);
                    if (!checkRes.IsSuccessStatusCode) throw new InvalidOperationException("Lỗi mạng khi kiểm tra trạng thái tiến trình.");

                    using var checkDoc = JsonDocument.Parse(await checkRes.Content.ReadAsStringAsync(ct));
                    var data = checkDoc.RootElement.GetProperty("data");
                    runStatus = data.GetProperty("status").GetString() ?? "";
                    datasetId = data.TryGetProperty("defaultDatasetId", out var ds) ? ds.GetString() : null;
                    onStatus($"Trạng thái tiến trình: {runStatus}...");
                }

                if (runStatus == "SUCCEEDED" && !string.IsNullOrEmpty(datasetId))
                {
                    onStatus("Đang tải dữ liệu kết quả...");
                    using var dsRes = await SendAsync(HttpMethod.Get, $"{ApiBase}/datasets/{datasetId}/items", token, null, ct);
                    using var dsDoc = JsonDocument.Parse(await dsRes.Content.ReadAsStringAsync(ct));
                    var records = NormalizeData(dsDoc.RootElement);
                    return new CrawlResult(CrawlStatus.Success, $"Hoàn tất! Đã thu thập thành công {records.Count} bản ghi.", records);
                }

                throw new InvalidOperationException($"Tiến trình cào thất bại với trạng thái: {runStatus}");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return await StoppedAsync(runId, token);
            }
            catch (Exception ex)
            {
                _log.LogWarning("Lỗi tại Token #{N}: {Message}", idx + 1, ex.Message);
                idx++;
                if (idx >= tokens.Count)
                {
                    return new CrawlResult(CrawlStatus.Error, "Đã thử hết tất cả Token trong danh sách nhưng đều báo lỗi hoặc hết hạn.", new());
                }

                onTokenChanged(idx);
                onStatus($"Token #{idx} lỗi. Đang tự động đổi sang Token #{idx + 1}...");
                try { await Task.Delay(2000, ct); }
                catch (OperationCanceledException) { return await StoppedAsync(null, null); }
            }
        }

        return new CrawlResult(CrawlStatus.Error, "Không thể hoàn thành việc cào dữ liệu.", new());
    }

    private static string? ExtractApifyError(string json)
    {
        try
        {
            using var d = JsonDocument.Parse(json);
            return d.RootElement.GetProperty("error").GetProperty("message").GetString();
        }
        catch { return null; }
    }

    // ------------------------------------------------------ Chuẩn hoá dữ liệu
    private record Flat(string Path, string Key, int Depth, JsonElement Value);

    private static readonly Regex OwnerPath = new("author|user|page|profile|owner|avatar|group", RegexOptions.IgnoreCase);
    private static readonly Regex LooseTime = new("time|date|created");
    private static readonly Regex LooseTimeExclude = new("duration|timezone|update|author|user|profile");
    private static readonly Regex FbLink = new(@"^https?://(www\.|m\.|web\.)?(facebook\.com|fb\.com|fb\.watch)", RegexOptions.IgnoreCase);
    private static readonly Regex NotPostPath = new("author|user|profile|avatar|image|photo|video|cover|thumb|picture|icon", RegexOptions.IgnoreCase);

    private static readonly string[] TimeKeys =
    {
        "createtime", "createdtime", "creationtime", "createdat", "createdon", "createdate", "created",
        "posttime", "postdate", "postedat", "postedtime", "publishtime", "publishedat", "publisheddate",
        "publishedtime", "timestamp", "time", "date", "datetime", "utime", "creationdate",
    };
    private static readonly string[] UrlKeys =
    {
        "posturl", "postlink", "permalink", "permalinkurl", "weburl", "url", "link",
        "facebookurl", "fburl", "shareurl", "toplevelurl", "postpermalink",
    };

    private static string NormKey(string k) => Regex.Replace(k.ToLowerInvariant(), "[^a-z0-9]", "");

    private static List<Flat> Flatten(JsonElement obj, string prefix = "", int depth = 0, List<Flat>? output = null)
    {
        output ??= new();
        if (obj.ValueKind != JsonValueKind.Object) return output;
        foreach (var p in obj.EnumerateObject())
        {
            var path = prefix.Length > 0 ? $"{prefix}.{p.Name}" : p.Name;
            if (p.Value.ValueKind == JsonValueKind.Object && depth < 3)
                Flatten(p.Value, path, depth + 1, output);
            else
                output.Add(new Flat(path, NormKey(p.Name), depth, p.Value));
        }
        return depth == 0 ? output.OrderBy(e => e.Depth).ToList() : output; // OrderBy ổn định như Array.sort
    }

    private static bool IsEmpty(JsonElement v) =>
        v.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null or JsonValueKind.Object
        || (v.ValueKind == JsonValueKind.String && v.GetString() == "");

    // Giống "truthy" của JavaScript (dùng cho chuỗi item.a || item.b || ...)
    private static bool Truthy(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.Undefined or JsonValueKind.Null or JsonValueKind.False => false,
        JsonValueKind.String => !string.IsNullOrEmpty(v.GetString()),
        JsonValueKind.Number => v.GetDouble() != 0,
        _ => true,
    };

    private static JsonElement? First(JsonElement item, params string[] names)
    {
        if (item.ValueKind != JsonValueKind.Object) return null;
        foreach (var n in names)
            if (item.TryGetProperty(n, out var v) && Truthy(v)) return v;
        return null;
    }

    private static string Str(JsonElement? v) => v switch
    {
        null => "",
        { ValueKind: JsonValueKind.String } s => s.GetString() ?? "",
        { } x => x.ToString(),
    };

    private static long Num(JsonElement? v)
    {
        if (v is not { } e) return 0;
        return e.ValueKind switch
        {
            JsonValueKind.Number => e.TryGetInt64(out var l) ? l : (long)e.GetDouble(),
            JsonValueKind.String => long.TryParse(e.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : 0,
            JsonValueKind.Array => e.GetArrayLength(),
            _ => 0,
        };
    }

    private static int ArrayCount(JsonElement? v) => v is { ValueKind: JsonValueKind.Array } a ? a.GetArrayLength() : 0;

    private static string? NameOf(JsonElement? v)
    {
        if (v is not { } e) return null;
        if (e.ValueKind == JsonValueKind.String) return e.GetString();
        if (e.ValueKind == JsonValueKind.Object && e.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String) return n.GetString();
        return null;
    }

    private static JsonElement? FindTime(List<Flat> flat)
    {
        foreach (var cand in TimeKeys)
        {
            var hit = flat.FirstOrDefault(e => e.Key == cand && !OwnerPath.IsMatch(e.Path) && !IsEmpty(e.Value));
            if (hit != null) return hit.Value;
        }
        var loose = flat.FirstOrDefault(e =>
            LooseTime.IsMatch(e.Key) && !LooseTimeExclude.IsMatch(e.Key) &&
            !OwnerPath.IsMatch(e.Path) && !IsEmpty(e.Value) && e.Value.ValueKind != JsonValueKind.Array);
        return loose?.Value;
    }

    private static string? FindUrl(List<Flat> flat)
    {
        foreach (var cand in UrlKeys)
        {
            var hit = flat.FirstOrDefault(e => e.Key == cand && e.Value.ValueKind == JsonValueKind.String
                && (e.Value.GetString() ?? "").StartsWith("http") && !OwnerPath.IsMatch(e.Path));
            if (hit != null) return hit.Value.GetString();
        }
        var loose = flat.FirstOrDefault(e => e.Value.ValueKind == JsonValueKind.String
            && FbLink.IsMatch(e.Value.GetString() ?? "") && !NotPostPath.IsMatch(e.Path));
        return loose?.Value.GetString();
    }

    /// <summary>Port của formatDateTime: trả về "HH:mm:ss dd/MM/yyyy" theo múi giờ cấu hình.</summary>
    private string FormatDateTime(JsonElement? raw)
    {
        if (raw is not { } v || IsEmpty(v)) return "Không có";
        var s = Str(v);
        if (s == "Không có") return s;

        DateTimeOffset? date = null;
        if (v.ValueKind == JsonValueKind.Number || Regex.IsMatch(s, @"^\d+$"))
        {
            var num = v.ValueKind == JsonValueKind.Number ? v.GetDouble() : double.Parse(s, CultureInfo.InvariantCulture);
            try
            {
                date = num < 10_000_000_000 ? DateTimeOffset.FromUnixTimeSeconds((long)num) : DateTimeOffset.FromUnixTimeMilliseconds((long)num);
            }
            catch { /* ngoài phạm vi -> fallback */ }
        }
        else
        {
            var cleaned = Regex.Replace(s, @"\s*UTC$", "Z");
            if (DateTimeOffset.TryParse(cleaned, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces, out var d))
                date = d;
        }

        if (date != null)
            return TimeZoneInfo.ConvertTime(date.Value, _tz).ToString("HH:mm:ss dd/MM/yyyy", CultureInfo.InvariantCulture);

        return s.Replace(" UTC", "").Replace("T", " ").Replace("Z", "");
    }

    private List<PostRecord> NormalizeData(JsonElement root)
    {
        var list = new List<PostRecord>();
        if (root.ValueKind != JsonValueKind.Array) return list;

        int index = 0;
        foreach (var item in root.EnumerateArray())
        {
            var flat = Flatten(item);
            var rawTime = FindTime(flat);

            var realPostId = Str(First(item, "postId", "post_id", "id"));
            var postId = Str(First(item, "postId", "id", "post_id"));
            if (postId == "") postId = $"post_{index}";

            var authorUserId = Str(First(item, "author_user_id", "authorUserId", "userId"));

            var postUrl = FindUrl(flat);
            if (string.IsNullOrEmpty(postUrl) && realPostId != "")
            {
                postUrl = authorUserId != ""
                    ? $"https://www.facebook.com/{authorUserId}/posts/{realPostId}"
                    : $"https://www.facebook.com/{realPostId}";
            }

            string? userName = null;
            if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("user", out var u)
                && u.ValueKind == JsonValueKind.Object && u.TryGetProperty("name", out var un))
                userName = NameOf(un);

            var author = NameOf(First(item, "author"))
                         ?? userName
                         ?? NameOf(First(item, "pageName"))
                         ?? NameOf(First(item, "authorName"));
            if (string.IsNullOrEmpty(author)) author = "Tác giả không rõ";

            double duration = 0;
            var dur = First(item, "durationInSec", "duration");
            if (dur is { } d1)
            {
                if (d1.ValueKind == JsonValueKind.Number) duration = d1.GetDouble();
                else double.TryParse(Str(d1), NumberStyles.Any, CultureInfo.InvariantCulture, out duration);
            }

            var type = Str(First(item, "type", "postType"));

            list.Add(new PostRecord
            {
                PostId = postId,
                ActionId = Str(First(item, "actionId", "action_id")),
                Text = Str(First(item, "text", "postText", "content", "description", "message")),
                PostUrl = string.IsNullOrEmpty(postUrl) ? "#" : postUrl,
                CreateTime = FormatDateTime(rawTime),
                Author = author,
                AuthorUsername = Str(First(item, "author_username", "authorUsername", "username")),
                AuthorUserId = authorUserId,
                AuthorProfileUrl = Str(First(item, "author_profile_url", "authorProfileUrl", "userProfileUrl")) is { Length: > 0 } p ? p : "#",
                AuthorAvatar = Str(First(item, "author_avatar", "authorAvatar", "userAvatar")),
                Likes = Num(First(item, "like_count", "likesCount", "likes", "reactionsCount", "reaction_count")),
                Views = Num(First(item, "view_count", "viewsCount", "views")),
                Shares = Num(First(item, "share_count", "sharesCount", "shares")),
                Comments = Num(First(item, "comment_count", "commentsCount", "comments")),
                Type = type == "" ? "post" : type,
                ImageCount = ArrayCount(First(item, "image_list", "images", "photos")),
                VideoCount = ArrayCount(First(item, "video_list", "videos")),
                VideoCoverImage = Str(First(item, "video_cover_image", "videoCover")),
                DurationInSec = duration,
            });
            index++;
        }
        return list;
    }
}
