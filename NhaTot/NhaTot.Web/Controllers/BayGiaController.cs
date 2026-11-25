using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using NhaTot.Web.Models;

namespace NhaTot.Web.Controllers
{
    [Authorize]
    public class BayGiaController : Controller
    {
        private readonly NhaTotDbContext _context;
        private readonly NhaTot.Web.Services.CrawlSuggestionService _suggestions;
        private const decimal TIEN_COC_BAY_GIA = 100000m; // 100.000 đ tạm giữ cọc cho mỗi mục tiêu theo dõi bẫy giá

        public BayGiaController(NhaTotDbContext context, NhaTot.Web.Services.CrawlSuggestionService suggestions)
        {
            _context = context;
            _suggestions = suggestions;
        }

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(claim, out var id) ? id : 0;
        }

        // GET: /BayGia
        public async Task<IActionResult> Index(long? targetId = null)
        {
            long userId = GetCurrentUserId();

            // Lấy thông tin ví hoặc tạo mới nếu chưa có
            var vi = await LayHoacTaoViAsync(userId);
            decimal soDu = vi.SoDuDiem ?? 0;

            // Load danh sách danh mục hỗ trợ cascade chọn
            ViewBag.DanhSachTinhThanh = await _context.TinhThanhs.Where(t => t.TrangThai == true).ToListAsync();
            ViewBag.DanhSachLoaiBds = await _context.LoaiBds.Where(l => l.TrangThai == true).ToListAsync();

            // Lấy danh sách mục tiêu bẫy giá của người dùng
            var mucTieus = await _context.MucTieuTheoDoiGias
                .Include(m => m.MaQuanHuyenNavigation)
                .Include(m => m.MaPhuongXaNavigation)
                .Include(m => m.MaLoaiBdsNavigation)
                .Where(m => m.MaNguoiDung == userId)
                .OrderByDescending(m => m.NgayTao)
                .ToListAsync();

            // Lấy các NhuCauTimKiem của user
            var nhuCaus = await _context.NhuCauTimKiems
                .Where(n => n.MaNguoiDung == userId)
                .OrderByDescending(n => n.NgayTao)
                .ToListAsync();

            var viewModel = new BayGiaIndexViewModel
            {
                SoDuViHienTai = soDu,
                MucTieuDangXemId = targetId,
                DanhSachMucTieu = new List<MucTieuTheoDoiChiTietDto>()
            };

            bool coThayDoiCsDl = false;
            var now = DateTime.Now;

            foreach (var m in mucTieus)
            {
                // Tìm NhuCauTimKiem tương ứng theo MaLoaiBds, MaPhuongXa và xấp xỉ thời gian tạo
                var nc = nhuCaus
                    .Where(n => n.MaLoaiBds == m.MaLoaiBds && 
                                (n.MaPhuongXa == m.MaPhuongXa || (!n.MaPhuongXa.HasValue && !m.MaPhuongXa.HasValue)))
                    .OrderBy(n => Math.Abs(((n.NgayTao ?? m.NgayTao ?? now) - (m.NgayTao ?? now)).TotalSeconds))
                    .FirstOrDefault();

                decimal? minGia = nc?.NganSachMinVnd;
                decimal maxGia = m.GiaMongMuonVnd ?? nc?.NganSachMaxVnd ?? 0;

                // Parse tiêu chí chi tiết từ TieuChiThem
                var tieuChiDetail = new TieuChiBayGiaDetailDto();
                var moTaList = new List<string>();

                if (nc?.DienTichMin.HasValue == true)
                {
                    tieuChiDetail.DienTichMin = nc.DienTichMin;
                    moTaList.Add($"Diện tích ≥ {nc.DienTichMin:N0} m²");
                }

                if (nc != null && !string.IsNullOrEmpty(nc.TieuChiThem))
                {
                    try
                    {
                        var parsed = JsonSerializer.Deserialize<TieuChiBayGiaDetailDto>(nc.TieuChiThem, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (parsed != null)
                        {
                            tieuChiDetail = parsed;
                            if (tieuChiDetail.DienTichMin.HasValue && !moTaList.Any(x => x.Contains("Diện tích")))
                            {
                                moTaList.Add($"Diện tích ≥ {tieuChiDetail.DienTichMin:N0} m²");
                            }
                            if (!string.IsNullOrEmpty(tieuChiDetail.Huong))
                            {
                                moTaList.Add($"Hướng: {tieuChiDetail.Huong}");
                            }
                            if (!string.IsNullOrEmpty(tieuChiDetail.PhapLy))
                            {
                                moTaList.Add($"Pháp lý: {tieuChiDetail.PhapLy}");
                            }
                            // Đất
                            if (!string.IsNullOrEmpty(tieuChiDetail.LoaiDat))
                            {
                                moTaList.Add($"Loại đất: {tieuChiDetail.LoaiDat}");
                            }
                            if (tieuChiDetail.MatTienMinM.HasValue)
                            {
                                moTaList.Add($"Mặt tiền ≥ {tieuChiDetail.MatTienMinM:N1} m");
                            }
                            if (tieuChiDetail.ChieuSauMinM.HasValue)
                            {
                                moTaList.Add($"Chiều sâu ≥ {tieuChiDetail.ChieuSauMinM:N1} m");
                            }
                            if (!string.IsNullOrEmpty(tieuChiDetail.LoaiDuong))
                            {
                                moTaList.Add($"Loại đường: {tieuChiDetail.LoaiDuong}");
                            }
                            if (tieuChiDetail.DoRongDuongMinM.HasValue)
                            {
                                moTaList.Add($"Đường rộng ≥ {tieuChiDetail.DoRongDuongMinM:N1} m");
                            }
                            if (!string.IsNullOrEmpty(tieuChiDetail.HinhDangDat))
                            {
                                moTaList.Add($"Hình dáng: {tieuChiDetail.HinhDangDat}");
                            }
                            // Nhà
                            if (tieuChiDetail.SoTangMin.HasValue)
                            {
                                moTaList.Add($"Số tầng ≥ {tieuChiDetail.SoTangMin}");
                            }
                            if (tieuChiDetail.SoPhongNgu.HasValue)
                            {
                                moTaList.Add($"Số phòng ngủ ≥ {tieuChiDetail.SoPhongNgu}");
                            }
                            if (tieuChiDetail.SoWcMin.HasValue)
                            {
                                moTaList.Add($"Số WC ≥ {tieuChiDetail.SoWcMin}");
                            }
                            if (tieuChiDetail.NamXayDungMin.HasValue)
                            {
                                moTaList.Add($"Năm XD từ {tieuChiDetail.NamXayDungMin}");
                            }
                            if (!string.IsNullOrEmpty(tieuChiDetail.TinhTrangNha))
                            {
                                moTaList.Add($"Tình trạng: {tieuChiDetail.TinhTrangNha}");
                            }
                            if (!string.IsNullOrEmpty(tieuChiDetail.NoiThat))
                            {
                                moTaList.Add($"Nội thất: {tieuChiDetail.NoiThat}");
                            }
                            if (!string.IsNullOrEmpty(tieuChiDetail.GhiChu))
                            {
                                moTaList.Add($"Ghi chú: {tieuChiDetail.GhiChu}");
                            }
                        }
                    }
                    catch { }
                }

                string loaiGiaoDich = m.LoaiGiaoDich ?? "BAN";
                var danhSachKhop = await TimKiemKetQuaKhopAsync(m.MaQuanHuyen, m.MaPhuongXa, m.MaLoaiBds, loaiGiaoDich, minGia, maxGia, tieuChiDetail);
                var goiY = await _suggestions.FindAsync(m.MaQuanHuyen, m.MaPhuongXa, m.MaLoaiBds,
                    loaiGiaoDich, minGia, maxGia, tieuChiDetail.DienTichMin, m.MaTinh);
                var tinDangTuongDong = await _suggestions.FindListingsAsync(m.MaQuanHuyen, m.MaPhuongXa, m.MaLoaiBds,
                    loaiGiaoDich, minGia, maxGia, tieuChiDetail.DienTichMin, m.MaTinh,
                    danhSachKhop.Where(x => x.NguonTin == "TinDang").Select(x => x.Id).ToArray());
                goiY.AddRange(tinDangTuongDong);

                bool daHetHan = m.NgayHetHan.HasValue && now >= m.NgayHetHan.Value;
                string trangThaiHienTai = m.TrangThai ?? "DANG_THEO_DOI";
                string? thongBaoHoanTien = null;

                // XỬ LÝ CƠ CHẾ HOÀN TIỀN / KHỚP LỆNH KHI QUÁ HẠN 1 THÁNG
                if (trangThaiHienTai == "DANG_THEO_DOI" || trangThaiHienTai == "ACTIVE")
                {
                    if (daHetHan)
                    {
                        if (danhSachKhop.Count == 0)
                        {
                            // Sau 1 tháng mà danh sách tin khớp vẫn trống (0 tin) -> Hoàn lại TienCocDiem vào ViTaiKhoan
                            decimal soDiemHoan = m.TienCocDiem ?? TIEN_COC_BAY_GIA;
                            vi.SoDuDiem = (vi.SoDuDiem ?? 0) + soDiemHoan;
                            vi.NgayCapNhat = now;

                            var gdHoan = new LichSuGiaoDich
                            {
                                MaVi = vi.MaVi,
                                LoaiGiaoDich = "HOAN_COC",
                                SoDiem = soDiemHoan,
                                SoDuSauGiaoDich = vi.SoDuDiem,
                                MoTa = $"Hoàn tiền cọc mục tiêu Bẫy Giá #{m.MaMucTieu} do sau 1 tháng không có tin phù hợp (+{soDiemHoan:N0} đ)",
                                NgayTao = now
                            };
                            _context.LichSuGiaoDiches.Add(gdHoan);

                            m.TrangThai = "DA_HOAN_TIEN";
                            trangThaiHienTai = "DA_HOAN_TIEN";
                            thongBaoHoanTien = $"Đã hoàn {soDiemHoan:N0} đ cọc vào ví vì không tìm thấy tin phù hợp trong 1 tháng.";
                            coThayDoiCsDl = true;
                        }
                        else
                        {
                            // Đã có tin khớp -> Chuyển sang 'DA_KHOP_LENH' và không hoàn tiền cọc
                            m.TrangThai = "DA_KHOP_LENH";
                            trangThaiHienTai = "DA_KHOP_LENH";
                            coThayDoiCsDl = true;
                        }
                    }
                    else if (danhSachKhop.Count > 0)
                    {
                        // Đang trong thời hạn và có tin khớp
                        trangThaiHienTai = "DANG_THEO_DOI";
                    }
                }

                var itemDto = new MucTieuTheoDoiChiTietDto
                {
                    MaMucTieu = m.MaMucTieu,
                    TenQuanHuyen = m.MaQuanHuyenNavigation?.TenQuanHuyen,
                    TenPhuongXa = m.MaPhuongXaNavigation?.TenPhuongXa,
                    TenLoaiBds = m.MaLoaiBdsNavigation?.TenLoaiBds,
                    LoaiGiaoDich = loaiGiaoDich,
                    NganSachMinVnd = minGia,
                    NganSachMaxVnd = maxGia,
                    SoPhongNgu = tieuChiDetail.SoPhongNgu,
                    PhiDaTruDiem = m.TienCocDiem,
                    TrangThai = trangThaiHienTai,
                    NgayTao = m.NgayTao,
                    NgayHetHan = m.NgayHetHan,
                    DaHetHan = daHetHan,
                    ThongBaoHoanTien = thongBaoHoanTien,
                    TieuChiChiTiet = tieuChiDetail,
                    DanhSachMoTaTieuChi = moTaList,
                    DanhSachKhop = danhSachKhop,
                    GoiYTuongDong = goiY.OrderByDescending(x => x.NgayDang).ToList()
                };

                viewModel.DanhSachMucTieu.Add(itemDto);
            }

            if (coThayDoiCsDl)
            {
                await _context.SaveChangesAsync();
                viewModel.SoDuViHienTai = vi.SoDuDiem ?? 0;
            }

            return View(viewModel);
        }

        // POST: /BayGia/TaoMucTieu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoMucTieu([Bind(Prefix = "FormTao")] TaoMucTieuBayGiaViewModel model)
        {
            long userId = GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                var errorList = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => $"{x.Key}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}")
                    .ToList();
                string errorDetail = string.Join("; ", errorList);

                TempData["ErrorMessage"] = $"Vui lòng kiểm tra lại thông tin: {errorDetail}";
                return RedirectToAction("Index");
            }

