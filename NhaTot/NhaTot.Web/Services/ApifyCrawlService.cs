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
            throw new InvalidOperationException($"Apify trả lỗi HTTP {(int)response.StatusCode}. Kiểm tra token, quyền sử dụng actor và hạn mức.");
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
}
