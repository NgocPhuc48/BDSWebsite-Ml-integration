using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using NhaTot.Web.Models;
using NhaTot.Web.Services;

namespace NhaTot.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class KiemDuyetTinController : Controller
{
    private readonly NhaTotDbContext _context;
    private readonly ListingModerationService _moderation;

    public KiemDuyetTinController(NhaTotDbContext context, ListingModerationService moderation)
    {
        _context = context;
        _moderation = moderation;
    }

    // GET: Admin/KiemDuyetTin?trangThai=PENDING
    public async Task<IActionResult> Index(string trangThai = "PENDING", int page = 1)
    {
        ViewBag.CurrentStatus = trangThai;

        var query = _context.TinDangs
            .Include(t => t.MaBdsNavigation)
                .ThenInclude(b => b!.MaLoaiBdsNavigation)
            .Include(t => t.MaBdsNavigation)
                .ThenInclude(b => b!.MaPhuongXaNavigation)
                    .ThenInclude(p => p!.MaQuanHuyenNavigation)
            .Include(t => t.MaNguoiDangNavigation)
            .AsQueryable();

        if (!string.IsNullOrEmpty(trangThai) && trangThai != "ALL")
        {
            query = query.Where(t => t.TrangThai == trangThai);
        }

        const int pageSize = 20;
        var totalItems = await query.CountAsync();
        page = PaginationViewModel.NormalizePage(page, pageSize, totalItems);
        ViewBag.Pagination = new PaginationViewModel(page, pageSize, totalItems);

        var list = await query
            .OrderByDescending(t => t.NgayDang)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var gbtModel = await _moderation.GetOrTrainModelAsync();
        var moderationMap = list.ToDictionary(t => t.MaTin, t => _moderation.AssessListing(t, gbtModel));
        ViewBag.GbtModel = gbtModel;
        ViewBag.ModerationMap = moderationMap;

        // Thá»‘ng kÃª sá»‘ lÆ°á»£ng cho cÃ¡c tháº» Tab
        ViewBag.CountPending = await _context.TinDangs.CountAsync(t => t.TrangThai == "PENDING");
        ViewBag.CountApproved = await _context.TinDangs.CountAsync(t => t.TrangThai == "APPROVED");
        ViewBag.CountRejected = await _context.TinDangs.CountAsync(t => t.TrangThai == "REJECTED");

        return View(list);
    }

