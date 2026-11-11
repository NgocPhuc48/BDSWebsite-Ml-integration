using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using NhaTot.Web.Models;

namespace NhaTot.Web.Controllers
{
    public class TinDangController : Controller
    {
        private readonly NhaTotDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TinDangController(NhaTotDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /TinDang/ChiTiet/5
        public async Task<IActionResult> ChiTiet(long id)
        {
            var tinDang = await _context.TinDangs
                .Include(t => t.MaBdsNavigation)
                .Include(t => t.MaNguoiDangNavigation)
                .FirstOrDefaultAsync(t => t.MaTin == id);

            if (tinDang == null)
            {
                return NotFound();
            }

            BatDongSan? bds = tinDang.MaBdsNavigation;
            ChiTietNha? chiTietNha = null;
            ChiTietDat? chiTietDat = null;

            if (bds != null)
            {
                chiTietNha = await _context.ChiTietNhas.FirstOrDefaultAsync(n => n.MaBds == bds.MaBds);
                chiTietDat = await _context.ChiTietDats.FirstOrDefaultAsync(d => d.MaBds == bds.MaBds);
            }

            decimal? giaAi = null;
            decimal? chechLech = null;
            double? phanTramChechLech = null;

            var lsDinhGia = bds != null
                ? await _context.LichSuDinhGias
                    .Where(x => x.MaBds == bds.MaBds)
                    .OrderByDescending(x => x.MaDinhGia)
                    .FirstOrDefaultAsync()
                : null;

            if (lsDinhGia != null && lsDinhGia.GiaDuDoanVnd.HasValue)
            {
                giaAi = lsDinhGia.GiaDuDoanVnd.Value;
                if (tinDang.GiaRaoVnd.HasValue && giaAi > 0)
                {
                    chechLech = tinDang.GiaRaoVnd.Value - giaAi.Value;
                    phanTramChechLech = (double)(Math.Abs(chechLech.Value) / giaAi.Value * 100);
                }
            }

            var tinLienQuan = await _context.TinDangs
                .Where(t => t.MaTin != id && t.TrangThai == "APPROVED")
                .OrderByDescending(t => t.NgayDang)
                .Take(4)
                .Select(t => new TinDangItemDto
                {
                    MaTin = t.MaTin,
                    TieuDe = t.TieuDe ?? "Chưa có tiêu đề",
                    GiaRaoVnd = t.GiaRaoVnd,
                    HinhAnh = t.DanhSachAnhDatDai,
                    AnhDaiDien = t.DanhSachAnhDatDai,
                    NgayDang = t.NgayDang
                })
                .ToListAsync();

            var viewModel = new ChiTietTinDangViewModel
            {
                TinDang = tinDang,
                BatDongSan = bds,
                ChiTietNha = chiTietNha,
                ChiTietDat = chiTietDat,
                NguoiDang = tinDang.MaNguoiDangNavigation,
                GiaDinhGiaAi = giaAi,
                MucChechLechGia = chechLech,
                PhamViChechLechPhanTram = phanTramChechLech,
                DanhSachBdsLienQuan = tinLienQuan
            };

            return View(viewModel);
        }

        // GET: /TinDang/DangTin
        [Authorize]
        public async Task<IActionResult> DangTin()
        {
            ViewBag.DanhSachLoaiBds = await _context.LoaiBds.Where(l => l.TrangThai == true).ToListAsync();
            ViewBag.DanhSachTinhThanh = await _context.TinhThanhs.Where(t => t.TrangThai == true).ToListAsync();
            return View(new DangTinViewModel());
        }

        // POST: /TinDang/DangTin
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DangTin(DangTinViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.DanhSachLoaiBds = await _context.LoaiBds.Where(l => l.TrangThai == true).ToListAsync();
                ViewBag.DanhSachTinhThanh = await _context.TinhThanhs.Where(t => t.TrangThai == true).ToListAsync();
                return View(model);
            }

            // Lấy ID người dùng từ Identity Claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            long currentUserId = long.TryParse(userIdClaim, out var id) ? id : 1;

            // Xử lý lưu các tập tin hình ảnh upload
            List<string> imagePaths = new List<string>();
            if (model.HinhAnhFiles != null && model.HinhAnhFiles.Count > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tindang");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                foreach (var file in model.HinhAnhFiles)
                {
                    if (file.Length > 0)
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        imagePaths.Add("/uploads/tindang/" + uniqueFileName);
                    }
                }
            }

            // Lấy tọa độ từ phường/xã để đồng bộ lên bản đồ GIS
            var phuongXa = await _context.PhuongXas.FindAsync(model.MaPhuongXa);
            decimal viDoDefault = phuongXa?.ViDo ?? 18.6738m;
            decimal kinhDoDefault = phuongXa?.KinhDo ?? 105.6925m;

