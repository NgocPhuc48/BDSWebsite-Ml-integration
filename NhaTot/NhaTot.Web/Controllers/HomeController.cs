using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using NhaTot.Web.Models;

namespace NhaTot.Web.Controllers;

public class HomeController : Controller
{
    private readonly NhaTotDbContext _context;

    public HomeController(NhaTotDbContext context)
    {
        _context = context;
    }

    // GET: / (Trang chủ & Tìm kiếm)
    public async Task<IActionResult> Index([FromQuery] SearchFilterModel filter, int page = 1)
    {
        var viewModel = new HomeViewModel
        {
            Filter = filter,
            DanhSachLoaiBds = await _context.LoaiBds.Where(l => l.TrangThai == true).ToListAsync(),
            DanhSachTinhThanh = await _context.TinhThanhs.Where(t => t.TrangThai == true).ToListAsync()
        };

        // Truy vấn gốc tin đã được duyệt
        var query = _context.TinDangs
            .Include(t => t.MaBdsNavigation)
                .ThenInclude(b => b!.MaLoaiBdsNavigation)
            .Include(t => t.MaBdsNavigation)
                .ThenInclude(b => b!.MaPhuongXaNavigation)
                    .ThenInclude(p => p!.MaQuanHuyenNavigation)
            .Where(t => t.TrangThai == "APPROVED")
            .AsQueryable();

        // --- ÁP DỤNG CÁC BỘ LỌC ĐA TIÊU CHÍ ---
        if (!string.IsNullOrEmpty(filter.LoaiGiaoDich))
            query = query.Where(t => t.LoaiGiaoDich == filter.LoaiGiaoDich);

        if (filter.MaLoaiBds.HasValue && filter.MaLoaiBds > 0)
            query = query.Where(t => t.MaBdsNavigation != null && t.MaBdsNavigation.MaLoaiBds == filter.MaLoaiBds);

        if (filter.MaPhuongXa.HasValue && filter.MaPhuongXa > 0)
            query = query.Where(t => t.MaBdsNavigation != null && t.MaBdsNavigation.MaPhuongXa == filter.MaPhuongXa);
        else if (filter.MaTinh.HasValue && filter.MaTinh > 0)
            query = query.Where(t => t.MaBdsNavigation != null && t.MaBdsNavigation.MaPhuongXaNavigation != null && t.MaBdsNavigation.MaPhuongXaNavigation.MaQuanHuyenNavigation != null && t.MaBdsNavigation.MaPhuongXaNavigation.MaQuanHuyenNavigation.MaTinh == filter.MaTinh);
        else if (filter.MaQuanHuyen.HasValue && filter.MaQuanHuyen > 0)
            query = query.Where(t => t.MaBdsNavigation != null && t.MaBdsNavigation.MaPhuongXaNavigation != null && t.MaBdsNavigation.MaPhuongXaNavigation.MaQuanHuyen == filter.MaQuanHuyen);

        if (filter.GiaTu.HasValue) query = query.Where(t => t.GiaRaoVnd >= filter.GiaTu);
        if (filter.GiaDen.HasValue) query = query.Where(t => t.GiaRaoVnd <= filter.GiaDen);

        if (filter.DienTichTu.HasValue) query = query.Where(t => t.MaBdsNavigation != null && t.MaBdsNavigation.DienTichDat >= filter.DienTichTu);
        if (filter.DienTichDen.HasValue) query = query.Where(t => t.MaBdsNavigation != null && t.MaBdsNavigation.DienTichDat <= filter.DienTichDen);

        if (!string.IsNullOrEmpty(filter.Huong))
            query = query.Where(t => t.MaBdsNavigation != null && t.MaBdsNavigation.Huong == filter.Huong);

        if (!string.IsNullOrWhiteSpace(filter.TuKhoa))
        {
            string kw = filter.TuKhoa.Trim().ToLower();
            query = query.Where(t => t.TieuDe!.ToLower().Contains(kw) || (t.MoTa != null && t.MoTa.ToLower().Contains(kw)));
        }

        const int pageSize = 12;
        var totalItems = await query.CountAsync();
        page = PaginationViewModel.NormalizePage(page, pageSize, totalItems);
        ViewBag.Pagination = new PaginationViewModel(page, pageSize, totalItems);

        // Lấy danh sách kết quả theo trang
        viewModel.TinMoiNhat = await query
            .OrderByDescending(t => t.NgayDang)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TinDangItemDto
            {
                MaTin = t.MaTin,
                TieuDe = t.TieuDe ?? "Không có tiêu đề",
                GiaRaoVnd = t.GiaRaoVnd,
                LoaiGiaoDich = t.LoaiGiaoDich ?? "BAN",
                TenLoaiBds = t.MaBdsNavigation!.MaLoaiBdsNavigation!.TenLoaiBds ?? "BĐS",
                DienTichDat = t.MaBdsNavigation.DienTichDat ?? t.MaBdsNavigation.DienTichSuDung,
                DiaChiHienThi = (t.MaBdsNavigation.MaPhuongXaNavigation != null)
                    ? $"{t.MaBdsNavigation.MaPhuongXaNavigation.TenPhuongXa}, {t.MaBdsNavigation.MaPhuongXaNavigation.MaQuanHuyenNavigation!.TenQuanHuyen}"
                    : t.MaBdsNavigation.DiaChiMoTa ?? "",
                ViDo = t.MaBdsNavigation.ViDo,
                KinhDo = t.MaBdsNavigation.KinhDo,
                HinhAnh = t.DanhSachAnhDatDai,
                AnhDaiDien = t.DanhSachAnhDatDai,
                NgayDang = t.NgayDang
            }).ToListAsync();

