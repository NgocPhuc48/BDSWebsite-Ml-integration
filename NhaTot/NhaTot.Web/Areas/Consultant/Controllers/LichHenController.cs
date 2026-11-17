using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using NhaTot.Web.Models;

namespace NhaTot.Web.Areas.Consultant.Controllers
{
    [Area("Consultant")]
    [Authorize]
    public class LichHenController : Controller
    {
        private readonly NhaTotDbContext _context;

        public LichHenController(NhaTotDbContext context)
        {
            _context = context;
        }

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(claim, out var id) ? id : 0;
        }

        private async Task<bool> IsConsultantAuthorizedAsync(long userId)
        {
            var user = await _context.NguoiDungs.FindAsync(userId);
            return user != null && (user.VaiTro == 3 || user.VaiTro == 4);
        }

        // GET: /Consultant/LichHen
        public async Task<IActionResult> Index(string status = "ALL")
        {
            long userId = GetCurrentUserId();
            if (!await IsConsultantAuthorizedAsync(userId))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập vào Kênh Tư Vấn Viên!";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            // Lấy danh sách các yêu cầu mà tư vấn viên đang phụ trách
            var myYeuCauIds = await _context.PhanCongTuVans
                .Where(p => p.MaTuVanVien == userId && p.MaYeuCau.HasValue)
                .Select(p => p.MaYeuCau!.Value)
                .ToListAsync();

            // Hiển thị lịch do chính tư vấn viên tạo HOẶC thuộc yêu cầu tư vấn đã phân công cho tư vấn viên HOẶC các lịch hẹn mới từ khách đặt trên tin đăng (chưa phân công hoặc phân công cho mình)
            var query = _context.LichHens
                .Include(l => l.MaYeuCauNavigation)
                    .ThenInclude(y => y!.MaNguoiGuiNavigation)
                .Include(l => l.MaTinNavigation)
                .Where(l => l.MaNguoiTao == userId || (l.MaYeuCau.HasValue && myYeuCauIds.Contains(l.MaYeuCau.Value)) || l.TrangThai == "CHO_XAC_NHAN")
                .AsQueryable();

            // Đếm thống kê
            var allLichHens = await query.ToListAsync();
            int countChoXacNhan = allLichHens.Count(l => l.TrangThai == "CHO_XAC_NHAN");
            int countSapDienRa = allLichHens.Count(l => l.TrangThai == "DA_XAC_NHAN" || l.TrangThai == "SAP_DIEN_RA" || l.TrangThai == "SCHEDULED" || l.TrangThai == "CONFIRMED");
            int countDaXem = allLichHens.Count(l => l.TrangThai == "DA_XEM_NHA" || l.TrangThai == "COMPLETED");
            int countDaHuy = allLichHens.Count(l => l.TrangThai == "DA_HUY" || l.TrangThai == "CANCELLED");

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status) && status != "ALL")
            {
                if (status == "CHO_XAC_NHAN")
                {
                    query = query.Where(l => l.TrangThai == "CHO_XAC_NHAN");
                }
                else if (status == "SAP_DIEN_RA")
                {
                    query = query.Where(l => l.TrangThai == "DA_XAC_NHAN" || l.TrangThai == "SAP_DIEN_RA" || l.TrangThai == "SCHEDULED" || l.TrangThai == "CONFIRMED");
                }
                else if (status == "DA_XEM_NHA")
                {
                    query = query.Where(l => l.TrangThai == "DA_XEM_NHA" || l.TrangThai == "COMPLETED");
                }
                else if (status == "DA_HUY")
                {
                    query = query.Where(l => l.TrangThai == "DA_HUY" || l.TrangThai == "CANCELLED");
                }
                else
                {
                    query = query.Where(l => l.TrangThai == status);
                }
            }

            var list = await query
                .OrderByDescending(l => l.ThoiGianHen)
                .Select(l => new LichHenItemDto
                {
                    MaLichHen = l.MaLichHen,
                    MaYeuCau = l.MaYeuCau,
                    TieuDeYeuCau = l.MaYeuCauNavigation != null ? l.MaYeuCauNavigation.TieuDe : "",
                    TenKhachHang = l.MaYeuCauNavigation != null && l.MaYeuCauNavigation.MaNguoiGuiNavigation != null ? l.MaYeuCauNavigation.MaNguoiGuiNavigation.HoTen : "Khách hàng",
                    SoDienThoaiKhach = l.MaYeuCauNavigation != null && l.MaYeuCauNavigation.MaNguoiGuiNavigation != null ? l.MaYeuCauNavigation.MaNguoiGuiNavigation.SoDienThoai : "",
                    MaTin = l.MaTin,
                    TieuDeTinDang = l.MaTinNavigation != null ? l.MaTinNavigation.TieuDe : "",
                    ThoiGianHen = l.ThoiGianHen,
                    DiaDiem = l.DiaDiem,
                    TrangThai = l.TrangThai ?? "SAP_DIEN_RA",
                    GhiChu = l.GhiChu
                })
                .ToListAsync();

            var vm = new LichHenIndexViewModel
            {
                StatusFilter = status,
                DanhSachLichHen = list,
                SoLuongChoXacNhan = countChoXacNhan,
                SoLuongSapDienRa = countSapDienRa,
                SoLuongDaXem = countDaXem,
                SoLuongDaHuy = countDaHuy
            };

            return View(vm);
        }

        // POST: /Consultant/LichHen/CapNhatTrangThai
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatTrangThai(long maLichHen, string trangThai)
        {
            long userId = GetCurrentUserId();
            if (!await IsConsultantAuthorizedAsync(userId)) return Forbid();

            var lichHen = await _context.LichHens
                .Include(l => l.MaYeuCauNavigation)
                .Include(l => l.MaTinNavigation)
                .FirstOrDefaultAsync(l => l.MaLichHen == maLichHen);

            if (lichHen != null)
            {
                lichHen.TrangThai = trangThai; // CHO_XAC_NHAN, DA_XAC_NHAN, SAP_DIEN_RA, DA_XEM_NHA, DA_HUY

                // Nếu là nhận xử lý hoặc xác nhận lịch hẹn từ yêu cầu tư vấn, gắn thêm phân công nếu chưa có
                if (lichHen.MaYeuCau.HasValue)
                {
                    bool daPhanCong = await _context.PhanCongTuVans.AnyAsync(p => p.MaYeuCau == lichHen.MaYeuCau.Value);
                    if (!daPhanCong)
                    {
                        _context.PhanCongTuVans.Add(new PhanCongTuVan
                        {
                            MaYeuCau = lichHen.MaYeuCau.Value,
                            MaTuVanVien = userId,
                            NgayPhanCong = DateTime.Now,
                            GhiChu = "Tự động gán khi tư vấn viên xử lý lịch hẹn"
                        });

                        var yc = await _context.YeuCauTuVans.FindAsync(lichHen.MaYeuCau.Value);
                        if (yc != null && yc.TrangThai == "CHO_XU_LY")
                        {
                            yc.TrangThai = "DANG_TU_VAN";
                        }
                    }
                }

                // Tự động gửi thông báo cho khách hàng
                long? targetUserId = lichHen.MaYeuCauNavigation?.MaNguoiGui ?? lichHen.MaNguoiTao;
                if (targetUserId.HasValue && targetUserId.Value > 0)
                {
                    string thongBaoText = trangThai switch
                    {
                        "DA_XAC_NHAN" => "Lịch hẹn xem nhà của bạn đã được xác nhận!",
                        "DA_XEM_NHA" => "Lịch hẹn xem nhà đã hoàn thành",
                        "DA_HUY" => "Lịch hẹn xem nhà đã bị hủy",
                        _ => "Cập nhật thông tin lịch hẹn xem nhà"
                    };

                    string thongBaoNoiDung = trangThai switch
                    {
                        "DA_XAC_NHAN" => $"Chuyên viên tư vấn đã xác nhận lịch hẹn xem BĐS #{lichHen.MaTin} lúc {lichHen.ThoiGianHen:dd/MM/yyyy HH:mm} tại '{lichHen.DiaDiem}'. Vui lòng đến đúng giờ hẹn!",
                        "DA_XEM_NHA" => $"Lịch hẹn #{lichHen.MaLichHen} lúc {lichHen.ThoiGianHen:dd/MM/yyyy HH:mm} đã được ghi nhận hoàn thành buổi xem thực địa. Cảm ơn bạn!",
                        "DA_HUY" => $"Lịch hẹn #{lichHen.MaLichHen} lúc {lichHen.ThoiGianHen:dd/MM/yyyy HH:mm} đã được chuyển sang trạng thái HỦY.",
                        _ => $"Lịch hẹn #{lichHen.MaLichHen} lúc {lichHen.ThoiGianHen:dd/MM/yyyy HH:mm} tại '{lichHen.DiaDiem}' vừa được chuyên viên cập nhật trạng thái: {trangThai}."
                    };

                    var tb = new ThongBao
                    {
                        MaNguoiDung = targetUserId.Value,
                        TieuDe = thongBaoText,
                        NoiDung = thongBaoNoiDung,
                        LoaiThongBao = "LICH_HEN",
                        DaDoc = false,
                        NgayTao = DateTime.Now
                    };
                    _context.ThongBaos.Add(tb);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái lịch hẹn #{maLichHen} sang '{trangThai}' và thông báo cho khách hàng thành công!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