            // Tạo đối tượng BatDongSan (dùng DienTichDat đúng schema SQL kèm tọa độ GIS)
            var bds = new BatDongSan
            {
                MaLoaiBds = model.MaLoaiBds,
                MaPhuongXa = model.MaPhuongXa,
                DiaChiMoTa = model.DiaChiMoTa,
                DienTichDat = (decimal)model.DienTichDatM2,
                ViDo = viDoDefault,
                KinhDo = kinhDoDefault
            };
            _context.BatDongSans.Add(bds);
            await _context.SaveChangesAsync();

            // Lưu thông tin chi tiết nhà (nếu có)
            if (model.SoTang.HasValue || model.SoPhongNgu.HasValue || model.SoWc.HasValue)
            {
                var chiTietNha = new ChiTietNha
                {
                    MaBds = bds.MaBds,
                    SoTang = model.SoTang,
                    SoPhongNgu = model.SoPhongNgu,
                    SoWc = model.SoWc,
                    NamXayDung = model.NamXayDung,
                    TinhTrangNha = model.TinhTrangNha,
                    NoiThat = model.NoiThat
                };
                _context.ChiTietNhas.Add(chiTietNha);
            }

            // Lưu thông tin chi tiết đất (nếu có)
            if (!string.IsNullOrEmpty(model.LoaiDat) || model.MatTienM.HasValue || model.ChieuSauM.HasValue)
            {
                var chiTietDat = new ChiTietDat
                {
                    MaBds = bds.MaBds,
                    LoaiDat = model.LoaiDat,
                    MatTienM = model.MatTienM.HasValue ? (decimal)model.MatTienM.Value : null,
                    ChieuSauM = model.ChieuSauM.HasValue ? (decimal)model.ChieuSauM.Value : null,
                    LoaiDuong = model.LoaiDuong,
                    DoRongDuongM = model.DoRongDuongM.HasValue ? (decimal)model.DoRongDuongM.Value : null,
                    HinhDang = model.HinhDang
                };
                _context.ChiTietDats.Add(chiTietDat);
            }

            // Tạo bản ghi TinDang
            var tinDang = new TinDang
            {
                MaBds = bds.MaBds,
                MaNguoiDang = currentUserId,
                TieuDe = model.TieuDe,
                MoTa = model.MoTa,
                GiaRaoVnd = model.GiaRaoVnd,
                DanhSachAnhDatDai = string.Join(",", imagePaths),
                NgayDang = DateTime.Now,
                TrangThai = "PENDING"
            };

            _context.TinDangs.Add(tinDang);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng tin thành công! Tin của bạn đang chờ duyệt.";
            return RedirectToAction("ChiTiet", "TinDang", new { id = tinDang.MaTin });
        }

        // POST: /TinDang/DatLichHen
        [HttpPost]
        public async Task<IActionResult> DatLichHen([FromBody] DatLichHenInputModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            long currentUserId = long.TryParse(userIdClaim, out var parsedId) ? parsedId : 1;

            DateTime thoiGian = model.NgayHen;
            if (!string.IsNullOrEmpty(model.GioHen) && TimeSpan.TryParse(model.GioHen, out var timeSpan))
            {
                thoiGian = thoiGian.Date.Add(timeSpan);
            }

            var lichHen = new LichHen
            {
                MaTin = model.MaTin,
                MaNguoiTao = currentUserId,
                ThoiGianHen = thoiGian,
                GhiChu = model.GhiChu,
                TrangThai = "CHO_XAC_NHAN"
            };

            _context.LichHens.Add(lichHen);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đặt lịch hẹn thành công! Bạn có thể theo dõi trong mục Quản lý Lịch hẹn." });
        }

        // POST: /TinDang/GuiYeuCauTuVan
        [HttpPost]
        public async Task<IActionResult> GuiYeuCauTuVan([FromBody] GuiYeuCauTuVanInputModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin." });

            var yeuCau = new YeuCauTuVan
            {
                NoiDung = $"[Yêu cầu tư vấn tin #{model.MaTin} - Họ tên: {model.HoTen} - SĐT: {model.SoDienThoai}]: {model.NoiDung}",
                NgayTao = DateTime.Now,
                TrangThai = "MOI"
            };

            _context.YeuCauTuVans.Add(yeuCau);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Gửi yêu cầu tư vấn thành công!" });
        }

        // POST: /TinDang/BaoCaoViPham
        [HttpPost]
        public async Task<IActionResult> BaoCaoViPham([FromBody] BaoCaoViPhamInputModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Vui lòng chọn lý do báo cáo." });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            long? currentUserId = long.TryParse(userIdClaim, out var parsedId) ? parsedId : (long?)null;

            var baoCao = new BaoCaoTinDang
            {
                MaTin = model.MaTin,
                MaNguoiBaoCao = currentUserId,
                LyDo = model.LyDo,
                MoTa = model.MoTa,
                NgayBaoCao = DateTime.Now,
                TrangThai = "PENDING"
            };

            _context.BaoCaoTinDangs.Add(baoCao);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Gửi báo cáo vi phạm thành công!" });
        }
    }
}