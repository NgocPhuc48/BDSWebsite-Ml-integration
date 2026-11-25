using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Services;

public sealed class CrawlExtractionService(NhaTotDbContext context)
{
    public async Task<(List<string> Provinces, List<WardCandidate> Wards)> LoadLocationsAsync(CancellationToken ct)
    {
        var provinces = await context.TinhThanhs.AsNoTracking().Where(x => x.TenTinh != null)
            .Select(x => x.TenTinh!).ToListAsync(ct);
        var wards = await context.PhuongXas.AsNoTracking().Where(x => x.TenPhuongXa != null)
            .Select(x => new WardCandidate(x.TenPhuongXa!, x.MaQuanHuyenNavigation == null || x.MaQuanHuyenNavigation.MaTinhNavigation == null
                ? null : x.MaQuanHuyenNavigation.MaTinhNavigation.TenTinh)).ToListAsync(ct);
        return (provinces, wards);
    }

    public static void Apply(DuLieuThuThap entity, List<string> provinces, List<WardCandidate> wards)
    {
        var result = RealEstateTextExtractor.Extract(entity.NoiDungGoc ?? "", provinces, wards);
        if (!entity.GiaRaoVnd.HasValue && !entity.GiaMinVnd.HasValue && !entity.GiaMaxVnd.HasValue)
        {
            if (result.ExactPrice.HasValue)
            {
                entity.GiaRaoVnd = result.ExactPrice;
                entity.GiaMinVnd = null;
                entity.GiaMaxVnd = null;
            }
            else if (result.PriceMin.HasValue && result.PriceMax.HasValue)
            {
                entity.GiaRaoVnd = null;
                entity.GiaMinVnd = result.PriceMin;
                entity.GiaMaxVnd = result.PriceMax;
            }
        }
        entity.DienTich ??= result.Area;
        entity.LoaiBdsnguon ??= result.Type;
        entity.TinhNguon ??= result.Province;
        entity.PhuongXaNguon ??= result.Ward;
        // EXTRACTED vẫn cần kiểm duyệt trước khi dùng cho huấn luyện hay đăng tin.
        entity.TrangThaiXuLy = result.Complete ? "EXTRACTED" : "NEEDS_REVIEW";
        entity.LichSuXuLyDuLieus.Add(new LichSuXuLyDuLieu
        {
            BuocXuLy = "EXTRACT_RULES_V1", KetQua = result.Complete ? "SUCCESS" : "PARTIAL",
            ThoiDiem = DateTime.UtcNow,
            ChiTiet = "Bóc tách bằng quy tắc; không suy đoán trường thiếu. Giá theo tháng/đơn giá m²/khoảng giá không chuyển thành tổng giá. Cần kiểm duyệt dữ liệu."
        });
    }

    public async Task<int> ProcessPendingAsync(CancellationToken ct)
    {
        var locations = await LoadLocationsAsync(ct);
        var items = await context.DuLieuThuThaps.Where(x => x.TrangThaiXuLy == "NEW" && x.NoiDungGoc != null)
            .OrderBy(x => x.MaDuLieu).Take(100).ToListAsync(ct);
        foreach (var item in items) Apply(item, locations.Provinces, locations.Wards);
        await context.SaveChangesAsync(ct);
        return items.Count;
    }
}
