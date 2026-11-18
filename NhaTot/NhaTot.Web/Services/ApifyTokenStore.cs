using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Services;

public sealed class ApifyTokenStore(NhaTotDbContext db, IDataProtectionProvider protection, IConfiguration configuration)
{
    private readonly IDataProtector protector = protection.CreateProtector("NhaTot.SocialBot.ApifyToken.v1");

    public async Task<string?> GetAsync(CancellationToken ct = default)
    {
        var encrypted = await db.LichCaoDuLieus.AsNoTracking().Where(x => x.Id == 1).Select(x => x.EncryptedApifyToken).SingleOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(encrypted)) return configuration["Apify:Token"];
        try { return protector.Unprotect(encrypted); }
        catch (CryptographicException) { throw new InvalidOperationException("Không đọc được token đã lưu. Hãy nhập lại token tại Admin → SocialBot → Kết nối Apify."); }
    }

    public async Task SaveAsync(string token, CancellationToken ct = default)
    {
        var row = await db.LichCaoDuLieus.SingleAsync(x => x.Id == 1, ct);
        row.EncryptedApifyToken = protector.Protect(token);
        await db.SaveChangesAsync(ct);
    }
}
