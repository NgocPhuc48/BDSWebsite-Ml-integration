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
    public async Task<IActionResult> Index([FromQuery] SearchFilterModel filter)
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

        // Lấy danh sách kết quả mới nhất
        viewModel.TinMoiNhat = await query
            .OrderByDescending(t => t.NgayDang)
            .Take(12)
            .Select(t => new TinDangItemDto
            {
                MaTin = t.MaTin,
                TieuDe = t.TieuDe ?? "Không có tiêu đề",
                GiaRaoVnd = t.GiaRaoVnd,
                LoaiGiaoDich = t.LoaiGiaoDich ?? "BAN",
                TenLoaiBds = t.MaBdsNavigation!.MaLoaiBdsNavigation!.TenLoaiBds ?? "BĐS",
                DienTichDat = t.MaBdsNavigation.DienTichDat,
                DiaChiHienThi = (t.MaBdsNavigation.MaPhuongXaNavigation != null)
                    ? $"{t.MaBdsNavigation.MaPhuongXaNavigation.TenPhuongXa}, {t.MaBdsNavigation.MaPhuongXaNavigation.MaQuanHuyenNavigation!.TenQuanHuyen}"
                    : t.MaBdsNavigation.DiaChiMoTa ?? "",
                ViDo = t.MaBdsNavigation.ViDo,
                KinhDo = t.MaBdsNavigation.KinhDo,
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
        var query = _context.TinDangs
            .Include(t => t.MaBdsNavigation)
                .ThenInclude(b => b!.MaPhuongXaNavigation)
            .Where(t => t.TrangThai == "APPROVED" && t.MaBdsNavigation != null && t.MaBdsNavigation.ViDo.HasValue && t.MaBdsNavigation.KinhDo.HasValue)
            .AsQueryable();

        if (minLat.HasValue && maxLat.HasValue && minLng.HasValue && maxLng.HasValue)
        {
            query = query.Where(t => t.MaBdsNavigation!.ViDo >= minLat && t.MaBdsNavigation.ViDo <= maxLat &&
                                     t.MaBdsNavigation.KinhDo >= minLng && t.MaBdsNavigation.KinhDo <= maxLng);
        }

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

        var pins = await query.Take(100).Select(t => new MapLocationDto
        {
            MaTin = t.MaTin,
            TieuDe = t.TieuDe ?? "BĐS",
            GiaRaoVnd = t.GiaRaoVnd,
            DienTichDat = t.MaBdsNavigation!.DienTichDat,
            ViDo = t.MaBdsNavigation.ViDo!.Value,
            KinhDo = t.MaBdsNavigation.KinhDo!.Value,
            DiaChi = t.MaBdsNavigation.DiaChiMoTa ?? (t.MaBdsNavigation.MaPhuongXaNavigation != null ? t.MaBdsNavigation.MaPhuongXaNavigation.TenPhuongXa! : "")
        }).ToListAsync();

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