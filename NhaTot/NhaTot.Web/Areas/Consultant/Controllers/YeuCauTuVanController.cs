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
    public class YeuCauTuVanController : Controller
    {
        private readonly NhaTotDbContext _context;

        public YeuCauTuVanController(NhaTotDbContext context)
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
            // Cho phép Tư vấn viên (VaiTro == 3) hoặc Quản trị viên (VaiTro == 4)
            return user != null && (user.VaiTro == 3 || user.VaiTro == 4);
        }

        // GET: /Consultant/YeuCauTuVan
        public async Task<IActionResult> Index(string tab = "my_tasks")
        {
            long userId = GetCurrentUserId();
            if (!await IsConsultantAuthorizedAsync(userId))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập vào Kênh Tư Vấn Viên!";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var vm = new TuVanIndexViewModel
            {
                ActiveTab = tab
            };

            // 1. Danh sách yêu cầu được phân công cho Tư vấn viên hiện tại
            var myPhanCongs = await _context.PhanCongTuVans
                .Include(p => p.MaYeuCauNavigation)
                    .ThenInclude(y => y!.MaNguoiGuiNavigation)
                .Include(p => p.MaYeuCauNavigation)
                    .ThenInclude(y => y!.MaNhuCauNavigation)
                        .ThenInclude(n => n!.MaLoaiBdsNavigation)
                .Include(p => p.MaYeuCauNavigation)
                    .ThenInclude(y => y!.MaNhuCauNavigation)
                        .ThenInclude(n => n!.MaPhuongXaNavigation)
                            .ThenInclude(px => px!.MaQuanHuyenNavigation)
                .Where(p => p.MaTuVanVien == userId && p.MaYeuCauNavigation != null)
                .OrderByDescending(p => p.NgayPhanCong)
                .ToListAsync();

            vm.DanhSachYeuCauCuaToi = myPhanCongs.Select(p =>
            {
                var y = p.MaYeuCauNavigation!;
                var nc = y.MaNhuCauNavigation;
                string khuVuc = nc?.MaPhuongXaNavigation != null
                    ? $"{nc.MaPhuongXaNavigation.TenPhuongXa}, {nc.MaPhuongXaNavigation.MaQuanHuyenNavigation?.TenQuanHuyen}"
                    : "Toàn tỉnh";

                return new YeuCauTuVanItemDto
                {
                    MaYeuCau = y.MaYeuCau,
                    TieuDe = y.TieuDe ?? $"Yêu cầu tư vấn #{y.MaYeuCau}",
                    NoiDung = y.NoiDung,
                    TrangThai = y.TrangThai ?? "DANG_TU_VAN",
                    MucDoUuTien = y.MucDoUuTien ?? 1,
                    NgayTao = y.NgayTao,
                    TenKhachHang = y.MaNguoiGuiNavigation?.HoTen ?? "Khách hàng",
                    SoDienThoai = y.MaNguoiGuiNavigation?.SoDienThoai,
                    Email = y.MaNguoiGuiNavigation?.Email,
                    TenLoaiBds = nc?.MaLoaiBdsNavigation?.TenLoaiBds ?? "BĐS",
                    KhuVuc = khuVuc,
                    NganSachMinVnd = nc?.NganSachMinVnd,
                    NganSachMaxVnd = nc?.NganSachMaxVnd,
                    NgayPhanCong = p.NgayPhanCong
                };
            }).ToList();

            // 2. Danh sách yêu cầu mới chưa có ai nhận
            var assignedIds = await _context.PhanCongTuVans
                .Where(p => p.MaYeuCau.HasValue)
                .Select(p => p.MaYeuCau!.Value)
                .ToListAsync();

            var availableYeuCaus = await _context.YeuCauTuVans
                .Include(y => y.MaNguoiGuiNavigation)
                .Include(y => y.MaNhuCauNavigation)
                    .ThenInclude(n => n!.MaLoaiBdsNavigation)
                .Include(y => y.MaNhuCauNavigation)
                    .ThenInclude(n => n!.MaPhuongXaNavigation)
                        .ThenInclude(px => px!.MaQuanHuyenNavigation)
                .Where(y => !assignedIds.Contains(y.MaYeuCau) && y.TrangThai != "DA_DONG" && y.TrangThai != "CLOSED")
                .OrderByDescending(y => y.NgayTao)
                .ToListAsync();

            vm.DanhSachYeuCauChuaNhan = availableYeuCaus.Select(y =>
            {
                var nc = y.MaNhuCauNavigation;
                string khuVuc = nc?.MaPhuongXaNavigation != null
                    ? $"{nc.MaPhuongXaNavigation.TenPhuongXa}, {nc.MaPhuongXaNavigation.MaQuanHuyenNavigation?.TenQuanHuyen}"
                    : "Toàn tỉnh";

                return new YeuCauTuVanItemDto
                {
                    MaYeuCau = y.MaYeuCau,
                    TieuDe = y.TieuDe ?? $"Yêu cầu tư vấn #{y.MaYeuCau}",
                    NoiDung = y.NoiDung,
                    TrangThai = y.TrangThai ?? "CHO_TIEP_NHAN",
                    MucDoUuTien = y.MucDoUuTien ?? 1,
                    NgayTao = y.NgayTao,
                    TenKhachHang = y.MaNguoiGuiNavigation?.HoTen ?? "Khách hàng",
                    SoDienThoai = y.MaNguoiGuiNavigation?.SoDienThoai,
                    Email = y.MaNguoiGuiNavigation?.Email,
                    TenLoaiBds = nc?.MaLoaiBdsNavigation?.TenLoaiBds ?? "BĐS",
                    KhuVuc = khuVuc,
                    NganSachMinVnd = nc?.NganSachMinVnd,
                    NganSachMaxVnd = nc?.NganSachMaxVnd
                };
            }).ToList();

            // 3. Danh sách lịch hẹn
            var myYeuCauIds = vm.DanhSachYeuCauCuaToi.Select(x => x.MaYeuCau).ToList();
            var lichHens = await _context.LichHens
                .Include(l => l.MaYeuCauNavigation)
                    .ThenInclude(y => y!.MaNguoiGuiNavigation)
                .Include(l => l.MaTinNavigation)
                .Where(l => l.MaNguoiTao == userId || (l.MaYeuCau.HasValue && myYeuCauIds.Contains(l.MaYeuCau.Value)))
                .OrderByDescending(l => l.ThoiGianHen)
                .ToListAsync();

            vm.DanhSachLichHen = lichHens.Select(l => new LichHenItemDto
            {
                MaLichHen = l.MaLichHen,
                MaYeuCau = l.MaYeuCau,
                TieuDeYeuCau = l.MaYeuCauNavigation?.TieuDe,
                TenKhachHang = l.MaYeuCauNavigation?.MaNguoiGuiNavigation?.HoTen ?? "Khách hàng",
                SoDienThoaiKhach = l.MaYeuCauNavigation?.MaNguoiGuiNavigation?.SoDienThoai,
                MaTin = l.MaTin,
                TieuDeTinDang = l.MaTinNavigation?.TieuDe,
                ThoiGianHen = l.ThoiGianHen,
                DiaDiem = l.DiaDiem,
                TrangThai = l.TrangThai ?? "SCHEDULED",
                GhiChu = l.GhiChu
            }).ToList();

            return View(vm);
        }

        // POST: /Consultant/YeuCauTuVan/NhanTuVan/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NhanTuVan(long id)
        {
            long userId = GetCurrentUserId();
            if (!await IsConsultantAuthorizedAsync(userId))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền thực hiện thao tác này!";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var yeuCau = await _context.YeuCauTuVans
                .Include(y => y.MaNguoiGuiNavigation)
                .FirstOrDefaultAsync(y => y.MaYeuCau == id);

            if (yeuCau == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu tư vấn!";
                return RedirectToAction("Index");
            }

            var existing = await _context.PhanCongTuVans.FirstOrDefaultAsync(p => p.MaYeuCau == id);
            if (existing != null)
            {
                TempData["ErrorMessage"] = "Yêu cầu tư vấn này đã có tư vấn viên khác tiếp nhận!";
                return RedirectToAction("Index");
            }

            var now = DateTime.Now;
            var phanCong = new PhanCongTuVan
            {
                MaYeuCau = id,
                MaTuVanVien = userId,
                MaNguoiPhanCong = userId,
                NgayPhanCong = now,
                GhiChu = "Tư vấn viên chủ động tiếp nhận xử lý từ Kênh Tư Vấn Viên"
            };
            _context.PhanCongTuVans.Add(phanCong);

            yeuCau.TrangThai = "DANG_TU_VAN";

            // Tự động lưu thông báo vào bảng ThongBao gửi cho khách hàng
            if (yeuCau.MaNguoiGui.HasValue)
            {
                var me = await _context.NguoiDungs.FindAsync(userId);
                var tb = new ThongBao
                {
                    MaNguoiDung = yeuCau.MaNguoiGui.Value,
                    TieuDe = "Tư vấn viên đã tiếp nhận yêu cầu của bạn",
                    NoiDung = $"Chuyên viên tư vấn {me?.HoTen ?? "NhaTot"} ({me?.SoDienThoai}) đã tiếp nhận yêu cầu tư vấn #{id} và sẽ sớm liên hệ hỗ trợ bạn.",
                    LoaiThongBao = "TU_VAN",
                    DaDoc = false,
                    NgayTao = now
                };
                _context.ThongBaos.Add(tb);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã tiếp nhận yêu cầu tư vấn #{id} thành công!";
            return RedirectToAction("ChiTiet", new { id });
        }

        // GET: /Consultant/YeuCauTuVan/ChiTiet/5
        public async Task<IActionResult> ChiTiet(long id)
        {
            long userId = GetCurrentUserId();
            if (!await IsConsultantAuthorizedAsync(userId))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập!";
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            var yeuCau = await _context.YeuCauTuVans
                .Include(y => y.MaNguoiGuiNavigation)
                .Include(y => y.MaNhuCauNavigation)
                    .ThenInclude(n => n!.MaLoaiBdsNavigation)
                .Include(y => y.MaNhuCauNavigation)
                    .ThenInclude(n => n!.MaPhuongXaNavigation)
                        .ThenInclude(px => px!.MaQuanHuyenNavigation)
                .FirstOrDefaultAsync(y => y.MaYeuCau == id);

            if (yeuCau == null)
            {
                return NotFound();
            }

            var phanCong = await _context.PhanCongTuVans
                .Include(p => p.MaTuVanVienNavigation)
                .FirstOrDefaultAsync(p => p.MaYeuCau == id);

            var lichHens = await _context.LichHens
                .Include(l => l.MaTinNavigation)
                .Where(l => l.MaYeuCau == id)
                .OrderByDescending(l => l.ThoiGianHen)
                .ToListAsync();

            // Lấy danh sách tin đăng gợi ý phù hợp nhất (tối đa 5 tin) từ bảng TinDang
            var nc = yeuCau.MaNhuCauNavigation;
            var tinDangQuery = _context.TinDangs
                .Include(t => t.MaBdsNavigation)
                    .ThenInclude(b => b!.MaLoaiBdsNavigation)
                .Include(t => t.MaBdsNavigation)
                    .ThenInclude(b => b!.MaPhuongXaNavigation)
                        .ThenInclude(px => px!.MaQuanHuyenNavigation)
                .Where(t => t.TrangThai == "APPROVED");

            if (nc != null)
            {
                // Khớp Loại BĐS
                if (nc.MaLoaiBds.HasValue && nc.MaLoaiBds.Value > 0)
                {
                    tinDangQuery = tinDangQuery.Where(t => t.MaBdsNavigation != null && t.MaBdsNavigation.MaLoaiBds == nc.MaLoaiBds);
                }
                // Khớp Khu vực: ưu tiên Phường/Xã hoặc Quận/Huyện
                if (nc.MaPhuongXa.HasValue && nc.MaPhuongXa.Value > 0)
                {
                    tinDangQuery = tinDangQuery.Where(t => t.MaBdsNavigation != null && t.MaBdsNavigation.MaPhuongXa == nc.MaPhuongXa);
                }
                else if (nc.MaPhuongXaNavigation?.MaQuanHuyen.HasValue == true)
                {
                    int qId = nc.MaPhuongXaNavigation.MaQuanHuyen.Value;
                    tinDangQuery = tinDangQuery.Where(t => t.MaBdsNavigation != null && t.MaBdsNavigation.MaPhuongXaNavigation != null && t.MaBdsNavigation.MaPhuongXaNavigation.MaQuanHuyen == qId);
                }
                // Khớp Ngân sách (Min - Max)
                if (nc.NganSachMaxVnd.HasValue && nc.NganSachMaxVnd.Value > 0)
                {
                    decimal maxBudget = nc.NganSachMaxVnd.Value * 1.15m;
                    tinDangQuery = tinDangQuery.Where(t => !t.GiaRaoVnd.HasValue || t.GiaRaoVnd <= maxBudget);
                }
                if (nc.NganSachMinVnd.HasValue && nc.NganSachMinVnd.Value > 0)
                {
                    tinDangQuery = tinDangQuery.Where(t => !t.GiaRaoVnd.HasValue || t.GiaRaoVnd >= nc.NganSachMinVnd.Value * 0.85m);
                }
            }

            var tinGoiYs = await tinDangQuery
                .OrderByDescending(t => t.NgayDang)
                .Take(5) // Tối đa 5 tin phù hợp nhất
                .Select(t => new TinDangGoiYDto
                {
                    MaTin = t.MaTin,
                    TieuDe = t.TieuDe ?? "Bất động sản chính chủ",
                    GiaRaoVnd = t.GiaRaoVnd,
                    DienTichDat = t.MaBdsNavigation != null ? t.MaBdsNavigation.DienTichDat : null,
                    DiaChi = t.MaBdsNavigation != null ? (t.MaBdsNavigation.DiaChiMoTa ?? (t.MaBdsNavigation.MaPhuongXaNavigation != null ? t.MaBdsNavigation.MaPhuongXaNavigation.TenPhuongXa : "")) : "",
                    TenLoaiBds = t.MaBdsNavigation != null ? t.MaBdsNavigation.MaLoaiBdsNavigation!.TenLoaiBds : "",
                    AnhDaiDien = t.DanhSachAnhDatDai,
                    Huong = t.MaBdsNavigation != null ? t.MaBdsNavigation.Huong : ""
                })
                .ToListAsync();

            var vm = new ChiTietYeuCauTuVanViewModel
            {
                YeuCau = yeuCau,
                KhachHang = yeuCau.MaNguoiGuiNavigation,
                NhuCau = nc,
                PhanCongHienTai = phanCong,
                DanhSachLichHen = lichHens,
                DanhSachTinDangGoiY = tinGoiYs,
                LaTuVanVienPhuTrach = phanCong?.MaTuVanVien == userId || phanCong == null
            };

            return View(vm);
        }

        // POST: /Consultant/YeuCauTuVan/CapNhatTrangThai
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatTrangThai(long id, string trangThai)
        {
            long userId = GetCurrentUserId();
            if (!await IsConsultantAuthorizedAsync(userId)) return Forbid();

            var yeuCau = await _context.YeuCauTuVans
                .Include(y => y.MaNguoiGuiNavigation)
                .FirstOrDefaultAsync(y => y.MaYeuCau == id);

            if (yeuCau != null)
            {
                yeuCau.TrangThai = trangThai; // DANG_TU_VAN, DA_HOAN_THANH, DA_DONG
                if (trangThai == "DA_HOAN_THANH" || trangThai == "DA_DONG" || trangThai == "CLOSED" || trangThai == "COMPLETED")
                {
                    yeuCau.NgayDong = DateTime.Now;
                }

                // Gửi thông báo cho khách hàng khi thay đổi trạng thái
                if (yeuCau.MaNguoiGui.HasValue)
                {
                    string tieuDeThongBao = trangThai switch
                    {
                        "DANG_TU_VAN" => "Yêu cầu tư vấn của bạn đang được xử lý",
                        "DA_HOAN_THANH" => "Yêu cầu tư vấn của bạn đã hoàn thành",
                        "DA_DONG" => "Yêu cầu tư vấn của bạn đã được đóng",
                        _ => "Cập nhật tiến độ yêu cầu tư vấn"
                    };

                    var tb = new ThongBao
                    {
                        MaNguoiDung = yeuCau.MaNguoiGui.Value,
                        TieuDe = tieuDeThongBao,
                        NoiDung = $"Yêu cầu #{yeuCau.MaYeuCau} '{yeuCau.TieuDe}' vừa được chuyên viên cập nhật trạng thái: {trangThai}.",
                        LoaiThongBao = "TU_VAN",
                        DaDoc = false,
                        NgayTao = DateTime.Now
                    };
                    _context.ThongBaos.Add(tb);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái yêu cầu #{id} sang '{trangThai}'!";
            }

            return RedirectToAction("ChiTiet", new { id });
        }

        // POST: /Consultant/YeuCauTuVan/TaoLichHen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoLichHen(TaoLichHenInputModel input)
        {
            long userId = GetCurrentUserId();
            if (!await IsConsultantAuthorizedAsync(userId)) return Forbid();

            if (input.ThoiGianHen < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Thời gian hẹn phải lớn hơn thời điểm hiện tại!";
                return RedirectToAction("ChiTiet", new { id = input.MaYeuCau });
            }

            var yeuCau = await _context.YeuCauTuVans
                .Include(y => y.MaNguoiGuiNavigation)
                .FirstOrDefaultAsync(y => y.MaYeuCau == input.MaYeuCau);

            if (yeuCau == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu tư vấn!";
                return RedirectToAction("Index");
            }

            var now = DateTime.Now;
            var lichHen = new LichHen
            {
                MaYeuCau = input.MaYeuCau,
                MaTin = input.MaTin > 0 ? input.MaTin : null,
                MaNguoiTao = userId,
                ThoiGianHen = input.ThoiGianHen,
                DiaDiem = input.DiaDiem,
                TrangThai = "SAP_DIEN_RA",
                GhiChu = input.GhiChu
            };
            _context.LichHens.Add(lichHen);

            // Tự động tạo Thông báo gửi cho khách hàng
            if (yeuCau.MaNguoiGui.HasValue)
            {
                var me = await _context.NguoiDungs.FindAsync(userId);
                var tb = new ThongBao
                {
                    MaNguoiDung = yeuCau.MaNguoiGui.Value,
                    TieuDe = "Bạn có lịch hẹn xem bất động sản mới",
                    NoiDung = $"Chuyên viên {me?.HoTen ?? "Tư vấn viên"} đã sắp xếp lịch hẹn xem BĐS lúc {input.ThoiGianHen:dd/MM/yyyy HH:mm} tại '{input.DiaDiem}'. Vui lòng kiểm tra lịch hẹn!",
                    LoaiThongBao = "LICH_HEN",
                    DaDoc = false,
                    NgayTao = now
                };
                _context.ThongBaos.Add(tb);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thiết lập lịch hẹn dẫn khách xem BĐS thành công!";
            return RedirectToAction("ChiTiet", new { id = input.MaYeuCau });
        }

        // POST: /Consultant/YeuCauTuVan/CapNhatLichHen
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatLichHen(long maLichHen, string trangThai, long? returnYeuCauId)
        {
            long userId = GetCurrentUserId();
            if (!await IsConsultantAuthorizedAsync(userId)) return Forbid();

            var lichHen = await _context.LichHens
                .Include(l => l.MaYeuCauNavigation)
                .FirstOrDefaultAsync(l => l.MaLichHen == maLichHen);

            if (lichHen != null)
            {
                lichHen.TrangThai = trangThai; // SAP_DIEN_RA, DA_XEM_NHA, DA_HUY (hoặc SCHEDULED, COMPLETED, CANCELLED)
                
                if ((trangThai == "DA_HUY" || trangThai == "CANCELLED") && lichHen.MaYeuCauNavigation?.MaNguoiGui.HasValue == true)
                {
                    var tb = new ThongBao
                    {
                        MaNguoiDung = lichHen.MaYeuCauNavigation.MaNguoiGui.Value,
                        TieuDe = "Lịch hẹn xem nhà đã bị hủy",
                        NoiDung = $"Lịch hẹn lúc {lichHen.ThoiGianHen:dd/MM/yyyy HH:mm} tại {lichHen.DiaDiem} đã được tư vấn viên cập nhật trạng thái HỦY.",
                        LoaiThongBao = "LICH_HEN",
                        DaDoc = false,
                        NgayTao = DateTime.Now
                    };
                    _context.ThongBaos.Add(tb);
                }
                else if ((trangThai == "DA_XEM_NHA" || trangThai == "COMPLETED") && lichHen.MaYeuCauNavigation?.MaNguoiGui.HasValue == true)
                {
                    var tb = new ThongBao
                    {
                        MaNguoiDung = lichHen.MaYeuCauNavigation.MaNguoiGui.Value,
                        TieuDe = "Lịch hẹn xem nhà đã hoàn thành",
                        NoiDung = $"Lịch hẹn lúc {lichHen.ThoiGianHen:dd/MM/yyyy HH:mm} tại {lichHen.DiaDiem} đã hoàn thành. Cảm ơn quý khách!",
                        LoaiThongBao = "LICH_HEN",
                        DaDoc = false,
                        NgayTao = DateTime.Now
                    };
                    _context.ThongBaos.Add(tb);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái lịch hẹn #{maLichHen}!";
            }

            if (returnYeuCauId.HasValue && returnYeuCauId.Value > 0)
            {
                return RedirectToAction("ChiTiet", new { id = returnYeuCauId.Value });
            }

            return RedirectToAction("Index", new { tab = "schedule" });
        }
    }
}