    // GET: Admin/KiemDuyetTin/GetDetail/5 (Xem chi tiáº¿t tin Ä‘Äƒng trÃªn Modal)
    [HttpGet]
public async Task<IActionResult> GetDetail(long id)
{
    try
    {
        var item = await _context.TinDangs
            .Include(t => t.MaNguoiDangNavigation)
            .Include(t => t.MaNguoiDuyetNavigation)
            .Include(t => t.MaBdsNavigation)
                .ThenInclude(b => b!.MaLoaiBdsNavigation)
            .Include(t => t.MaBdsNavigation)
                .ThenInclude(b => b!.MaPhuongXaNavigation)
                    .ThenInclude(p => p!.MaQuanHuyenNavigation)
            .Include(t => t.MaBdsNavigation)
                .ThenInclude(b => b!.ChiTietNhas)
            .Include(t => t.MaBdsNavigation)
                .ThenInclude(b => b!.ChiTietDats)
            .FirstOrDefaultAsync(t => t.MaTin == id);

        if (item == null)
            return Json(new { success = false, message = "Không tìm thấy tin đăng trong hệ thống!" });

        var bds = item.MaBdsNavigation;
        var chiTietNha = bds?.ChiTietNhas?.FirstOrDefault();
        var chiTietDat = bds?.ChiTietDats?.FirstOrDefault();
        string tenLoai = bds?.MaLoaiBdsNavigation?.TenLoaiBds ?? "";
        bool isDat = tenLoai.Contains("Đất", StringComparison.OrdinalIgnoreCase) || chiTietDat != null;

        var gbtModel = await _moderation.GetOrTrainModelAsync();
        var moderation = _moderation.AssessListing(item, gbtModel);

        return Json(new
        {
            success = true,
            data = new
            {
                maTin = item.MaTin,
                tieuDe = item.TieuDe ?? "Không có tiêu đề",
                moTa = item.MoTa ?? "Không có mô tả chi tiết.",
                giaRaoVnd = item.GiaRaoVnd,
                loaiGiaoDich = item.LoaiGiaoDich ?? "BAN",
                trangThai = item.TrangThai ?? "PENDING",
                ngayDang = item.NgayDang?.ToString("dd/MM/yyyy HH:mm") ?? "---",
                lyDoTuChoi = item.LyDoTuChoi ?? "",
                danhSachAnhDatDai = item.DanhSachAnhDatDai,
                danhSachAnhSoDoSoHong = item.DanhSachAnhSoDoSoHong,
                moderation = new
                {
                    level = moderation.Level.ToString(),
                    badgeText = moderation.BadgeText,
                    summary = moderation.Summary,
                    predictedPriceVnd = moderation.PredictedPriceVnd,
                    deviationPercent = moderation.DeviationPercent,
                    modelName = moderation.ModelName
                },

                nguoiDang = new
                {
                    hoTen = item.MaNguoiDangNavigation?.HoTen ?? "N/A",
                    soDienThoai = item.MaNguoiDangNavigation?.SoDienThoai ?? "---",
                    email = item.MaNguoiDangNavigation?.Email ?? "---"
                },

                bds = bds == null ? null : new
                {
                    tenLoaiBds = tenLoai,
                    isDat = isDat,
                    tenDuAn = bds.TenDuAn ?? "",
                    diaChiMoTa = bds.DiaChiMoTa ?? "",
                    tenPhuongXa = bds.MaPhuongXaNavigation?.TenPhuongXa ?? "",
                    tenQuanHuyen = bds.MaPhuongXaNavigation?.MaQuanHuyenNavigation?.TenQuanHuyen ?? "",
                    dienTichDat = bds.DienTichDat ?? 0,
                    dienTichSuDung = bds.DienTichSuDung ?? 0,
                    huong = bds.Huong ?? "---",
                    phapLy = bds.PhapLy ?? "---",
                    viDo = bds.ViDo,
                    kinhDo = bds.KinhDo,

                    // Chi tiết Nhà
                    hasNha = chiTietNha != null,
                    soTang = chiTietNha?.SoTang ?? 0,
                    soPhongNgu = chiTietNha?.SoPhongNgu ?? 0,
                    soWc = chiTietNha?.SoWc ?? 0,
                    namXayDung = chiTietNha?.NamXayDung,
                    tinhTrangNha = chiTietNha?.TinhTrangNha ?? "---",
                    noiThat = chiTietNha?.NoiThat ?? "---",

                    // Chi tiết Đất
                    hasDat = chiTietDat != null,
                    loaiDat = chiTietDat?.LoaiDat ?? "---",
                    matTienM = chiTietDat?.MatTienM,
                    chieuSauM = chiTietDat?.ChieuSauM,
                    loaiDuong = chiTietDat?.LoaiDuong ?? "---",
                    doRongDuongM = chiTietDat?.DoRongDuongM,
                    hinhDang = chiTietDat?.HinhDang ?? "---"
                }
            }
        });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = "Lá»—i Server: " + ex.Message });
    }
}

    // POST: Admin/KiemDuyetTin/Approve/5 (Duyá»‡t tin)
    [HttpPost]
    public async Task<IActionResult> Approve(long id)
    {
        try
        {
            var item = await _context.TinDangs.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y tin Ä‘Äƒng!" });

            var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            long adminId = long.TryParse(adminIdClaim, out var parsedId) ? parsedId : 1;

            item.TrangThai = "APPROVED";
            item.NgayDuyet = DateTime.Now;
            item.MaNguoiDuyet = adminId;
            item.LyDoTuChoi = null;

            _context.TinDangs.Update(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "ÄÃ£ duyá»‡t tin Ä‘Äƒng thÃ nh cÃ´ng!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i khi duyá»‡t tin: " + ex.Message });
        }
    }

    // POST: Admin/KiemDuyetTin/Reject (Tá»« chá»‘i tin kÃ¨m lÃ½ do)
    [HttpPost]
    public async Task<IActionResult> Reject([FromBody] RejectRequest model)
    {
        if (model.Id <= 0 || string.IsNullOrWhiteSpace(model.LyDo))
        {
            return Json(new { success = false, message = "Vui lÃ²ng nháº­p Ä‘áº§y Ä‘á»§ lÃ½ do tá»« chá»‘i!" });
        }

        try
        {
            var item = await _context.TinDangs.FindAsync(model.Id);
            if (item == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y tin Ä‘Äƒng!" });

            var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            long adminId = long.TryParse(adminIdClaim, out var parsedId) ? parsedId : 1;

            item.TrangThai = "REJECTED";
            item.NgayDuyet = DateTime.Now;
            item.MaNguoiDuyet = adminId;
            item.LyDoTuChoi = model.LyDo.Trim();

            _context.TinDangs.Update(item);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "ÄÃ£ tá»« chá»‘i tin Ä‘Äƒng!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i há»‡ thá»‘ng: " + ex.Message });
        }
    }

    // POST: Admin/KiemDuyetTin/Delete/5 (Xóa hoàn toàn tin đăng và dữ liệu liên quan)
    [HttpPost]
    public async Task<IActionResult> Delete(long id)
    {
        try
        {
            var item = await _context.TinDangs.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "Không tìm thấy tin đăng cần xóa!" });

            // 1. Dọn dẹp dữ liệu phụ thuộc của TinDang
            var lichSuGia = _context.LichSuGiaTins.Where(x => x.MaTin == id);
            _context.LichSuGiaTins.RemoveRange(lichSuGia);

            var lichHen = _context.LichHens.Where(x => x.MaTin == id);
            _context.LichHens.RemoveRange(lichHen);

            var yeuThich = _context.YeuThiches.Where(x => x.MaTin == id);
            _context.YeuThiches.RemoveRange(yeuThich);

            var baoCao = _context.BaoCaoTinDangs.Where(x => x.MaTin == id);
            _context.BaoCaoTinDangs.RemoveRange(baoCao);

            long? maBds = item.MaBds;

            // 2. Xóa TinDang
            _context.TinDangs.Remove(item);
            await _context.SaveChangesAsync();

            // 3. Nếu BĐS không còn bài đăng nào khác thì xóa BĐS và thông tin nhà/đất
            if (maBds.HasValue)
            {
                bool conTinKhac = await _context.TinDangs.AnyAsync(t => t.MaBds == maBds);
                if (!conTinKhac)
                {
                    var ctNha = _context.ChiTietNhas.Where(c => c.MaBds == maBds);
                    _context.ChiTietNhas.RemoveRange(ctNha);

                    var ctDat = _context.ChiTietDats.Where(c => c.MaBds == maBds);
                    _context.ChiTietDats.RemoveRange(ctDat);

                    var lsDinhGia = _context.LichSuDinhGias.Where(c => c.MaBds == maBds);
                    _context.LichSuDinhGias.RemoveRange(lsDinhGia);

                    var bds = await _context.BatDongSans.FindAsync(maBds.Value);
                    if (bds != null)
                    {
                        _context.BatDongSans.Remove(bds);
                    }
                    await _context.SaveChangesAsync();
                }
            }

            return Json(new { success = true, message = "Đã xóa tin đăng vĩnh viễn!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi khi xóa bài đăng: " + ex.Message });
        }
    }
}

public class RejectRequest
{
    public long Id { get; set; }
    public string LyDo { get; set; } = string.Empty;
}