            if (userId <= 0) return Forbid();
            if (model.NganSachMinVnd < 0 || model.NganSachMinVnd > model.NganSachMaxVnd
                || model.LoaiGiaoDich is not ("BAN" or "CHO_THUE"))
            {
                TempData["ErrorMessage"] = "Khoảng giá hoặc loại giao dịch không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }
            if (!await _context.LoaiBds.AnyAsync(x => x.MaLoaiBds == model.MaLoaiBds && x.TrangThai == true)
                || !await _context.TinhThanhs.AnyAsync(x => x.MaTinh == model.MaTinh && x.TrangThai == true)
                || (model.MaQuanHuyen.HasValue && !await _context.QuanHuyens.AnyAsync(x => x.MaQuanHuyen == model.MaQuanHuyen && x.MaTinh == model.MaTinh && x.TrangThai == true))
                || (model.MaPhuongXa.HasValue && !await _context.PhuongXas.AnyAsync(x => x.MaPhuongXa == model.MaPhuongXa && x.TrangThai == true
                    && x.MaQuanHuyenNavigation != null && x.MaQuanHuyenNavigation.MaTinh == model.MaTinh
                    && (!model.MaQuanHuyen.HasValue || x.MaQuanHuyen == model.MaQuanHuyen))))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn loại BĐS và địa phương hợp lệ, cùng tỉnh/huyện.";
                return RedirectToAction(nameof(Index));
            }
            await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            // 1. Kiểm tra số dư ví tài khoản
            var vi = await LayHoacTaoViAsync(userId);
            if ((vi.SoDuDiem ?? 0) < TIEN_COC_BAY_GIA)
            {
                TempData["ErrorMessage"] = $"Số dư ví của bạn không đủ ({vi.SoDuDiem ?? 0:N0} đ). Tiền cọc kích hoạt bẫy giá là {TIEN_COC_BAY_GIA:N0} đ. Vui lòng nạp thêm tiền vào ví!";
                return RedirectToAction("QuanLyVi", "NguoiDung");
            }

