using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace NhaTot.Web.Services;

public sealed class ApifyCrawlService(HttpClient client, ApifyTokenStore tokens)
{
    private async Task<JsonElement> SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var token = await tokens.GetAsync(ct);
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Chưa có token. Mở Admin → SocialBot → Kết nối Apify để nhập token.");
        using var request = new HttpRequestMessage(method, "https://api.apify.com/v2/" + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body != null) request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(ct);
            var detail = errBody.Length > 300 ? errBody[..300] : errBody;
            throw new InvalidOperationException($"Apify trả lỗi HTTP {(int)response.StatusCode}: {detail}");
        }
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return json.RootElement.Clone();
    }

    public async Task<JsonElement> StartAsync(string[] urls, int limit, int days, CancellationToken ct)
    {
        return (await SendAsync(HttpMethod.Post, "acts/apisamurai~facebook-scraper/runs", new
        {
            pageUrls = urls, includePosts = true, maxPostsPerPage = limit,
            postedAfter = $"{days} days", includeComments = false
        }, ct)).GetProperty("data");
    }

    public async Task<JsonElement> StatusAsync(string runId, CancellationToken ct) =>
        (await SendAsync(HttpMethod.Get, $"actor-runs/{Uri.EscapeDataString(runId)}", null, ct)).GetProperty("data");

    public async Task AbortAsync(string runId, CancellationToken ct) =>
        _ = await SendAsync(HttpMethod.Post, $"actor-runs/{Uri.EscapeDataString(runId)}/abort", null, ct);

    public Task<JsonElement> ResultsAsync(string datasetId, CancellationToken ct) =>
        SendAsync(HttpMethod.Get, $"datasets/{Uri.EscapeDataString(datasetId)}/items?clean=true&limit=1000", null, ct);

    public async Task<JsonElement> StartKeywordAsync(string keyword, string location, bool strictSearch, bool filterRecent,
            string timeInterval, int limit, CancellationToken ct)
    {
        var scopedKeyword = !string.IsNullOrWhiteSpace(location) && !keyword.Contains(location, StringComparison.OrdinalIgnoreCase)
            ? $"{keyword} {location}"
            : keyword;

        var payload = new Dictionary<string, object>
        {
            ["scraperMode"] = "facebookSearchScraper",
            ["function"] = "facebook_search_scraper",
            ["keyword"] = scopedKeyword,
            ["enableStrictSearch"] = strictSearch,
            ["filterByRecent"] = filterRecent,
            ["fetchPostsInTimeInterval"] = timeInterval,
            ["resultsLimit"] = limit,
            ["results_limit"] = limit,
            ["maxItems"] = limit,
            ["maxResults"] = limit,
            ["min_wait_time_in_sec"] = 1,
            ["max_wait_time_in_sec"] = 4
        };

        return (await SendAsync(HttpMethod.Post, "acts/alien_force~facebook-scraper-pro-rental/runs", payload, ct)).GetProperty("data");
    }

    public async Task<JsonElement> StartGroupSearchAsync(
        string[] searchQueries,
        int groupsPerSearch,
        int minMembers,
        int maxPostsPerGroup,
        string? onlyPostsNewerThan,
        bool newestFirst,
        bool includeComments,
        bool includeReplies,
        CancellationToken ct)
    {
        var payload = new Dictionary<string, object>
        {
            ["searchQueries"] = searchQueries,
            ["maxResultsPerQuery"] = groupsPerSearch,
            ["maxPosts"] = maxPostsPerGroup,
            ["viewOption"] = newestFirst ? "CHRONOLOGICAL" : "RECENT_ACTIVITY",
            ["includeComments"] = includeComments,
            ["maxComments"] = 10,
            ["commentsSortBy"] = "newest",
            ["includeReplies"] = includeComments && includeReplies,
            ["maxRepliesPerComment"] = 5,
            ["replyDepth"] = "1"
        };
        if (minMembers > 0)
            payload["minMembers"] = minMembers;
        if (!string.IsNullOrWhiteSpace(onlyPostsNewerThan))
            payload["onlyPostsNewerThan"] = onlyPostsNewerThan;

        return (await SendAsync(HttpMethod.Post, "acts/calm_builder~facebook-groups-scraper/runs", payload, ct)).GetProperty("data");
    }
}
