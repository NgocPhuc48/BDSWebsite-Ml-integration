using System;
using System.Collections.Generic;
using System.Globalization;
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
                    .ThenInclude(b => b!.MaLoaiBdsNavigation)
                .Include(t => t.MaBdsNavigation)
                    .ThenInclude(b => b!.MaPhuongXaNavigation)
                        .ThenInclude(p => p!.MaQuanHuyenNavigation)
                            .ThenInclude(q => q!.MaTinhNavigation)
                .Include(t => t.MaBdsNavigation)
                    .ThenInclude(b => b!.ChiTietNhas)
                .Include(t => t.MaBdsNavigation)
                    .ThenInclude(b => b!.ChiTietDats)
                .Include(t => t.MaNguoiDangNavigation)
                .FirstOrDefaultAsync(t => t.MaTin == id);

            if (tinDang == null)
            {
                return NotFound();
            }

            BatDongSan? bds = tinDang.MaBdsNavigation;
            ChiTietNha? chiTietNha = bds?.ChiTietNhas?.FirstOrDefault();
            ChiTietDat? chiTietDat = bds?.ChiTietDats?.FirstOrDefault();

            if (chiTietNha == null && bds != null)
            {
                chiTietNha = await _context.ChiTietNhas.FirstOrDefaultAsync(n => n.MaBds == bds.MaBds);
            }
            if (chiTietDat == null && bds != null)
            {
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
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tindang");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            if (model.HinhAnhFiles != null && model.HinhAnhFiles.Count > 0)
            {
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

            // Xử lý lưu các tập tin ảnh sổ đỏ / sổ hồng
            List<string> soDoPaths = new List<string>();
            if (model.AnhSoDoFiles != null && model.AnhSoDoFiles.Count > 0)
            {
                string sodoFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "sodo");
                if (!Directory.Exists(sodoFolder))
                {
                    Directory.CreateDirectory(sodoFolder);
                }

                foreach (var file in model.AnhSoDoFiles)
                {
                    if (file.Length > 0)
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                        string filePath = Path.Combine(sodoFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        soDoPaths.Add("/uploads/sodo/" + uniqueFileName);
                    }
                }
            }

            // Lấy tọa độ người dùng chọn từ bản đồ hoặc tự định vị, xử lý format và chống tràn số SQL DECIMAL(10, 7)
            decimal viDoDefault = 18.6738m;
            decimal kinhDoDefault = 105.6925m;

            // Đọc trực tiếp từ form để tránh lỗi model binder bị ảnh hưởng bởi Culture (dấu chấm vs dấu phẩy)
            string? rawViDo = Request.Form["ViDo"].FirstOrDefault();
            string? rawKinhDo = Request.Form["KinhDo"].FirstOrDefault();

            decimal? parsedViDo = null;
            if (!string.IsNullOrWhiteSpace(rawViDo))
            {
                string cleanViDo = rawViDo.Trim().Replace(',', '.');
                if (decimal.TryParse(cleanViDo, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                {
                    parsedViDo = v;
                }
            }
            if (!parsedViDo.HasValue && model.ViDo.HasValue)
            {
                parsedViDo = model.ViDo.Value;
            }

            decimal? parsedKinhDo = null;
            if (!string.IsNullOrWhiteSpace(rawKinhDo))
            {
                string cleanKinhDo = rawKinhDo.Trim().Replace(',', '.');
                if (decimal.TryParse(cleanKinhDo, NumberStyles.Any, CultureInfo.InvariantCulture, out var k))
                {
                    parsedKinhDo = k;
                }
            }
            if (!parsedKinhDo.HasValue && model.KinhDo.HasValue)
            {
                parsedKinhDo = model.KinhDo.Value;
            }

            // Kiểm tra khoảng hợp lệ: ViDo từ -90 đến 90, KinhDo từ -180 đến 180 và làm tròn Math.Round(..., 7)
            decimal viDoValue;
            if (parsedViDo.HasValue && parsedViDo.Value >= -90m && parsedViDo.Value <= 90m)
            {
                viDoValue = Math.Round(parsedViDo.Value, 7);
            }
            else
            {
                viDoValue = viDoDefault;
            }

            decimal kinhDoValue;
            if (parsedKinhDo.HasValue && parsedKinhDo.Value >= -180m && parsedKinhDo.Value <= 180m)
            {
                kinhDoValue = Math.Round(parsedKinhDo.Value, 7);
            }
            else
            {
                kinhDoValue = kinhDoDefault;
            }

            // Tạo đối tượng BatDongSan đầy đủ các thuộc tính (Diện tích đất, Diện tích sử dụng, Hướng, Pháp lý, Tên dự án, Địa chỉ mô tả, Tọa độ GIS)
            var bds = new BatDongSan
            {
                MaLoaiBds = model.MaLoaiBds,
                MaPhuongXa = model.MaPhuongXa,
                DiaChiMoTa = model.DiaChiMoTa,
                TenDuAn = model.TenDuAn,
                DienTichDat = model.DienTichDatM2,
                DienTichSuDung = model.DienTichSuDungM2,
                Huong = model.Huong,
                PhapLy = model.PhapLy,
                MoTaDacDiem = model.MoTaDacDiem,
                ViDo = viDoValue,
                KinhDo = kinhDoValue,
                NgayTao = DateTime.Now
            };
            _context.BatDongSans.Add(bds);
            await _context.SaveChangesAsync();

            // Kiểm tra loại BĐS để lưu thông tin tương ứng (Đất hoặc Nhà)
            var loaiBds = await _context.LoaiBds.FindAsync(model.MaLoaiBds);
            bool isDat = loaiBds != null && (loaiBds.TenLoaiBds?.IndexOf("Đất", StringComparison.OrdinalIgnoreCase) >= 0
                                          || loaiBds.TenLoaiBds?.IndexOf("Dat", StringComparison.OrdinalIgnoreCase) >= 0);

            // Lưu thông tin chi tiết nhà (ChiTietNha) nếu không phải thuần đất và có dữ liệu nhập
            if (!isDat && (model.SoTang.HasValue || model.SoPhongNgu.HasValue || model.SoWc.HasValue || model.NamXayDung.HasValue || !string.IsNullOrEmpty(model.TinhTrangNha) || !string.IsNullOrEmpty(model.NoiThat)))
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

            // Lưu thông tin chi tiết đất (ChiTietDat) nếu có dữ liệu đất
            if (!string.IsNullOrEmpty(model.LoaiDat) || model.MatTienM.HasValue || model.ChieuSauM.HasValue || !string.IsNullOrEmpty(model.LoaiDuong) || model.DoRongDuongM.HasValue || !string.IsNullOrEmpty(model.HinhDang))
            {
                var chiTietDat = new ChiTietDat
                {
                    MaBds = bds.MaBds,
                    LoaiDat = model.LoaiDat,
                    MatTienM = model.MatTienM,
                    ChieuSauM = model.ChieuSauM,
                    LoaiDuong = model.LoaiDuong,
                    DoRongDuongM = model.DoRongDuongM,
                    HinhDang = model.HinhDang
                };
                _context.ChiTietDats.Add(chiTietDat);
            }

            // Tạo bản ghi TinDang
            var tinDang = new TinDang
            {
                MaBds = bds.MaBds,
                MaNguoiDang = currentUserId,
                LoaiGiaoDich = model.LoaiGiaoDich ?? "BAN",
                TieuDe = model.TieuDe,
                MoTa = model.MoTa,
                GiaRaoVnd = model.GiaRaoVnd,
                TienCocVnd = model.TienCocVnd,
                KyHanThanhToanThang = model.KyHanThanhToanThang,
                ThoiHanHopDongMinThang = model.ThoiHanHopDongMinThang,
                DanhSachAnhDatDai = imagePaths.Count > 0 ? string.Join(",", imagePaths) : null,
                DanhSachAnhSoDoSoHong = soDoPaths.Count > 0 ? string.Join(",", soDoPaths) : null,
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
                return Json(new { success = false, message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại!" });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var currentUserId) || currentUserId <= 0)
            {
                return Json(new { success = false, requireLogin = true, message = "Vui lòng đăng nhập để đặt lịch hẹn xem nhà." });
            }

            var tinDang = await _context.TinDangs
                .Include(t => t.MaBdsNavigation)
                    .ThenInclude(b => b!.MaPhuongXaNavigation)
                        .ThenInclude(p => p!.MaQuanHuyenNavigation)
                            .ThenInclude(q => q!.MaTinhNavigation)
                .FirstOrDefaultAsync(t => t.MaTin == model.MaTin);

            if (tinDang == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin bất động sản." });
            }

            DateTime thoiGian = model.NgayHen;
            if (!string.IsNullOrEmpty(model.GioHen) && TimeSpan.TryParse(model.GioHen, out var timeSpan))
            {
                thoiGian = thoiGian.Date.Add(timeSpan);
            }

            if (thoiGian < DateTime.Now)
            {
                return Json(new { success = false, message = "Thời gian hẹn phải lớn hơn thời điểm hiện tại!" });
            }

            // Địa điểm gặp: ưu tiên địa điểm nhập vào, nếu trống thì lấy địa chỉ đầy đủ của BĐS
            var bds = tinDang.MaBdsNavigation;
            string diaChiBds = bds != null
                ? string.Join(", ", new[] { bds.DiaChiMoTa, bds.MaPhuongXaNavigation?.TenPhuongXa, bds.MaPhuongXaNavigation?.MaQuanHuyenNavigation?.TenQuanHuyen, bds.MaPhuongXaNavigation?.MaQuanHuyenNavigation?.MaTinhNavigation?.TenTinh }.Where(s => !string.IsNullOrWhiteSpace(s)))
                : $"BĐS #{tinDang.MaTin}";

            string diaDiemGap = !string.IsNullOrWhiteSpace(model.DiaDiem) ? model.DiaDiem.Trim() : diaChiBds;

            // 1. Tự động tạo 1 bản ghi YeuCauTuVan (TrangThai = 'CHO_XU_LY')
            var user = await _context.NguoiDungs.FindAsync(currentUserId);
            var yeuCau = new YeuCauTuVan
            {
                MaNguoiGui = currentUserId,
                TieuDe = $"Khách hẹn xem BĐS #{tinDang.MaTin}: {tinDang.TieuDe}",
                NoiDung = $"Khách hàng {user?.HoTen ?? "Khách"} đặt lịch hẹn xem nhà thực tế lúc {thoiGian:dd/MM/yyyy HH:mm} tại '{diaDiemGap}'. Ghi chú: {(string.IsNullOrWhiteSpace(model.GhiChu) ? "Không có" : model.GhiChu)}",
                MucDoUuTien = 2,
                TrangThai = "CHO_XU_LY",
                NgayTao = DateTime.Now
            };

            _context.YeuCauTuVans.Add(yeuCau);
            await _context.SaveChangesAsync(); // Lưu để lấy MaYeuCau

            // 2. Tự động tạo 1 bản ghi LichHen gắn với MaYeuCau vừa tạo
            var lichHen = new LichHen
            {
                MaYeuCau = yeuCau.MaYeuCau,
                MaTin = model.MaTin,
                MaNguoiTao = currentUserId,
                ThoiGianHen = thoiGian,
                DiaDiem = diaDiemGap,
                GhiChu = model.GhiChu,
                TrangThai = "CHO_XAC_NHAN"
            };

            _context.LichHens.Add(lichHen);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đặt lịch hẹn xem nhà thành công! Yêu cầu của bạn đã được chuyển đến bộ phận Tư vấn viên để sắp xếp và liên hệ xác nhận."
            });
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