            // 2. Trừ điểm ví tài khoản & ghi lịch sử giao dịch (TRU_DIEM)
            vi.SoDuDiem -= TIEN_COC_BAY_GIA;
            vi.NgayCapNhat = DateTime.Now;

            var lichSuGd = new LichSuGiaoDich
            {
                MaVi = vi.MaVi,
                LoaiGiaoDich = "TRU_DIEM",
                SoDiem = TIEN_COC_BAY_GIA,
                SoDuSauGiaoDich = vi.SoDuDiem,
                MoTa = $"Tạm giữ tiền cọc kích hoạt Bẫy Giá mục tiêu theo dõi thị trường BĐS (-{TIEN_COC_BAY_GIA:N0} đ)",
                NgayTao = DateTime.Now
            };
            _context.LichSuGiaoDiches.Add(lichSuGd);

            // 3. Nếu chưa có MaQuanHuyen nhưng có MaPhuongXa, tự suy luận MaQuanHuyen
            int? maQuanHuyen = model.MaQuanHuyen;
            if (!maQuanHuyen.HasValue && model.MaPhuongXa.HasValue)
            {
                var px = await _context.PhuongXas.FindAsync(model.MaPhuongXa.Value);
                maQuanHuyen = px?.MaQuanHuyen;
            }

