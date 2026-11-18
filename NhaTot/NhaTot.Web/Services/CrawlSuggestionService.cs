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
        var query = from post in context.DuLieuThuThaps.AsNoTracking()
                    join ward in context.PhuongXas on post.MaPhuongXaDaDuyet equals ward.MaPhuongXa
                    join type in context.LoaiBds on post.MaLoaiBdsDaDuyet equals type.MaLoaiBds
                    where post.TrangThaiXuLy == "APPROVED" && post.LoaiNoiDungDaDuyet == "OFFER"
                        && post.LoaiGiaoDichDaDuyet == transaction && post.MaLoaiBdsDaDuyet == typeId
                        && ward.TrangThai == true && type.TrangThai == true
                    select new { Post = post, Ward = ward, Type = type };
        if (wardId is > 0) query = query.Where(x => x.Ward.MaPhuongXa == wardId);
        else if (districtId is > 0) query = query.Where(x => x.Ward.MaQuanHuyen == districtId);
        else if (provinceId is > 0) query = query.Where(x => x.Ward.MaQuanHuyenNavigation != null && x.Ward.MaQuanHuyenNavigation.MaTinh == provinceId);
        else return []; // Không đoán địa phương cho mục tiêu cũ thiếu mã tỉnh.
        var result = new List<KetQuaKhopItemDto>();
        await foreach (var x in query.OrderByDescending(x => x.Post.NgayDangNguon ?? x.Post.NgayThuThap).AsAsyncEnumerable())
        {
            var post = x.Post;
            if (!CrawlSuggestionPolicy.Fits(post.GiaRaoVnd, post.DienTich, minimumPrice, maximumPrice, minimumArea)) continue;
            if (!Uri.TryCreate(post.Urltin, UriKind.Absolute, out var url) || url.Scheme != "https"
                || !(url.Host == "facebook.com" || url.Host.EndsWith(".facebook.com", StringComparison.Ordinal))) continue;
            result.Add(new KetQuaKhopItemDto {
                Id = post.MaDuLieu, NguonTin = "CrawlApproved", TieuDe = post.TieuDe ?? "Bài rao từ Facebook",
                GiaRaoVnd = post.GiaRaoVnd, GiaHienThi = $"{post.GiaRaoVnd:N0} đ" + (transaction == "CHO_THUE" ? "/tháng" : ""),
                DienTich = post.DienTich, TenLoaiBds = x.Type.TenLoaiBds,
                TenKhuVuc = $"{x.Ward.TenPhuongXa}, {post.TinhNguon}", UrlTinGoc = url.AbsoluteUri,
                CoLinkMxh = true, NgayDang = post.NgayDangNguon ?? post.NgayThuThap
            });
            if (result.Count == 25) break;
        }
        return result;
    }
}
