using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using NhaTot.Web.Models;

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
        public async Task<IActionResult> QuanLyTin(int page = 1)
        {
            long userId = GetCurrentUserId();
            var query = _context.TinDangs
                .Include(t => t.MaBdsNavigation)
                    .ThenInclude(b => b!.MaLoaiBdsNavigation)
                .Include(t => t.MaBdsNavigation)
                    .ThenInclude(b => b!.MaPhuongXaNavigation)
                        .ThenInclude(p => p!.MaQuanHuyenNavigation)
                .Where(t => t.MaNguoiDang == userId);

            const int pageSize = 15;
            var totalItems = await query.CountAsync();
            page = PaginationViewModel.NormalizePage(page, pageSize, totalItems);
            ViewBag.Pagination = new PaginationViewModel(page, pageSize, totalItems);

            var danhSachTin = await query
                .OrderByDescending(t => t.NgayDang)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

        // POST: /NguoiDung/NapTienTest (Popup nạp tiền/điểm vào ví để test chức năng Bẫy Giá)
        [HttpPost]
        public async Task<IActionResult> NapTienTest([FromBody] NapTienRequest model)
        {
            if (model == null || model.SoDiem <= 0)
            {
                return Json(new { success = false, message = "Số tiền hoặc số điểm nạp phải lớn hơn 0!" });
            }

            long userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện nạp ví!" });
            }

            var vi = await _context.ViTaiKhoans.FirstOrDefaultAsync(v => v.MaNguoiDung == userId);
            if (vi == null)
            {
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

            decimal soDiemNap = Math.Round(model.SoDiem, 2);
            vi.SoDuDiem = (vi.SoDuDiem ?? 0) + soDiemNap;
            vi.NgayCapNhat = DateTime.Now;

            string moTaGiaoDich = string.IsNullOrWhiteSpace(model.GhiChu)
                ? $"Nạp tiền qua mã QR thanh toán (+{soDiemNap:N0} điểm/VNĐ) phục vụ Bẫy Giá"
                : model.GhiChu.Trim();

            var gd = new LichSuGiaoDich
            {
                MaVi = vi.MaVi,
                LoaiGiaoDich = "NAP_TIEN_QR",
                SoDiem = soDiemNap,
                SoDuSauGiaoDich = vi.SoDuDiem,
                MoTa = moTaGiaoDich,
                NgayTao = DateTime.Now
            };

            _context.LichSuGiaoDiches.Add(gd);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Nạp thành công {soDiemNap:N0} vào ví tài khoản!",
                soDuMoi = vi.SoDuDiem
            });
        }

        public class NapTienRequest
        {
            public decimal SoDiem { get; set; }
            public string? GhiChu { get; set; }
        }

        // GET: /NguoiDung/LichHen (Danh sách lịch hẹn xem nhà)
        public async Task<IActionResult> LichHen()
        {
            long userId = GetCurrentUserId();
            var danhSachLichHen = await _context.LichHens
                .Include(l => l.MaTinNavigation)
                    .ThenInclude(t => t!.MaBdsNavigation)
                .Include(l => l.MaYeuCauNavigation)
                .Where(l => l.MaNguoiTao == userId || (l.MaTinNavigation != null && l.MaTinNavigation.MaNguoiDang == userId))
                .OrderByDescending(l => l.ThoiGianHen)
                .ToListAsync();

            ViewBag.CurrentUserId = userId;
            return View(danhSachLichHen);
        }

        // POST: /NguoiDung/HuyLichHen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyLichHen(long maLichHen, string? lyDoHuy)
        {
            long userId = GetCurrentUserId();
            var lichHen = await _context.LichHens
                .Include(l => l.MaYeuCauNavigation)
                .FirstOrDefaultAsync(l => l.MaLichHen == maLichHen);

            if (lichHen == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin lịch hẹn!";
                return RedirectToAction(nameof(LichHen));
            }

            // Chỉ cho phép người tạo hoặc chủ nhà hủy
            if (lichHen.MaNguoiTao != userId && lichHen.MaYeuCauNavigation?.MaNguoiGui != userId)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền hủy lịch hẹn này!";
                return RedirectToAction(nameof(LichHen));
            }

            // Kiểm tra trạng thái: chỉ được hủy khi chưa diễn ra / chưa hủy / chưa hoàn thành
            if (lichHen.TrangThai == "DA_XEM_NHA" || lichHen.TrangThai == "COMPLETED")
            {
                TempData["ErrorMessage"] = "Cuộc hẹn đã hoàn thành xem nhà thực tế, không thể hủy!";
                return RedirectToAction(nameof(LichHen));
            }

            if (lichHen.TrangThai == "DA_HUY" || lichHen.TrangThai == "CANCELLED" || lichHen.TrangThai == "HUY")
            {
                TempData["ErrorMessage"] = "Lịch hẹn này đã bị hủy trước đó!";
                return RedirectToAction(nameof(LichHen));
            }

            lichHen.TrangThai = "DA_HUY";
            if (!string.IsNullOrWhiteSpace(lyDoHuy))
            {
                lichHen.GhiChu = string.IsNullOrWhiteSpace(lichHen.GhiChu)
                    ? $"[Khách hủy: {lyDoHuy}]"
                    : $"{lichHen.GhiChu} (Khách hủy: {lyDoHuy})";
            }

            // Gửi thông báo đến tư vấn viên nếu có phân công
            if (lichHen.MaYeuCau.HasValue)
            {
                var phanCong = await _context.PhanCongTuVans.FirstOrDefaultAsync(p => p.MaYeuCau == lichHen.MaYeuCau.Value);
                if (phanCong != null && phanCong.MaTuVanVien.HasValue)
                {
                    _context.ThongBaos.Add(new ThongBao
                    {
                        MaNguoiDung = phanCong.MaTuVanVien.Value,
                        TieuDe = "Khách hàng đã hủy lịch hẹn xem nhà",
                        NoiDung = $"Khách hàng đã hủy lịch hẹn #{lichHen.MaLichHen} lúc {lichHen.ThoiGianHen:dd/MM/yyyy HH:mm} tại '{lichHen.DiaDiem}'. Lý do: {(string.IsNullOrWhiteSpace(lyDoHuy) ? "Không có" : lyDoHuy)}",
                        LoaiThongBao = "LICH_HEN",
                        DaDoc = false,
                        NgayTao = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Bạn đã hủy lịch hẹn xem nhà thành công.";
            return RedirectToAction(nameof(LichHen));
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