            // 4. Lưu mục tiêu vào bảng MucTieuTheoDoiGia
            var ngayTao = DateTime.Now;
            var mucTieu = new MucTieuTheoDoiGia
            {
                MaNguoiDung = userId,
                MaTinh = model.MaTinh,
                MaQuanHuyen = maQuanHuyen,
                MaPhuongXa = model.MaPhuongXa,
                MaLoaiBds = model.MaLoaiBds,
                LoaiGiaoDich = model.LoaiGiaoDich ?? "BAN",
                GiaMongMuonVnd = model.NganSachMaxVnd,
                TienCocDiem = TIEN_COC_BAY_GIA,
                TrangThai = "DANG_THEO_DOI",
                NgayTao = ngayTao,
                NgayHetHan = ngayTao.AddMonths(1) // Theo dõi trong 1 tháng
            };
            _context.MucTieuTheoDoiGias.Add(mucTieu);

            // 5. Gom toàn bộ tiêu chí người dùng nhập thành DTO để serialize vào JSON
            var tieuChiObj = new TieuChiBayGiaDetailDto
            {
                DienTichMin = model.DienTichMin,
                Huong = model.Huong,
                PhapLy = model.PhapLy,
                LoaiDat = model.LoaiDat,
                MatTienMinM = model.MatTienMinM,
                ChieuSauMinM = model.ChieuSauMinM,
                LoaiDuong = model.LoaiDuong,
                DoRongDuongMinM = model.DoRongDuongMinM,
                HinhDangDat = model.HinhDangDat,
                SoTangMin = model.SoTangMin,
                SoPhongNgu = model.SoPhongNgu,
                SoWcMin = model.SoWcMin,
                NamXayDungMin = model.NamXayDungMin,
                TinhTrangNha = model.TinhTrangNha,
                NoiThat = model.NoiThat,
                GhiChu = model.GhiChuTieuChi
            };

