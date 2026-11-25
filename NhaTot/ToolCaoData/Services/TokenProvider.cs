namespace ToolCaoData.Services;

/// <summary>
/// Đọc danh sách Apify token ở phía SERVER (api.txt hoặc biến môi trường APIFY_TOKENS).
/// Token không bao giờ được gửi xuống trình duyệt, khác với bản React (import api.txt?raw).
/// </summary>
public class TokenProvider
{
    public IReadOnlyList<string> Tokens { get; }
    public string? LoadError { get; }

    public TokenProvider(IConfiguration config, IWebHostEnvironment env, ILogger<TokenProvider> logger)
    {
        var list = new List<string>();
        try
        {
            var file = Path.Combine(env.ContentRootPath, config["TokenFile"] ?? "api.txt");
            if (File.Exists(file))
            {
                list.AddRange(File.ReadAllLines(file)
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 0 && !l.StartsWith('#')));
            }

            var envTokens = Environment.GetEnvironmentVariable("APIFY_TOKENS");
            if (!string.IsNullOrWhiteSpace(envTokens))
            {
                list.AddRange(envTokens.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            if (list.Count == 0) LoadError = "Tệp api.txt trống, không tồn tại hoặc không hợp lệ.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Không đọc được token");
            LoadError = "Không thể đọc dữ liệu từ api.txt.";
        }

        Tokens = list.Distinct().ToList();
    }
}
