using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Services;

public sealed class ApifyTokenStore(
    NhaTotDbContext db,
    IDataProtectionProvider protection,
    IConfiguration configuration,
    IWebHostEnvironment env)
{
    private readonly IDataProtector protector = protection.CreateProtector("NhaTot.SocialBot.ApifyToken.v1");

    private IReadOnlyList<string> LoadTokensFromFile()
    {
        var candidates = new[]
        {
            Path.Combine(env.ContentRootPath, "api.txt"),
            Path.Combine(env.ContentRootPath, "..", "ToolCaoData", "api.txt")
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;
            var tokens = File.ReadAllLines(path)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.StartsWith('#'))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            if (tokens.Count > 0) return tokens;
        }
        return [];
    }

    public async Task<IReadOnlyList<string>> GetAllAsync(CancellationToken ct = default)
    {
        var result = new List<string>();
        var encrypted = await db.LichCaoDuLieus.AsNoTracking()
            .Where(x => x.Id == 1)
            .Select(x => x.EncryptedApifyToken)
            .SingleOrDefaultAsync(ct);

        if (!string.IsNullOrWhiteSpace(encrypted))
        {
            try
            {
                var unprotected = protector.Unprotect(encrypted)?.Trim();
                if (!string.IsNullOrWhiteSpace(unprotected))
                    result.Add(unprotected);
            }
            catch (CryptographicException)
            {
                // Khi chạy trên máy khác chưa có khóa DPAPI cũ, tự động fallback sang api.txt
            }
        }

        var cfgToken = configuration["Apify:Token"]?.Trim();
        if (!string.IsNullOrWhiteSpace(cfgToken))
            result.Add(cfgToken);

        foreach (var fileToken in LoadTokensFromFile())
        {
            if (!result.Contains(fileToken, StringComparer.Ordinal))
                result.Add(fileToken);
        }

        return result;
    }

    public async Task<string?> GetAsync(CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.Count > 0 ? all[0] : null;
    }

    public async Task SaveAsync(string token, CancellationToken ct = default)
    {
        var row = await db.LichCaoDuLieus.SingleAsync(x => x.Id == 1, ct);
        row.EncryptedApifyToken = protector.Protect(token);
        await db.SaveChangesAsync(ct);
    }
}
