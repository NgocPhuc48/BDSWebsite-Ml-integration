using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
namespace NhaTot.Web.Services;

public sealed class ValuationTrainingService(NhaTotDbContext db, IWebHostEnvironment environment)
{
    public const string ModelName = "RidgeLogPrice-v1";
    public async Task<ValuationSample[]> DataAsync()
    {
        var internalRows = await db.TinDangs.AsNoTracking().Where(t => t.TrangThai == "APPROVED" && t.LoaiGiaoDich == "BAN"
            && t.MaBdsNavigation != null && t.MaBdsNavigation.MaPhuongXaNavigation != null && t.GiaRaoVnd > 0 && t.NgayDang != null)
            .Select(t => new { t.MaBds, t.GiaRaoVnd, t.NgayDang, t.MaBdsNavigation!.MaLoaiBds,
                t.MaBdsNavigation.MaPhuongXa, t.MaBdsNavigation.MaPhuongXaNavigation!.MaQuanHuyen,
                Area = t.MaBdsNavigation.DienTichDat ?? t.MaBdsNavigation.DienTichSuDung }).ToListAsync();
        var socialRows = await (from p in db.DuLieuThuThaps.AsNoTracking()
            join w in db.PhuongXas on p.MaPhuongXaDaDuyet equals w.MaPhuongXa
            where p.TrangThaiXuLy == "APPROVED" && p.LoaiNoiDungDaDuyet == "OFFER" && p.LoaiGiaoDichDaDuyet == "BAN" && p.NgayDangNguon != null
            select new { p.MaTinNguon, p.Urltin, p.HashNoiDung, p.MaDuLieu, p.MaLoaiBdsDaDuyet, p.MaPhuongXaDaDuyet, w.MaQuanHuyen,
                p.DienTich, p.GiaRaoVnd, p.NgayDangNguon }).ToListAsync();
        return ValuationRegression.Clean(internalRows.Select(x => new ValuationSample("bds:"+x.MaBds, x.MaLoaiBds ?? 0, x.MaQuanHuyen ?? 0,
            x.MaPhuongXa ?? 0, (double)(x.Area ?? 0), (double)(x.GiaRaoVnd ?? 0), x.NgayDang!.Value))
            .Concat(socialRows.Select(x => new ValuationSample("social:"+(x.HashNoiDung ?? x.MaTinNguon ?? x.Urltin ?? x.MaDuLieu.ToString()),
                x.MaLoaiBdsDaDuyet ?? 0, x.MaQuanHuyen ?? 0, x.MaPhuongXaDaDuyet ?? 0,
                (double)(x.DienTich ?? 0), (double)(x.GiaRaoVnd ?? 0), x.NgayDangNguon!.Value))));
    }
    public string ArtifactPath(string filename)
    {
        if (Path.GetFileName(filename) != filename || !filename.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Artifact không hợp lệ.");
        return Path.Combine(environment.ContentRootPath, "App_Data", "ValuationModels", filename);
    }
    public async Task<ValuationArtifact?> LoadAsync(PhienBanMoHinh row)
    {
        if (row.TenMoHinh != ModelName || string.IsNullOrWhiteSpace(row.DuongDanArtifact)) return null;
        try
        {
            var m = JsonSerializer.Deserialize<ValuationArtifact>(await File.ReadAllTextAsync(ArtifactPath(row.DuongDanArtifact)));
            return m is { Schema: 1, Accepted: true } && m.Weights.Length == 4 + m.Categories.Length ? m : null;
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidOperationException) { return null; }
    }
    public async Task<string> TrainAsync()
    {
        var data = await DataAsync(); var model = ValuationRegression.Train(data);
        var filename = Guid.NewGuid().ToString("N") + ".json"; var path = ArtifactPath(filename);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path + ".tmp", JsonSerializer.Serialize(model)); File.Move(path + ".tmp", path);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if (model.Accepted)
            foreach (var old in await db.PhienBanMoHinhs.Where(x => x.TenMoHinh == ModelName && x.TrangThai == "ACTIVE").ToListAsync()) old.TrangThai = "DEPRECATED";
        var row = new PhienBanMoHinh { TenMoHinh = ModelName, PhienBan = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
            DuongDanArtifact = filename, NgayHuanLuyen = DateTime.Now, SoMauHuanLuyen = model.Support.Length,
            TrangThai = model.Accepted ? "ACTIVE" : "REJECTED",
            GhiChu = $"Giá rao BAN; {data.Length} mẫu sau làm sạch; train {model.Support.Length}, test {model.Metrics.Count}. MAPE={model.Metrics.Mape:P1}; baseline MAE={model.Metrics.BaselineMae:N0}." };
        row.KetQuaDanhGiaMoHinhs.Add(new KetQuaDanhGiaMoHinh { TapDuLieu = "TEMPORAL_TEST", Mae = (decimal)model.Metrics.Mae,
            Rmse = (decimal)model.Metrics.Rmse, R2 = (decimal)Math.Clamp(model.Metrics.R2, -9999, 1),
            SoMau = model.Metrics.Count, NgayDanhGia = DateTime.Now, GhiChu = "Test độc lập sau tập train/calibration; đơn vị VNĐ." });
        db.PhienBanMoHinhs.Add(row); await db.SaveChangesAsync(); await transaction.CommitAsync();
        return $"{data.Length} mẫu; train {model.Support.Length}, test {model.Metrics.Count}. MAE {model.Metrics.Mae:N0} VNĐ, MAPE {model.Metrics.Mape:P1}. "
            + (model.Accepted ? "Đã kích hoạt và áp dụng vào Định giá." : "Chưa đạt MAPE ≤35% và MAE tốt hơn baseline; không kích hoạt.");
    }
}