        viewModel.TinNoiBat = viewModel.TinMoiNhat.Take(4).ToList();

        return View(viewModel);
    }

    // --- AJAX APIs DÀNH CHO BẬC THANG ĐỊA CHÍNH ---
    [HttpGet]
    public async Task<IActionResult> GetQuanHuyen(int maTinh)
    {
        var list = await _context.QuanHuyens
            .Where(q => q.MaTinh == maTinh && q.TrangThai == true)
            .Select(q => new { q.MaQuanHuyen, q.TenQuanHuyen })
            .ToListAsync();
        return Json(list);
    }

    [HttpGet]
    public async Task<IActionResult> GetPhuongXa(int? maQuanHuyen, int? maTinh)
    {
        var query = _context.PhuongXas.Where(p => p.TrangThai == true);

        if (maTinh.HasValue && maTinh > 0)
        {
            query = query.Where(p => p.MaQuanHuyenNavigation != null && p.MaQuanHuyenNavigation.MaTinh == maTinh);
        }
        else if (maQuanHuyen.HasValue && maQuanHuyen > 0)
        {
            query = query.Where(p => p.MaQuanHuyen == maQuanHuyen);
        }

        var list = await query
            .OrderBy(p => p.TenPhuongXa)
            .Select(p => new { p.MaPhuongXa, p.TenPhuongXa })
            .ToListAsync();
        return Json(list);
    }

    // --- AJAX API BẢN ĐỒ GIS ---
    [HttpGet]
    public async Task<IActionResult> GetMapLocations(
        decimal? minLat, decimal? maxLat, decimal? minLng, decimal? maxLng,
        int? maLoaiBds, string? loaiGiaoDich, decimal? giaTu, decimal? giaDen,
        int? maTinh, int? maQuanHuyen, int? maPhuongXa, string? tuKhoa)
    {
        // 1. Lấy tất cả tin đã duyệt (APPROVED) cùng điều kiện với danh sách tin
        var query = _context.TinDangs
            .Include(t => t.MaBdsNavigation)
                .ThenInclude(b => b!.MaPhuongXaNavigation)
                    .ThenInclude(p => p!.MaQuanHuyenNavigation)
            .Where(t => t.TrangThai == "APPROVED" && t.MaBdsNavigation != null)
            .AsQueryable();

        if (!string.IsNullOrEmpty(loaiGiaoDich)) query = query.Where(t => t.LoaiGiaoDich == loaiGiaoDich);
        if (maLoaiBds.HasValue && maLoaiBds > 0) query = query.Where(t => t.MaBdsNavigation!.MaLoaiBds == maLoaiBds);
        if (giaTu.HasValue) query = query.Where(t => t.GiaRaoVnd >= giaTu);
        if (giaDen.HasValue) query = query.Where(t => t.GiaRaoVnd <= giaDen);

        if (maPhuongXa.HasValue && maPhuongXa > 0)
        {
            query = query.Where(t => t.MaBdsNavigation!.MaPhuongXa == maPhuongXa);
        }
        else if (maTinh.HasValue && maTinh > 0)
        {
            query = query.Where(t => t.MaBdsNavigation!.MaPhuongXaNavigation != null &&
                                     t.MaBdsNavigation.MaPhuongXaNavigation.MaQuanHuyenNavigation != null &&
                                     t.MaBdsNavigation.MaPhuongXaNavigation.MaQuanHuyenNavigation.MaTinh == maTinh);
        }
        else if (maQuanHuyen.HasValue && maQuanHuyen > 0)
        {
            query = query.Where(t => t.MaBdsNavigation!.MaPhuongXaNavigation != null && t.MaBdsNavigation.MaPhuongXaNavigation.MaQuanHuyen == maQuanHuyen);
        }

        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            string kw = tuKhoa.Trim().ToLower();
            query = query.Where(t => t.TieuDe!.ToLower().Contains(kw) || (t.MoTa != null && t.MoTa.ToLower().Contains(kw)));
        }

        var tinList = await query
            .OrderByDescending(t => t.NgayDang)
            .Take(100)
            .ToListAsync();

        // 2. Tọa độ mặc định trung tâm (TP Vinh, Nghệ An)
        const decimal baseDefaultLat = 18.6738m;
        const decimal baseDefaultLng = 105.6925m;

        // Bảng tọa độ trung tâm tham chiếu cho các Huyện/Thị xã ở Nghệ An nếu có
        var quanHuyenCoords = new Dictionary<string, (decimal Lat, decimal Lng)>(StringComparer.OrdinalIgnoreCase)
        {
            { "Thành phố Vinh", (18.6738m, 105.6925m) },
            { "Thị xã Cửa Lò", (18.8012m, 105.7130m) },
            { "Thị xã Thái Hòa", (19.3241m, 105.4389m) },
            { "Thị xã Hoàng Mai", (19.2483m, 105.7061m) },
            { "Huyện Nghi Lộc", (18.7833m, 105.5833m) },
            { "Huyện Diễn Châu", (18.9833m, 105.5833m) },
            { "Huyện Hưng Nguyên", (18.6833m, 105.6167m) },
            { "Huyện Nam Đàn", (18.7000m, 105.4667m) }
        };

        var pins = new List<MapLocationDto>();

        foreach (var t in tinList)
        {
            var bds = t.MaBdsNavigation;
            if (bds == null) continue;

            decimal viDo;
            decimal kinhDo;

            // Kiểm tra tọa độ có hợp lệ không (> 0 và nằm trong vùng Việt Nam)
            if (bds.ViDo.HasValue && bds.ViDo.Value > 8m && bds.ViDo.Value < 24m &&
                bds.KinhDo.HasValue && bds.KinhDo.Value > 100m && bds.KinhDo.Value < 112m)
            {
                viDo = bds.ViDo.Value;
                kinhDo = bds.KinhDo.Value;
            }
            else
            {
                // Tự động gán tọa độ theo Quận/Huyện hoặc Phường/Xã nếu chưa có
                string? tenQuan = bds.MaPhuongXaNavigation?.MaQuanHuyenNavigation?.TenQuanHuyen;
                decimal centerLat = baseDefaultLat;
                decimal centerLng = baseDefaultLng;

                if (!string.IsNullOrEmpty(tenQuan))
                {
                    foreach (var kvp in quanHuyenCoords)
                    {
                        if (tenQuan.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) || kvp.Key.Contains(tenQuan, StringComparison.OrdinalIgnoreCase))
                        {
                            centerLat = kvp.Value.Lat;
                            centerLng = kvp.Value.Lng;
                            break;
                        }
                    }
                }

                // Thêm độ lệch ngẫu nhiên cực nhỏ (~0.001 - 0.004 độ) dựa trên MaTin để các ghim không bị đè khít lên nhau
                int hash = (int)(t.MaTin % 100);
                double angle = (hash * 36.0) * Math.PI / 180.0;
                double radius = 0.0015 + ((hash % 5) * 0.0006);

                viDo = Math.Round(centerLat + (decimal)(radius * Math.Cos(angle)), 6);
                kinhDo = Math.Round(centerLng + (decimal)(radius * Math.Sin(angle)), 6);
            }

            // Nếu người dùng đang pan/zoom bản đồ và truyền bounds, lọc theo bounds
            if (minLat.HasValue && maxLat.HasValue && minLng.HasValue && maxLng.HasValue)
            {
                if (viDo < minLat.Value || viDo > maxLat.Value || kinhDo < minLng.Value || kinhDo > maxLng.Value)
                {
                    continue;
                }
            }

            string diaChi = bds.DiaChiMoTa ?? "";
            if (string.IsNullOrWhiteSpace(diaChi) && bds.MaPhuongXaNavigation != null)
            {
                diaChi = $"{bds.MaPhuongXaNavigation.TenPhuongXa}, {bds.MaPhuongXaNavigation.MaQuanHuyenNavigation?.TenQuanHuyen}";
            }

            pins.Add(new MapLocationDto
            {
                MaTin = t.MaTin,
                TieuDe = t.TieuDe ?? "BĐS",
                GiaRaoVnd = t.GiaRaoVnd,
                DienTichDat = bds.DienTichDat,
                ViDo = viDo,
                KinhDo = kinhDo,
                DiaChi = diaChi
            });
        }

        return Json(pins);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleYeuThich(long maTin)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out var userId))
        {
            return Json(new { success = false, message = "Vui lòng đăng nhập để lưu tin yêu thích!" });
        }

        var existing = await _context.YeuThiches
            .FirstOrDefaultAsync(y => y.MaTin == maTin && y.MaNguoiDung == userId);

        if (existing != null)
        {
            _context.YeuThiches.Remove(existing);
            await _context.SaveChangesAsync();
            return Json(new { success = true, isSaved = false, message = "Đã bỏ lưu tin khỏi danh sách yêu thích!" });
        }
        else
        {
            var yt = new YeuThich
            {
                MaTin = maTin,
                MaNguoiDung = userId,
                NgayLuu = DateTime.Now
            };
            _context.YeuThiches.Add(yt);
            await _context.SaveChangesAsync();
            return Json(new { success = true, isSaved = true, message = "Đã lưu tin vào danh sách yêu thích!" });
        }
    }
}