            var jsonTieuChi = JsonSerializer.Serialize(tieuChiObj);

            // Lưu đồng bộ chi tiết vào NhuCauTimKiem
            var nhuCau = new NhuCauTimKiem
            {
                MaNguoiDung = userId,
                MaLoaiBds = model.MaLoaiBds,
                MaPhuongXa = model.MaPhuongXa,
                LoaiGiaoDich = model.LoaiGiaoDich ?? "BAN",
                NganSachMinVnd = model.NganSachMinVnd,
                NganSachMaxVnd = model.NganSachMaxVnd,
                DienTichMin = model.DienTichMin,
                MucDich = "Theo dõi bẫy giá tự động",
                TieuChiThem = jsonTieuChi,
                NgayTao = ngayTao
            };
            _context.NhuCauTimKiems.Add(nhuCau);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = $"Kích hoạt Bẫy Giá thành công! Đã tạm giữ cọc {TIEN_COC_BAY_GIA:N0} đ từ ví. Thời hạn theo dõi: 1 tháng.";
            return RedirectToAction("Index", new { targetId = mucTieu.MaMucTieu });
        }

        // POST: /BayGia/HuyMucTieu/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyMucTieu(long id)
        {
            long userId = GetCurrentUserId();
            var target = await _context.MucTieuTheoDoiGias.FirstOrDefaultAsync(m => m.MaMucTieu == id && m.MaNguoiDung == userId);
            if (target != null)
            {
                target.TrangThai = "CLOSED";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã dừng theo dõi mục tiêu bẫy giá này.";
            }

            return RedirectToAction("Index");
        }

        // --- HÀM THUẬT TOÁN TỰ ĐỘNG TÌM KIẾM VÀ GHÉP KẾT QUẢ KHỚP MỤC TIÊU ---
        private async Task<List<KetQuaKhopItemDto>> TimKiemKetQuaKhopAsync(
            int? maQuanHuyen, int? maPhuongXa, int? maLoaiBds, string? loaiGiaoDich,
            decimal? minGia, decimal? maxGia, TieuChiBayGiaDetailDto tieuChi)
        {
            var ketQuaList = new List<KetQuaKhopItemDto>();

            // Tính ngưỡng giá trần: GiaRaoVND <= GiaMongMuonVND * 1.1 (hoặc giá Liên hệ)
            decimal? nguongGiaTran = maxGia.HasValue && maxGia.Value > 0 ? maxGia.Value * 1.1m : null;

            // 1. Tìm trong TinDang trên hệ thống NhaTot
            var tinDangQuery = _context.TinDangs
                .Include(t => t.MaBdsNavigation)
                    .ThenInclude(b => b!.MaLoaiBdsNavigation)
                .Include(t => t.MaBdsNavigation)
                    .ThenInclude(b => b!.MaPhuongXaNavigation)
                .Include(t => t.MaBdsNavigation)
                    .ThenInclude(b => b!.ChiTietNhas)
                .Include(t => t.MaBdsNavigation)
                    .ThenInclude(b => b!.ChiTietDats)
                .Include(t => t.MaNguoiDangNavigation)
                .Where(t => t.TrangThai == "APPROVED");

            // Khớp Loại giao dịch (BAN hoặc CHO_THUE)
            if (!string.IsNullOrEmpty(loaiGiaoDich))
            {
                tinDangQuery = tinDangQuery.Where(t => t.LoaiGiaoDich == loaiGiaoDich);
            }

            // Khớp Loại BĐS
            if (maLoaiBds.HasValue && maLoaiBds > 0)
            {
                tinDangQuery = tinDangQuery.Where(t => t.MaBdsNavigation != null && t.MaBdsNavigation.MaLoaiBds == maLoaiBds);
            }

            // Khớp Khu vực (Quận / Phường)
            if (maPhuongXa.HasValue && maPhuongXa > 0)
            {
                tinDangQuery = tinDangQuery.Where(t => t.MaBdsNavigation != null && t.MaBdsNavigation.MaPhuongXa == maPhuongXa);
            }
            else if (maQuanHuyen.HasValue && maQuanHuyen > 0)
            {
                tinDangQuery = tinDangQuery.Where(t => t.MaBdsNavigation != null && t.MaBdsNavigation.MaPhuongXaNavigation != null && t.MaBdsNavigation.MaPhuongXaNavigation.MaQuanHuyen == maQuanHuyen);
            }

            // Khớp Giá: GiaRaoVND <= GiaMongMuonVND * 1.1 HOẶC NULL/<= 0 (Giá: Liên hệ)
            if (nguongGiaTran.HasValue)
            {
                tinDangQuery = tinDangQuery.Where(t =>
                    !t.GiaRaoVnd.HasValue ||
                    t.GiaRaoVnd.Value <= 0 ||
                    (t.GiaRaoVnd.Value <= nguongGiaTran.Value && (!minGia.HasValue || t.GiaRaoVnd.Value >= minGia.Value)));
            }

            var tinDangs = await tinDangQuery
                .OrderByDescending(t => t.NgayDang)
                .Take(50)
                .ToListAsync();

            foreach (var t in tinDangs)
            {
                var bds = t.MaBdsNavigation;
                var chiTietNha = bds?.ChiTietNhas?.FirstOrDefault();
                var chiTietDat = bds?.ChiTietDats?.FirstOrDefault();

                // Lọc theo Diện tích tối thiểu (nếu có yêu cầu)
                if (tieuChi.DienTichMin.HasValue && tieuChi.DienTichMin.Value > 0)
                {
                    decimal? dienTich = bds?.DienTichDat ?? bds?.DienTichSuDung;
                    if (dienTich.HasValue && dienTich.Value < tieuChi.DienTichMin.Value)
                    {
                        continue;
                    }
                }

                // Lọc theo Hướng (nếu có yêu cầu)
                if (!string.IsNullOrEmpty(tieuChi.Huong) && !string.IsNullOrEmpty(bds?.Huong))
                {
                    if (!bds.Huong.Contains(tieuChi.Huong, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                // Lọc theo Tiêu chí Nhà ở (Số phòng ngủ, số tầng, số WC)
                if (tieuChi.SoPhongNgu.HasValue && tieuChi.SoPhongNgu.Value > 0)
                {
                    if (chiTietNha != null && chiTietNha.SoPhongNgu.HasValue && chiTietNha.SoPhongNgu.Value < tieuChi.SoPhongNgu.Value)
                    {
                        continue;
                    }
                }

                // Lọc theo Tiêu chí Đất (Mặt tiền tối thiểu)
                if (tieuChi.MatTienMinM.HasValue && tieuChi.MatTienMinM.Value > 0)
                {
                    if (chiTietDat != null && chiTietDat.MatTienM.HasValue && chiTietDat.MatTienM.Value < tieuChi.MatTienMinM.Value)
                    {
                        continue;
                    }
                }

                // Xử lý chuỗi giá hiển thị
                string giaHienThi;
                if (!t.GiaRaoVnd.HasValue || t.GiaRaoVnd.Value <= 0)
                {
                    giaHienThi = "Giá: Liên hệ";
                }
                else if (t.GiaRaoVnd.Value >= 1000000000m)
                {
                    giaHienThi = $"{t.GiaRaoVnd.Value / 1000000000m:0.##} tỷ";
                }
                else if (t.GiaRaoVnd.Value >= 1000000m)
                {
                    giaHienThi = $"{t.GiaRaoVnd.Value / 1000000m:0.##} triệu";
                }
                else
                {
                    giaHienThi = $"{t.GiaRaoVnd.Value:N0} đ";
                }

                string? hinhAnh = t.DanhSachAnhDatDai?.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                ketQuaList.Add(new KetQuaKhopItemDto
                {
                    NguonTin = "TinDang",
                    Id = t.MaTin,
                    TieuDe = t.TieuDe ?? "Bất động sản chính chủ",
                    GiaRaoVnd = t.GiaRaoVnd,
                    GiaHienThi = giaHienThi,
                    TenKhuVuc = bds?.MaPhuongXaNavigation?.TenPhuongXa ?? bds?.DiaChiMoTa,
                    TenLoaiBds = bds?.MaLoaiBdsNavigation?.TenLoaiBds,
                    DienTich = bds?.DienTichDat ?? bds?.DienTichSuDung,
                    Huong = bds?.Huong,
                    SoPhongNgu = chiTietNha?.SoPhongNgu,
                    MatTienM = chiTietDat?.MatTienM,
                    HinhAnh = hinhAnh,
                    SoDienThoai = t.MaNguoiDangNavigation?.SoDienThoai ?? "0988888888",
                    TenLienHe = t.MaNguoiDangNavigation?.HoTen ?? "Người bán NhaTot",
                    UrlTinGoc = $"/TinDang/ChiTiet/{t.MaTin}",
                    CoLinkMxh = false,
                    NgayDang = t.NgayDang
                });

                if (ketQuaList.Count >= 25) break;
            }

            // 2. Tìm trong DuLieuThuThap & LeadKhachHangNhuCau (Dữ liệu từ Facebook/Mạng xã hội)
            var leadQuery = _context.LeadKhachHangNhuCaus
                .Include(l => l.MaDuLieuNavigation)
                .Include(l => l.MaPhuongXaNavigation)
                .Where(l => l.MaDuLieuNavigation == null || l.MaDuLieuNavigation.DuLieuGocJson == null)
                .AsQueryable();

            if (!string.IsNullOrEmpty(loaiGiaoDich))
            {
                leadQuery = leadQuery.Where(l => l.LoaiGiaoDich == loaiGiaoDich || l.LoaiGiaoDich == null);
            }

            if (maPhuongXa.HasValue && maPhuongXa > 0)
            {
                leadQuery = leadQuery.Where(l => l.MaPhuongXa == maPhuongXa);
            }
            else if (maQuanHuyen.HasValue && maQuanHuyen > 0)
            {
                leadQuery = leadQuery.Where(l => l.MaPhuongXaNavigation != null && l.MaPhuongXaNavigation.MaQuanHuyen == maQuanHuyen);
            }

            var leads = await leadQuery
                .OrderByDescending(l => l.NgayQuet)
                .Take(25)
                .ToListAsync();

            foreach (var l in leads)
            {
                var tinNguon = l.MaDuLieuNavigation;
                decimal? giaNguon = tinNguon?.GiaRaoVnd
                    ?? (tinNguon?.GiaMinVnd.HasValue == true && tinNguon?.GiaMaxVnd.HasValue == true
                        ? (tinNguon.GiaMinVnd.Value + tinNguon.GiaMaxVnd.Value) / 2m
                        : (tinNguon?.GiaMinVnd ?? tinNguon?.GiaMaxVnd));

                // Lọc theo khoảng giá: GiaRaoVnd <= GiaMongMuonVND * 1.1 HOẶC NULL/<= 0 -> Giá: Liên hệ
                if (nguongGiaTran.HasValue)
                {
                    if (giaNguon.HasValue && giaNguon.Value > 0)
                    {
                        if (giaNguon.Value > nguongGiaTran.Value || (minGia.HasValue && giaNguon.Value < minGia.Value))
                        {
                            continue;
                        }
                    }
                }

                // Lọc theo diện tích nếu nguồn MXH có lưu
                if (tieuChi.DienTichMin.HasValue && tinNguon?.DienTich.HasValue == true)
                {
                    if (tinNguon.DienTich.Value < tieuChi.DienTichMin.Value)
                    {
                        continue;
                    }
                }

                string giaHienThi;
                if (!giaNguon.HasValue || giaNguon.Value <= 0)
                {
                    giaHienThi = "Giá: Liên hệ";
                }
                else if (giaNguon.Value >= 1000000000m)
                {
                    giaHienThi = $"{giaNguon.Value / 1000000000m:0.##} tỷ";
                }
                else if (giaNguon.Value >= 1000000m)
                {
                    giaHienThi = $"{giaNguon.Value / 1000000m:0.##} triệu";
                }
                else
                {
                    giaHienThi = $"{giaNguon.Value:N0} đ";
                }

                // Link bài gốc Facebook hoặc Link Profile Facebook
                string linkMxh = !string.IsNullOrEmpty(tinNguon?.Urltin) 
                    ? tinNguon.Urltin 
                    : (!string.IsNullOrEmpty(l.LinkProfileMxh) ? l.LinkProfileMxh : "https://facebook.com");

                ketQuaList.Add(new KetQuaKhopItemDto
                {
                    NguonTin = "LeadMXH",
                    Id = l.MaLead,
                    TieuDe = tinNguon?.TieuDe ?? l.NoiDungNhuCau ?? "Bất động sản từ Mạng xã hội",
                    GiaRaoVnd = giaNguon,
                    GiaHienThi = giaHienThi,
                    TenKhuVuc = l.MaPhuongXaNavigation?.TenPhuongXa ?? tinNguon?.PhuongXaNguon ?? "Nghệ An",
                    TenLoaiBds = tinNguon?.LoaiBdsnguon ?? "BĐS Mạng xã hội",
                    DienTich = tinNguon?.DienTich,
                    SoPhongNgu = null,
                    HinhAnh = null,
                    SoDienThoai = l.SoDienThoai ?? "0945112233",
                    TenLienHe = l.TenKhachHang ?? "Chính chủ MXH",
                    UrlTinGoc = linkMxh,
                    CoLinkMxh = true,
                    NgayDang = l.NgayQuet ?? tinNguon?.NgayDangNguon
                });
            }

            return ketQuaList;
        }

        private async Task<ViTaiKhoan> LayHoacTaoViAsync(long userId)
        {
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
            return vi;
        }
    }
}
