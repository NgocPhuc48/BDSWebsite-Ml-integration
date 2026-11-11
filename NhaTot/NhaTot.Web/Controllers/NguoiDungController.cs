using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Controllers
{
    [Authorize]
    public class NguoiDungController : Controller
    {
        private readonly NhaTotDbContext _context;

        public NguoiDungController(NhaTotDbContext context)
        {
            _context = context;
        }

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(claim, out var id) ? id : 0;
        }

        // GET: /NguoiDung/QuanLyTin (Danh sách tin của người dùng hiện tại)
        public async Task<IActionResult> QuanLyTin()
        {
            long userId = GetCurrentUserId();
            var danhSachTin = await _context.TinDangs
                .Include(t => t.MaBdsNavigation)
                    .ThenInclude(b => b!.MaLoaiBdsNavigation)
                .Include(t => t.MaBdsNavigation)
                    .ThenInclude(b => b!.MaPhuongXaNavigation)
                        .ThenInclude(p => p!.MaQuanHuyenNavigation)
                .Where(t => t.MaNguoiDang == userId)
                .OrderByDescending(t => t.NgayDang)
                .ToListAsync();

            return View(danhSachTin);
        }

        // GET: /NguoiDung/QuanLyVi (Xem số dư ví và thông tin ví)
        public async Task<IActionResult> QuanLyVi()
        {
            long userId = GetCurrentUserId();
            var vi = await _context.ViTaiKhoans.FirstOrDefaultAsync(v => v.MaNguoiDung == userId);
            if (vi == null)
            {
                // Tự động tạo ví nếu chưa có
                vi = new ViTaiKhoan
                {
                    MaNguoiDung = userId,
                    SoDuDiem = 0,
                    TrangThai = "ACTIVE",
                    NgayTao = DateTime.Now,
                    NgayCapNhat = DateTime.Now
                };
                _context.ViTaiKhoans.Add(vi);
                await _context.SaveChangesAsync();
            }

            var lichSuGiaoDich = await _context.LichSuGiaoDiches
                .Where(g => g.MaVi == vi.MaVi)
                .OrderByDescending(g => g.NgayTao)
                .Take(20)
                .ToListAsync();

            ViewBag.LichSu = lichSuGiaoDich;
            return View(vi);
        }

        // GET: /NguoiDung/LichSuGiaoDich
        public async Task<IActionResult> LichSuGiaoDich()
        {
            return await QuanLyVi();
        }

        // GET: /NguoiDung/LichHen (Danh sách lịch hẹn xem nhà)
        public async Task<IActionResult> LichHen()
        {
            long userId = GetCurrentUserId();
            var danhSachLichHen = await _context.LichHens
                .Include(l => l.MaTinNavigation)
                    .ThenInclude(t => t!.MaBdsNavigation)
                .Where(l => l.MaNguoiTao == userId || (l.MaTinNavigation != null && l.MaTinNavigation.MaNguoiDang == userId))
                .OrderByDescending(l => l.ThoiGianHen)
                .ToListAsync();

            ViewBag.CurrentUserId = userId;
            return View(danhSachLichHen);
        }

        // GET: /NguoiDung/TinDaLuu (Danh sách tin yêu thích)
        public async Task<IActionResult> TinDaLuu()
        {
            long userId = GetCurrentUserId();
            var yeuThich = await _context.YeuThiches
                .Include(y => y.MaTinNavigation)
                    .ThenInclude(t => t!.MaBdsNavigation)
                .Where(y => y.MaNguoiDung == userId)
                .OrderByDescending(y => y.NgayLuu)
                .ToListAsync();

            return View(yeuThich);
        }
    }
}

