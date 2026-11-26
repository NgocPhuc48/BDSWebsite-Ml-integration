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
            var parsedGia = RealEstateTextExtractor.TryParseGiaRaoVnd(post.GiaRaoVnd);
            var effMin = parsedGia ?? post.GiaMinVnd;
            var effMax = parsedGia ?? post.GiaMaxVnd;
            if (!CrawlSuggestionPolicy.FitsRange(effMin, effMax, post.DienTich, minimumPrice, maximumPrice, minimumArea)) continue;
            if (!Uri.TryCreate(post.Urltin, UriKind.Absolute, out var url) || (url.Scheme != "https" && url.Scheme != "http")
                || !(url.Host == "facebook.com" || url.Host.EndsWith(".facebook.com", StringComparison.OrdinalIgnoreCase)
                     || url.Host == "fb.com" || url.Host.EndsWith(".fb.com", StringComparison.OrdinalIgnoreCase))) continue;
            var areaLabel = x.Ward != null ? $"{x.Ward.TenPhuongXa}, {post.TinhNguon}" : (post.TinhNguon ?? "Nghệ An / Hà Tĩnh");
            var midPrice = parsedGia ?? (effMin.HasValue && effMax.HasValue ? (effMin.Value + effMax.Value) / 2m : (effMin ?? effMax));
            var basePriceLabel = parsedGia.HasValue
                ? RealEstateTextExtractor.FormatVndReadable(parsedGia.Value)
                : (!string.IsNullOrWhiteSpace(post.GiaRaoVnd)
                    ? post.GiaRaoVnd.Trim()
                    : RealEstateTextExtractor.FormatPriceRange(post.GiaMinVnd, post.GiaMaxVnd));
            var priceLabel = basePriceLabel
                + (transaction == "CHO_THUE" && (parsedGia.HasValue || (post.GiaMinVnd.HasValue && post.GiaMaxVnd.HasValue)) ? "/tháng" : "");
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

    /// <summary>
    /// Gợi ý bất động sản từ TinDang và DuLieuThuThap dựa trên độ tương đồng >= 80%, tách riêng 2 danh sách.
    /// </summary>
    public async Task<(List<KetQuaKhopItemDto> SystemListings, List<KetQuaKhopItemDto> FacebookPosts)> FindRecommendedBySourceAsync(
        BayGiaDemandProfile demand,
        IReadOnlyCollection<long>? excludeListingIds = null,
        double minSimilarityPercent = BayGiaRecommendationEngine.DefaultMinSimilarityPercent,
        int maxPerSource = 24)
    {
        if (demand.PropertyTypeId <= 0) return ([], []);

        var excluded = excludeListingIds ?? Array.Empty<long>();
        var tinCandidates = new List<BayGiaCandidate>();
        var fbCandidates = new List<BayGiaCandidate>();
        var tinLookup = new Dictionary<long, (string Title, string? AreaName, string? TypeName, string? Image, string? ContactName, string? Phone)>();
        var socialLookup = new Dictionary<long, (string Title, string? AreaName, string? TypeName, string PriceLabel, string Url)>();

        // 1. Nguồn TinDang đã duyệt (ưu tiên cùng giao dịch & tỉnh nếu có)
        var tinQuery = context.TinDangs.AsNoTracking()
            .Include(t => t.MaNguoiDangNavigation)
            .Include(t => t.MaBdsNavigation!).ThenInclude(b => b.MaLoaiBdsNavigation)
            .Include(t => t.MaBdsNavigation!).ThenInclude(b => b.MaPhuongXaNavigation!).ThenInclude(w => w.MaQuanHuyenNavigation)
            .Include(t => t.MaBdsNavigation!).ThenInclude(b => b.ChiTietNhas)
            .Include(t => t.MaBdsNavigation!).ThenInclude(b => b.ChiTietDats)
            .Where(t => t.TrangThai == "APPROVED" && t.LoaiGiaoDich == demand.TransactionType && t.MaBdsNavigation != null);

        if (demand.ProvinceId is > 0)
        {
            tinQuery = tinQuery.Where(t =>
                t.MaBdsNavigation!.MaPhuongXaNavigation != null &&
                t.MaBdsNavigation.MaPhuongXaNavigation.MaQuanHuyenNavigation != null &&
                t.MaBdsNavigation.MaPhuongXaNavigation.MaQuanHuyenNavigation.MaTinh == demand.ProvinceId);
        }

        if (demand.MaxPrice is > 0)
        {
            var upperBound = demand.MaxPrice.Value * 1.35m;
            tinQuery = tinQuery.Where(t => !t.GiaRaoVnd.HasValue || t.GiaRaoVnd <= upperBound);
        }

        var tinRows = await tinQuery
            .OrderByDescending(t => t.NgayDang)
            .Take(500)
            .ToListAsync();

        foreach (var t in tinRows)
        {
            if (excluded.Contains(t.MaTin)) continue;
            var b = t.MaBdsNavigation!;
            var ward = b.MaPhuongXaNavigation;
            var dist = ward?.MaQuanHuyenNavigation;
            var nha = b.ChiTietNhas.FirstOrDefault();
            var dat = b.ChiTietDats.FirstOrDefault();
            var area = b.DienTichDat ?? b.DienTichSuDung;

            tinCandidates.Add(new BayGiaCandidate(
                Source: "TinDang",
                Id: t.MaTin,
                TransactionType: t.LoaiGiaoDich ?? "BAN",
                PropertyTypeId: b.MaLoaiBds,
                PropertyTypeName: b.MaLoaiBdsNavigation?.TenLoaiBds,
                ProvinceId: dist?.MaTinh,
                DistrictId: ward?.MaQuanHuyen,
                WardId: b.MaPhuongXa,
                Price: t.GiaRaoVnd,
                Area: area,
                Direction: b.Huong,
                LegalStatus: b.PhapLy,
                Bedrooms: nha?.SoPhongNgu,
                FrontageM: dat?.MatTienM,
                PublishedAt: t.NgayDang));

            var areaLabel = ward != null
                ? $"{ward.TenPhuongXa}, {dist?.TenQuanHuyen}"
                : (b.DiaChiMoTa ?? "Nghệ An / Hà Tĩnh");
            tinLookup[t.MaTin] = (
                t.TieuDe ?? "Tin đăng Nhà Tốt",
                areaLabel,
                b.MaLoaiBdsNavigation?.TenLoaiBds,
                t.DanhSachAnhDatDai,
                t.MaNguoiDangNavigation?.HoTen ?? "Thành viên Nhà Tốt",
                t.MaNguoiDangNavigation?.SoDienThoai);
        }

        // 2. Nguồn DuLieuThuThap đã duyệt (Facebook MXH hợp lệ)
        var socialQuery = from post in context.DuLieuThuThaps.AsNoTracking()
                          join type in context.LoaiBds on post.MaLoaiBdsDaDuyet equals type.MaLoaiBds
                          join w in context.PhuongXas on post.MaPhuongXaDaDuyet equals w.MaPhuongXa into wardGroup
                          from ward in wardGroup.DefaultIfEmpty()
                          where post.TrangThaiXuLy == "APPROVED"
                              && post.LoaiNoiDungDaDuyet == "OFFER"
                              && post.LoaiGiaoDichDaDuyet == demand.TransactionType
                          orderby post.NgayDangNguon ?? post.NgayThuThap descending
                          select new
                          {
                              Post = post,
                              TypeName = type.TenLoaiBds,
                              WardId = ward != null ? (int?)ward.MaPhuongXa : null,
                              WardName = ward != null ? ward.TenPhuongXa : null,
                              DistrictId = ward != null ? ward.MaQuanHuyen : null,
                              DistrictName = ward != null && ward.MaQuanHuyenNavigation != null ? ward.MaQuanHuyenNavigation.TenQuanHuyen : null,
                              ProvinceId = ward != null && ward.MaQuanHuyenNavigation != null ? ward.MaQuanHuyenNavigation.MaTinh : null
                          };

        var socialRows = await socialQuery.Take(300).ToListAsync();
        foreach (var x in socialRows)
        {
            var post = x.Post;
            if (!Uri.TryCreate(post.Urltin, UriKind.Absolute, out var url) || (url.Scheme != "https" && url.Scheme != "http")
                || !(url.Host == "facebook.com" || url.Host.EndsWith(".facebook.com", StringComparison.OrdinalIgnoreCase)
                     || url.Host == "fb.com" || url.Host.EndsWith(".fb.com", StringComparison.OrdinalIgnoreCase)))
                continue;

            var parsedGia = RealEstateTextExtractor.TryParseGiaRaoVnd(post.GiaRaoVnd);
            var effMin = parsedGia ?? post.GiaMinVnd;
            var effMax = parsedGia ?? post.GiaMaxVnd;
            var midPrice = parsedGia ?? (effMin.HasValue && effMax.HasValue ? (effMin.Value + effMax.Value) / 2m : (effMin ?? effMax));
            var priceLabel = parsedGia.HasValue
                ? RealEstateTextExtractor.FormatVndReadable(parsedGia.Value)
                : (!string.IsNullOrWhiteSpace(post.GiaRaoVnd)
                    ? post.GiaRaoVnd.Trim()
                    : RealEstateTextExtractor.FormatPriceRange(post.GiaMinVnd, post.GiaMaxVnd));
            if (demand.TransactionType == "CHO_THUE" && (parsedGia.HasValue || (post.GiaMinVnd.HasValue && post.GiaMaxVnd.HasValue)))
                priceLabel += "/tháng";

            var areaLabel = x.WardName != null
                ? $"{x.WardName}, {x.DistrictName ?? post.TinhNguon}"
                : (post.TinhNguon ?? "Nghệ An / Hà Tĩnh");

            fbCandidates.Add(new BayGiaCandidate(
                Source: "CrawlApproved",
                Id: post.MaDuLieu,
                TransactionType: post.LoaiGiaoDichDaDuyet ?? "BAN",
                PropertyTypeId: post.MaLoaiBdsDaDuyet,
                PropertyTypeName: x.TypeName,
                ProvinceId: x.ProvinceId,
                DistrictId: x.DistrictId,
                WardId: x.WardId,
                Price: midPrice,
                Area: post.DienTich,
                Direction: null,
                LegalStatus: null,
                Bedrooms: null,
                FrontageM: null,
                PublishedAt: post.NgayDangNguon ?? post.NgayThuThap));

            socialLookup[post.MaDuLieu] = (
                post.TieuDe ?? "Bài rao từ Facebook",
                areaLabel,
                x.TypeName,
                priceLabel,
                url.AbsoluteUri);
        }

        // 3. Chấm điểm & lọc >= minSimilarityPercent (80%) riêng cho từng nguồn
        var rankedTin = BayGiaRecommendationEngine.FilterRecommendations(demand, tinCandidates, minSimilarityPercent, maxPerSource);
        var tinResults = new List<KetQuaKhopItemDto>(rankedTin.Count);
        foreach (var (c, sim) in rankedTin)
        {
            if (!tinLookup.TryGetValue(c.Id, out var tInfo)) continue;
            string giaHienThi = !c.Price.HasValue || c.Price.Value <= 0
                ? "Giá: Liên hệ"
                : RealEstateTextExtractor.FormatVndReadable(c.Price.Value) + (c.TransactionType == "CHO_THUE" ? "/tháng" : "");

            tinResults.Add(new KetQuaKhopItemDto
            {
                Id = c.Id,
                NguonTin = "TinDang",
                TieuDe = tInfo.Title,
                GiaRaoVnd = c.Price,
                GiaHienThi = giaHienThi,
                DienTich = c.Area,
                Huong = c.Direction,
                SoPhongNgu = c.Bedrooms,
                MatTienM = c.FrontageM,
                HinhAnh = tInfo.Image,
                TenLoaiBds = tInfo.TypeName,
                TenKhuVuc = tInfo.AreaName,
                TenLienHe = tInfo.ContactName,
                SoDienThoai = tInfo.Phone,
                UrlTinGoc = $"/TinDang/ChiTiet/{c.Id}",
                CoLinkMxh = false,
                NgayDang = c.PublishedAt,
                DoTuongDongPhanTram = sim.ScorePercent,
                LyDoTuongDong = sim.Reasons
            });
        }

        var rankedFb = BayGiaRecommendationEngine.FilterRecommendations(demand, fbCandidates, minSimilarityPercent, maxPerSource);
        var fbResults = new List<KetQuaKhopItemDto>(rankedFb.Count);
        foreach (var (c, sim) in rankedFb)
        {
            if (!socialLookup.TryGetValue(c.Id, out var sInfo)) continue;
            fbResults.Add(new KetQuaKhopItemDto
            {
                Id = c.Id,
                NguonTin = "CrawlApproved",
                TieuDe = sInfo.Title,
                GiaRaoVnd = c.Price,
                GiaHienThi = sInfo.PriceLabel,
                DienTich = c.Area,
                TenLoaiBds = sInfo.TypeName,
                TenKhuVuc = sInfo.AreaName,
                UrlTinGoc = sInfo.Url,
                CoLinkMxh = true,
                NgayDang = c.PublishedAt,
                DoTuongDongPhanTram = sim.ScorePercent,
                LyDoTuongDong = sim.Reasons
            });
        }

        return (tinResults, fbResults);
    }

    /// <summary>
    /// Gợi ý gộp cả TinDang và DuLieuThuThap đạt độ tương đồng >= 80%.
    /// </summary>
    public async Task<List<KetQuaKhopItemDto>> FindRecommendedAsync(
        BayGiaDemandProfile demand,
        IReadOnlyCollection<long> excludeListingIds,
        double minSimilarityPercent = BayGiaRecommendationEngine.DefaultMinSimilarityPercent,
        int maxResults = 18)
    {
        var (tinResults, fbResults) = await FindRecommendedBySourceAsync(demand, excludeListingIds, minSimilarityPercent, maxResults);
        return tinResults.Concat(fbResults)
            .OrderByDescending(x => x.DoTuongDongPhanTram ?? 0)
            .ThenByDescending(x => x.NgayDang ?? DateTime.MinValue)
            .Take(maxResults)
            .ToList();
    }
}
