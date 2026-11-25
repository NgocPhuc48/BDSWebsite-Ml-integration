using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using NhaTot.Web.Models;

namespace NhaTot.Web.Services;

public sealed class CrawlSuggestionService(NhaTotDbContext context)
{
    public async Task<List<KetQuaKhopItemDto>> FindListingsAsync(int? districtId, int? wardId, int? typeId,
        string transaction, decimal? minimumPrice, decimal? maximumPrice, decimal? minimumArea,
        int? provinceId, IReadOnlyCollection<long> matchedListingIds)
    {
        if (typeId is null or <= 0) return [];
        var now = DateTime.Now;
        var query = from post in context.TinDangs.AsNoTracking()
                    join property in context.BatDongSans on post.MaBds equals property.MaBds
                    join ward in context.PhuongXas on property.MaPhuongXa equals ward.MaPhuongXa
                    join type in context.LoaiBds on property.MaLoaiBds equals type.MaLoaiBds
                    where post.TrangThai == "APPROVED" && (!post.NgayHetHan.HasValue || post.NgayHetHan > now)
                        && post.LoaiGiaoDich == transaction && property.MaLoaiBds == typeId
                        && ward.TrangThai == true && type.TrangThai == true
                    select new { Post = post, Property = property, Ward = ward, Type = type };
        if (wardId is > 0) query = query.Where(x => x.Ward.MaPhuongXa == wardId);
        else if (districtId is > 0) query = query.Where(x => x.Ward.MaQuanHuyen == districtId);
        else if (provinceId is > 0) query = query.Where(x => x.Ward.MaQuanHuyenNavigation != null && x.Ward.MaQuanHuyenNavigation.MaTinh == provinceId);
        else return [];
        var result = new List<KetQuaKhopItemDto>();
        await foreach (var x in query.OrderByDescending(x => x.Post.NgayDang).ThenByDescending(x => x.Post.MaTin).AsAsyncEnumerable())
        {
            if (matchedListingIds.Contains(x.Post.MaTin)) continue;
            var area = x.Property.DienTichDat ?? x.Property.DienTichSuDung;
            if (!CrawlSuggestionPolicy.Fits(x.Post.GiaRaoVnd, area, minimumPrice, maximumPrice, minimumArea)) continue;
            result.Add(new KetQuaKhopItemDto {
                Id = x.Post.MaTin, NguonTin = "TinDang", TieuDe = x.Post.TieuDe ?? "Tin đăng Nhà Tốt",
                GiaRaoVnd = x.Post.GiaRaoVnd,
                GiaHienThi = $"{x.Post.GiaRaoVnd:N0} đ" + (transaction == "CHO_THUE" ? "/tháng" : ""),
                DienTich = area, TenLoaiBds = x.Type.TenLoaiBds, TenKhuVuc = x.Ward.TenPhuongXa,
                UrlTinGoc = $"/TinDang/ChiTiet/{x.Post.MaTin}", CoLinkMxh = false, NgayDang = x.Post.NgayDang
            });
            if (result.Count == 25) break;
        }
        return result;
    }

    public async Task<List<KetQuaKhopItemDto>> FindAsync(int? districtId, int? wardId, int? typeId,
        string transaction, decimal? minimumPrice, decimal? maximumPrice, decimal? minimumArea, int? provinceId)
    {
        if (typeId is null or <= 0) return [];
        string? provinceName = provinceId is > 0
            ? await context.TinhThanhs.AsNoTracking().Where(t => t.MaTinh == provinceId).Select(t => t.TenTinh).FirstOrDefaultAsync()
            : null;
        var query = from post in context.DuLieuThuThaps.AsNoTracking()
                    join type in context.LoaiBds on post.MaLoaiBdsDaDuyet equals type.MaLoaiBds
                    join w in context.PhuongXas on post.MaPhuongXaDaDuyet equals w.MaPhuongXa into wardGroup
                    from ward in wardGroup.DefaultIfEmpty()
                    where post.TrangThaiXuLy == "APPROVED" && post.LoaiNoiDungDaDuyet == "OFFER"
                        && post.LoaiGiaoDichDaDuyet == transaction && post.MaLoaiBdsDaDuyet == typeId
                        && (ward == null || ward.TrangThai == true) && type.TrangThai == true
                    select new { Post = post, Ward = ward, Type = type };
        if (wardId is > 0) query = query.Where(x => x.Ward != null && x.Ward.MaPhuongXa == wardId);
        else if (districtId is > 0) query = query.Where(x => x.Ward != null && x.Ward.MaQuanHuyen == districtId);
        else if (provinceId is > 0) query = query.Where(x =>
            (x.Ward != null && x.Ward.MaQuanHuyenNavigation != null && x.Ward.MaQuanHuyenNavigation.MaTinh == provinceId)
            || (provinceName != null && x.Post.TinhNguon != null && provinceName.Contains(x.Post.TinhNguon)));
        else return []; // Không đoán địa phương cho mục tiêu cũ thiếu mã tỉnh.
        var result = new List<KetQuaKhopItemDto>();
        await foreach (var x in query.OrderByDescending(x => x.Post.NgayDangNguon ?? x.Post.NgayThuThap).AsAsyncEnumerable())
        {
            var post = x.Post;
            var effMin = post.GiaRaoVnd ?? post.GiaMinVnd;
            var effMax = post.GiaRaoVnd ?? post.GiaMaxVnd;
            if (!CrawlSuggestionPolicy.FitsRange(effMin, effMax, post.DienTich, minimumPrice, maximumPrice, minimumArea)) continue;
            if (!Uri.TryCreate(post.Urltin, UriKind.Absolute, out var url) || (url.Scheme != "https" && url.Scheme != "http")
                || !(url.Host == "facebook.com" || url.Host.EndsWith(".facebook.com", StringComparison.OrdinalIgnoreCase)
                     || url.Host == "fb.com" || url.Host.EndsWith(".fb.com", StringComparison.OrdinalIgnoreCase))) continue;
            var areaLabel = x.Ward != null ? $"{x.Ward.TenPhuongXa}, {post.TinhNguon}" : (post.TinhNguon ?? "Nghệ An / Hà Tĩnh");
            var midPrice = post.GiaRaoVnd ?? (effMin.HasValue && effMax.HasValue ? (effMin.Value + effMax.Value) / 2m : (effMin ?? effMax));
            var priceLabel = (post.GiaRaoVnd.HasValue
                ? RealEstateTextExtractor.FormatVndReadable(post.GiaRaoVnd.Value)
                : RealEstateTextExtractor.FormatPriceRange(post.GiaMinVnd, post.GiaMaxVnd))
                + (transaction == "CHO_THUE" ? "/tháng" : "");
            result.Add(new KetQuaKhopItemDto {
                Id = post.MaDuLieu, NguonTin = "CrawlApproved", TieuDe = post.TieuDe ?? "Bài rao từ Facebook",
                GiaRaoVnd = midPrice, GiaHienThi = priceLabel,
                DienTich = post.DienTich, TenLoaiBds = x.Type.TenLoaiBds,
                TenKhuVuc = areaLabel, UrlTinGoc = url.AbsoluteUri,
                CoLinkMxh = true, NgayDang = post.NgayDangNguon ?? post.NgayThuThap
            });
            if (result.Count == 25) break;
        }
        return result;
    }
}
