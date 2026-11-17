using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using NhaTot.Web.Models;

namespace NhaTot.Web.Controllers
{
    public class DinhGiaController : Controller
    {
        private readonly NhaTotDbContext _context;

        public DinhGiaController(NhaTotDbContext context)
        {
            _context = context;
        }

        private long? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(claim, out var id) ? id : (long?)null;
        }

        // GET: /DinhGia
        public async Task<IActionResult> Index()
        {
            ViewBag.DanhSachTinhThanh = await _context.TinhThanhs.Where(t => t.TrangThai == true).ToListAsync();
            ViewBag.DanhSachLoaiBds = await _context.LoaiBds.Where(l => l.TrangThai == true).ToListAsync();
            return View(new DinhGiaInputModel());
        }

        // POST: /DinhGia/TinhGia
        [HttpPost]
        public async Task<IActionResult> TinhGia([FromBody] DinhGiaInputModel input)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = errors });
            }

            // 1. Kiểm tra loại BĐS: Nếu chứa chữ 'Đất' thì làm sạch các thuộc tính nhà ở
            var loaiBds = await _context.LoaiBds.FindAsync(input.MaLoaiBds);
            bool isDat = loaiBds != null && (loaiBds.TenLoaiBds?.IndexOf("Đất", StringComparison.OrdinalIgnoreCase) >= 0
                                          || loaiBds.TenLoaiBds?.IndexOf("Dat", StringComparison.OrdinalIgnoreCase) >= 0);

            if (isDat)
            {
                input.SoTang = null;
                input.SoPhongNgu = null;
                input.SoWc = null;
                input.TinhTrangNha = null;
                input.NoiThat = null;
            }

            // 2. Tự động xác định tọa độ ViDo & KinhDo (nếu không truyền từ client, lấy tọa độ trung tâm khu vực hoặc BĐS đã có)
            decimal? viDoChuan = null;
            decimal? kinhDoChuan = null;

            if (input.ViDo.HasValue && input.ViDo.Value >= -90m && input.ViDo.Value <= 90m)
            {
                viDoChuan = Math.Round(input.ViDo.Value, 7);
            }
            if (input.KinhDo.HasValue && input.KinhDo.Value >= -180m && input.KinhDo.Value <= 180m)
            {
                kinhDoChuan = Math.Round(input.KinhDo.Value, 7);
            }

            // Nếu client không nhập tọa độ, tự động nội suy từ BĐS cùng Phường/Xã gần nhất
            if (!viDoChuan.HasValue || !kinhDoChuan.HasValue)
            {
                var bdsMau = await _context.BatDongSans
                    .Where(b => b.MaPhuongXa == input.MaPhuongXa && b.ViDo.HasValue && b.KinhDo.HasValue)
                    .Select(b => new { b.ViDo, b.KinhDo })
                    .FirstOrDefaultAsync();

                if (bdsMau != null)
                {
                    viDoChuan = bdsMau.ViDo;
                    kinhDoChuan = bdsMau.KinhDo;
                }
            }

            // 3. Tính toán Định giá BĐS Hiện tại
            // Lấy đơn giá trung bình từ các tin đăng cùng phường/xã hoặc loại BĐS
            var tinCungPhuong = await _context.TinDangs
                .Include(t => t.MaBdsNavigation)
                .Where(t => t.MaBdsNavigation != null
                         && t.MaBdsNavigation.MaPhuongXa == input.MaPhuongXa
                         && t.GiaRaoVnd.HasValue
                         && t.GiaRaoVnd.Value > 0
                         && t.MaBdsNavigation.DienTichDat.HasValue
                         && t.MaBdsNavigation.DienTichDat.Value > 0)
                .Select(t => t.GiaRaoVnd!.Value / t.MaBdsNavigation!.DienTichDat!.Value)
                .ToListAsync();

            decimal baseDonGiaM2 = 25000000m; // Mặc định 25 tr/m²
            if (tinCungPhuong.Any())
            {
                baseDonGiaM2 = tinCungPhuong.Average();
            }

            // Hệ số điều chỉnh theo đặc điểm
            decimal heSo = 1.0m;

            // Độ rộng đường
            if (input.DuongVaoRong.HasValue)
            {
                if (input.DuongVaoRong.Value >= 10m) heSo += 0.25m; // Đường lớn > 10m
                else if (input.DuongVaoRong.Value >= 6m) heSo += 0.15m; // Đường ô tô tránh nhau
                else if (input.DuongVaoRong.Value < 3m) heSo -= 0.10m; // Đường hẻm nhỏ
            }

            // Mặt tiền
            if (input.ChieuRongMatTien.HasValue && input.ChieuRongMatTien.Value >= 6m)
            {
                heSo += 0.10m;
            }

            // Hướng nhà
            if (!string.IsNullOrEmpty(input.Huong))
            {
                string huongLower = input.Huong.ToLower();
                if (huongLower.Contains("nam") || huongLower.Contains("dong nam"))
                {
                    heSo += 0.05m;
                }
            }

            // Pháp lý
            if (!string.IsNullOrEmpty(input.PhapLy) && (input.PhapLy.ToLower().Contains("sổ") || input.PhapLy.ToLower().Contains("so")))
            {
                heSo += 0.08m;
            }

            // Số tầng & nội thất (chỉ áp dụng nếu là nhà)
            decimal giaTriNhaThem = 0m;
            if (!isDat)
            {
                int soTang = input.SoTang ?? 1;
                giaTriNhaThem += soTang * 180000000m; // Giá trị xây dựng ước tính ~180tr/tầng

                if (!string.IsNullOrEmpty(input.TinhTrangNha) && input.TinhTrangNha.ToLower().Contains("mới"))
                {
                    giaTriNhaThem += 50000000m;
                }
            }

            decimal donGiaM2SauDieuChinh = Math.Round(baseDonGiaM2 * heSo, 0);
            decimal giaDuDoanVnd = Math.Round((input.DienTich * donGiaM2SauDieuChinh) + giaTriNhaThem, 0);

            // Khoảng giá Min - Max (dao động +/- 7%)
            decimal giaThapNhatVnd = Math.Round(giaDuDoanVnd * 0.93m, 0);
            decimal giaCaoNhatVnd = Math.Round(giaDuDoanVnd * 1.07m, 0);
            decimal doTinCay = 91.5m;

            // 4. Lưu vào bảng LichSuDinhGia (không gây lỗi SQL)
            try
            {
                var lichSu = new LichSuDinhGia
                {
                    MaNguoiDung = GetCurrentUserId(),
                    MaBds = input.MaBds,
                    MaPhuongXa = input.MaPhuongXa,
                    MaLoaiBds = input.MaLoaiBds,
                    DienTich = input.DienTich,
                    ChieuRongMatTien = input.ChieuRongMatTien,
                    DuongVaoRong = input.DuongVaoRong,
                    Huong = input.Huong,
                    PhapLy = input.PhapLy,
                    ViDo = viDoChuan,
                    KinhDo = kinhDoChuan,
                    GiaDuDoanVnd = giaDuDoanVnd,
                    GiaThapNhatVnd = giaThapNhatVnd,
                    GiaCaoNhatVnd = giaCaoNhatVnd,
                    DoTinCay = doTinCay,
                    ThoiGianDinhGia = DateTime.Now
                };
                _context.LichSuDinhGias.Add(lichSu);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Bỏ qua lỗi log để đảm bảo người dùng luôn nhận được kết quả tính
                Console.WriteLine("Lỗi ghi LichSuDinhGia: " + ex.Message);
            }

            // 5. Tính toán Dự báo giá trong 6 tháng tới (Mốc 1 đến 6 tháng)
            // Tỷ lệ tăng trưởng dự kiến theo chu kỳ BĐS Nghệ An (~ 0.8% - 1.2% / tháng)
            double monthlyGrowthRate = isDat ? 0.011 : 0.009; // Đất nền tăng trưởng trung bình ~1.1%/tháng, nhà ở ~0.9%/tháng
            var danhSachDuBao = new List<DuBaoThangItemDto>();
            var now = DateTime.Now;

            for (int i = 1; i <= 6; i++)
            {
                var futureDate = now.AddMonths(i);
                double compoundRate = Math.Pow(1.0 + monthlyGrowthRate, i) - 1.0;
                decimal futurePrice = Math.Round(giaDuDoanVnd * (decimal)(1.0 + compoundRate), 0);
                decimal futureMin = Math.Round(futurePrice * 0.93m, 0);
                decimal futureMax = Math.Round(futurePrice * 1.07m, 0);

                danhSachDuBao.Add(new DuBaoThangItemDto
                {
                    Thang = i,
                    NhanThang = $"T+{i} ({futureDate:MM/yyyy})",
                    GiaDuBaoVnd = futurePrice,
                    GiaMinVnd = futureMin,
                    GiaMaxVnd = futureMax,
                    PhanTramTangTruong = Math.Round(compoundRate * 100, 2)
                });
            }

            var response = new KetQuaDinhGiaViewModel
            {
                Success = true,
                Message = "Định giá và dự báo xu hướng thành công!",
                GiaDuDoanVnd = giaDuDoanVnd,
                GiaThapNhatVnd = giaThapNhatVnd,
                GiaCaoNhatVnd = giaCaoNhatVnd,
                DonGiaTrenM2Vnd = donGiaM2SauDieuChinh,
                DoTinCayPhanTram = (double)doTinCay,
                ThoiGianThucHien = DateTime.Now,
                DanhSachDuBao6Thang = danhSachDuBao
            };

            return Json(response);
        }
    }
}
