using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using NhaTot.Web.Areas.Admin.Models;

namespace NhaTot.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BayGiaController : Controller
    {
        private readonly NhaTotDbContext _context;

        public BayGiaController(NhaTotDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/BayGia
        public async Task<IActionResult> Index(string? trangThai = null)
        {
            var query = _context.MucTieuTheoDoiGias
                .Include(m => m.MaNguoiDungNavigation)
                .Include(m => m.MaLoaiBdsNavigation)
                .Include(m => m.MaQuanHuyenNavigation)
                .Include(m => m.MaPhuongXaNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(trangThai) && trangThai != "ALL")
            {
                query = query.Where(m => m.TrangThai == trangThai);
            }

            var mucTieus = await query
                .OrderByDescending(m => m.NgayTao)
                .ToListAsync();

            var now = DateTime.Now;

            // Load ví của tất cả người dùng liên quan
            var userIds = mucTieus.Where(m => m.MaNguoiDung.HasValue).Select(m => m.MaNguoiDung!.Value).Distinct().ToList();
            var viList = await _context.ViTaiKhoans.Where(v => v.MaNguoiDung.HasValue && userIds.Contains(v.MaNguoiDung.Value)).ToListAsync();

            var items = new List<AdminMucTieuItemDto>();
            foreach (var m in mucTieus)
            {
                var vi = viList.FirstOrDefault(v => v.MaNguoiDung == m.MaNguoiDung);

                // Khu vực hiển thị
                string kv = "Nghệ An";
                if (m.MaPhuongXaNavigation != null)
                {
                    kv = $"{m.MaPhuongXaNavigation.TenPhuongXa}, {m.MaQuanHuyenNavigation?.TenQuanHuyen}";
                }
                else if (m.MaQuanHuyenNavigation != null)
                {
                    kv = m.MaQuanHuyenNavigation.TenQuanHuyen ?? "Nghệ An";
                }

                // Đếm số lượng tin khớp hiện có
                int soTinKhop = await DemSoTinKhopAsync(m.MaQuanHuyen, m.MaPhuongXa, m.MaLoaiBds, m.LoaiGiaoDich, m.GiaMongMuonVnd);

                items.Add(new AdminMucTieuItemDto
                {
                    MaMucTieu = m.MaMucTieu,
                    MaNguoiDung = m.MaNguoiDung,
                    TenNguoiDung = m.MaNguoiDungNavigation?.HoTen ?? "Người dùng #" + m.MaNguoiDung,
                    SoDienThoai = m.MaNguoiDungNavigation?.SoDienThoai ?? "---",
                    Email = m.MaNguoiDungNavigation?.Email ?? "---",
                    SoDuViHienTai = vi?.SoDuDiem ?? 0,
                    TenKhuVuc = kv,
                    TenLoaiBds = m.MaLoaiBdsNavigation?.TenLoaiBds ?? "Bất động sản",
                    LoaiGiaoDich = m.LoaiGiaoDich ?? "BAN",
                    GiaMongMuonVnd = m.GiaMongMuonVnd,
                    TienCocDiem = m.TienCocDiem ?? 100000,
                    TrangThai = m.TrangThai ?? "DANG_THEO_DOI",
                    NgayTao = m.NgayTao,
                    NgayHetHan = m.NgayHetHan,
                    DaQuaHan = m.NgayHetHan.HasValue && now >= m.NgayHetHan.Value,
                    SoTinKhop = soTinKhop
                });
            }

            // Thống kê nhanh toàn hệ thống
            var allTargets = await _context.MucTieuTheoDoiGias.ToListAsync();

            var viewModel = new AdminBayGiaViewModel
            {
                FilterTrangThai = trangThai ?? "ALL",
                TongMucTieuDangChay = allTargets.Count(m => m.TrangThai == "DANG_THEO_DOI" || m.TrangThai == "ACTIVE"),
                TongDiemTamGiuCoc = allTargets.Where(m => m.TrangThai == "DANG_THEO_DOI" || m.TrangThai == "ACTIVE").Sum(m => m.TienCocDiem ?? 100000),
                TongMucTieuDaKhop = allTargets.Count(m => m.TrangThai == "DA_KHOP_LENH"),
                TongMucTieuDaHoanTien = allTargets.Count(m => m.TrangThai == "DA_HOAN_TIEN"),
                DanhSachMucTieu = items
            };

            return View(viewModel);
        }

        // POST: /Admin/BayGia/ChayDoiSoatVaHoanCoc
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChayDoiSoatVaHoanCoc()
        {
            var now = DateTime.Now;

            // Tìm các mục tiêu đang theo dõi mà đã quá NgayHetHan
            var targetsQuaHan = await _context.MucTieuTheoDoiGias
                .Where(m => (m.TrangThai == "DANG_THEO_DOI" || m.TrangThai == "ACTIVE") && m.NgayHetHan.HasValue && m.NgayHetHan.Value <= now)
                .ToListAsync();

            if (!targetsQuaHan.Any())
            {
                TempData["InfoMessage"] = "Hiện tại không có mục tiêu nào quá hạn cần hoàn cọc hoặc khớp lệnh.";
                return RedirectToAction("Index");
            }

            int soMucTieuHoanCoc = 0;
            decimal tongDiemHoan = 0;
            int soMucTieuKhopLenh = 0;

            foreach (var m in targetsQuaHan)
            {
                int soTinKhop = await DemSoTinKhopAsync(m.MaQuanHuyen, m.MaPhuongXa, m.MaLoaiBds, m.LoaiGiaoDich, m.GiaMongMuonVnd);

                if (soTinKhop == 0)
                {
                    // Hoàn lại tiền cọc cho người dùng
                    if (m.MaNguoiDung.HasValue)
                    {
                        var vi = await _context.ViTaiKhoans.FirstOrDefaultAsync(v => v.MaNguoiDung == m.MaNguoiDung.Value);
                        if (vi == null)
                        {
                            vi = new ViTaiKhoan
                            {
                                MaNguoiDung = m.MaNguoiDung.Value,
                                SoDuDiem = 0,
                                TrangThai = "ACTIVE",
                                NgayTao = now,
                                NgayCapNhat = now
                            };
                            _context.ViTaiKhoans.Add(vi);
                        }

                        decimal diemHoan = m.TienCocDiem ?? 100000m;
                        vi.SoDuDiem = (vi.SoDuDiem ?? 0) + diemHoan;
                        vi.NgayCapNhat = now;

                        var gdHoan = new LichSuGiaoDich
                        {
                            MaVi = vi.MaVi,
                            LoaiGiaoDich = "HOAN_COC",
                            SoDiem = diemHoan,
                            SoDuSauGiaoDich = vi.SoDuDiem,
                            MoTa = $"Admin chạy đối soát: Hoàn tiền cọc mục tiêu Bẫy Giá #{m.MaMucTieu} do quá hạn 1 tháng không có tin khớp (+{diemHoan:N0} đ)",
                            NgayTao = now
                        };
                        _context.LichSuGiaoDiches.Add(gdHoan);

                        tongDiemHoan += diemHoan;
                        soMucTieuHoanCoc++;
                    }

                    m.TrangThai = "DA_HOAN_TIEN";
                }
                else
                {
                    // Đã có tin khớp lệnh -> Không hoàn tiền
                    m.TrangThai = "DA_KHOP_LENH";
                    soMucTieuKhopLenh++;
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Chạy đối soát thành công! Đã hoàn cọc cho {soMucTieuHoanCoc} mục tiêu (tổng hoàn {tongDiemHoan:N0} điểm) và xác nhận khớp lệnh cho {soMucTieuKhopLenh} mục tiêu.";
            return RedirectToAction("Index");
        }

        // Đếm số lượng tin khớp (TinDang + LeadMXH)
        private async Task<int> DemSoTinKhopAsync(int? maQuanHuyen, int? maPhuongXa, int? maLoaiBds, string? loaiGiaoDich, decimal? maxGia)
        {
            decimal? nguongGia = maxGia.HasValue && maxGia.Value > 0 ? maxGia.Value * 1.1m : null;

            // 1. TinDang
            var tinDangQuery = _context.TinDangs
                .Include(t => t.MaBdsNavigation)
                    .ThenInclude(b => b!.MaPhuongXaNavigation)
                .Where(t => t.TrangThai == "APPROVED");

            if (!string.IsNullOrEmpty(loaiGiaoDich))
                tinDangQuery = tinDangQuery.Where(t => t.LoaiGiaoDich == loaiGiaoDich);

            if (maLoaiBds.HasValue && maLoaiBds > 0)
                tinDangQuery = tinDangQuery.Where(t => t.MaBdsNavigation != null && t.MaBdsNavigation.MaLoaiBds == maLoaiBds);

            if (maPhuongXa.HasValue && maPhuongXa > 0)
                tinDangQuery = tinDangQuery.Where(t => t.MaBdsNavigation != null && t.MaBdsNavigation.MaPhuongXa == maPhuongXa);
            else if (maQuanHuyen.HasValue && maQuanHuyen > 0)
                tinDangQuery = tinDangQuery.Where(t => t.MaBdsNavigation != null && t.MaBdsNavigation.MaPhuongXaNavigation != null && t.MaBdsNavigation.MaPhuongXaNavigation.MaQuanHuyen == maQuanHuyen);

            if (nguongGia.HasValue)
            {
                tinDangQuery = tinDangQuery.Where(t => !t.GiaRaoVnd.HasValue || t.GiaRaoVnd <= 0 || t.GiaRaoVnd <= nguongGia.Value);
            }

            int countTinDang = await tinDangQuery.CountAsync();

            // 2. LeadMXH
            var leadQuery = _context.LeadKhachHangNhuCaus
                .Include(l => l.MaDuLieuNavigation)
                .Include(l => l.MaPhuongXaNavigation)
                .Where(l => l.MaDuLieuNavigation == null || l.MaDuLieuNavigation.DuLieuGocJson == null)
                .AsQueryable();

            if (!string.IsNullOrEmpty(loaiGiaoDich))
                leadQuery = leadQuery.Where(l => l.LoaiGiaoDich == loaiGiaoDich || l.LoaiGiaoDich == null);

            if (maPhuongXa.HasValue && maPhuongXa > 0)
                leadQuery = leadQuery.Where(l => l.MaPhuongXa == maPhuongXa);
            else if (maQuanHuyen.HasValue && maQuanHuyen > 0)
                leadQuery = leadQuery.Where(l => l.MaPhuongXaNavigation != null && l.MaPhuongXaNavigation.MaQuanHuyen == maQuanHuyen);

            if (nguongGia.HasValue)
            {
                leadQuery = leadQuery.Where(l => l.MaDuLieuNavigation == null || !l.MaDuLieuNavigation.GiaRaoVnd.HasValue || l.MaDuLieuNavigation.GiaRaoVnd <= 0 || l.MaDuLieuNavigation.GiaRaoVnd <= nguongGia.Value);
            }

            int countLead = await leadQuery.CountAsync();

            return countTinDang + countLead;
        }
    }
}
