using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class PhanCongTuVanController : Controller
{
    private readonly NhaTotDbContext _context;

    public PhanCongTuVanController(NhaTotDbContext context)
    {
        _context = context;
    }

    // GET: Admin/PhanCongTuVan
    public async Task<IActionResult> Index(string? trangThai, int? mucDoUuTien)
    {
        ViewBag.CurrentStatus = trangThai ?? "PENDING";
        ViewBag.CurrentPriority = mucDoUuTien;

        var query = _context.YeuCauTuVans
            .Include(y => y.MaNguoiGuiNavigation)
            .Include(y => y.MaNhuCauNavigation)
            .Include(y => y.PhanCongTuVans)
                .ThenInclude(p => p.MaTuVanVienNavigation)
            .AsQueryable();

        // Lá»c theo tráº¡ng thÃ¡i yÃªu cáº§u
        if (!string.IsNullOrEmpty(trangThai) && trangThai != "ALL")
        {
            query = query.Where(y => y.TrangThai == trangThai);
        }

        // Lá»c theo má»©c Ä‘á»™ Æ°u tiÃªn
        if (mucDoUuTien.HasValue && mucDoUuTien > 0)
        {
            query = query.Where(y => y.MucDoUuTien == mucDoUuTien);
        }

        var list = await query.OrderByDescending(y => y.NgayTao).ToListAsync();

        // Thá»‘ng kÃª sá»‘ lÆ°á»£ng theo tráº¡ng thÃ¡i
        ViewBag.CountPending = await _context.YeuCauTuVans.CountAsync(y => y.TrangThai == "PENDING");
        ViewBag.CountInProgress = await _context.YeuCauTuVans.CountAsync(y => y.TrangThai == "IN_PROGRESS");
        ViewBag.CountClosed = await _context.YeuCauTuVans.CountAsync(y => y.TrangThai == "CLOSED");

        // Láº¥y danh sÃ¡ch TÆ° váº¥n viÃªn (VaiTro = 3) hoáº·c Admin (VaiTro = 4)
        ViewBag.DanhSachTuVanVien = await _context.NguoiDungs
            .Where(u => u.VaiTro == 3 || u.VaiTro == 4)
            .Where(u => u.TrangThai == "ACTIVE")
            .OrderBy(u => u.HoTen)
            .ToListAsync();

        return View(list);
    }

    // GET: Admin/PhanCongTuVan/GetDetail/5 (Chi tiáº¿t yÃªu cáº§u & Lá»‹ch sá»­ phÃ¢n cÃ´ng)
    [HttpGet]
    public async Task<IActionResult> GetDetail(long id)
    {
        var item = await _context.YeuCauTuVans
            .Include(y => y.MaNguoiGuiNavigation)
            .Include(y => y.MaNhuCauNavigation)
            .Include(y => y.PhanCongTuVans)
                .ThenInclude(p => p.MaTuVanVienNavigation)
            .Include(y => y.PhanCongTuVans)
                .ThenInclude(p => p.MaNguoiPhanCongNavigation)
            .FirstOrDefaultAsync(y => y.MaYeuCau == id);

        if (item == null)
            return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y yÃªu cáº§u tÆ° váº¥n!" });

        return Json(new { success = true, data = item });
    }

    // POST: Admin/PhanCongTuVan/Assign (PhÃ¢n cÃ´ng YÃªu cáº§u cho TÆ° váº¥n viÃªn)
    [HttpPost]
    public async Task<IActionResult> Assign([FromBody] AssignConsultantModel model)
    {
        if (model.MaYeuCau <= 0 || model.MaTuVanVien <= 0)
        {
            return Json(new { success = false, message = "ThÃ´ng tin phÃ¢n cÃ´ng khÃ´ng há»£p lá»‡!" });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var yeuCau = await _context.YeuCauTuVans.FindAsync(model.MaYeuCau);
            if (yeuCau == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y yÃªu cáº§u tÆ° váº¥n!" });

            var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            long adminId = long.TryParse(adminIdClaim, out var parsedId) ? parsedId : 1;

            // 1. Táº¡o báº£n ghi phÃ¢n cÃ´ng má»›i
            var phanCong = new PhanCongTuVan
            {
                MaYeuCau = model.MaYeuCau,
                MaTuVanVien = model.MaTuVanVien,
                MaNguoiPhanCong = adminId,
                NgayPhanCong = DateTime.Now,
                GhiChu = model.GhiChu?.Trim()
            };

            _context.PhanCongTuVans.Add(phanCong);

            // 2. Chuyá»ƒn tráº¡ng thÃ¡i YÃªu cáº§u sang ÄANG Xá»¬ LÃ (IN_PROGRESS)
            yeuCau.TrangThai = "IN_PROGRESS";
            _context.YeuCauTuVans.Update(yeuCau);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Json(new { success = true, message = "PhÃ¢n cÃ´ng tÆ° váº¥n viÃªn thÃ nh cÃ´ng!" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Json(new { success = false, message = "Lá»—i khi phÃ¢n cÃ´ng: " + ex.Message });
        }
    }
}

public class AssignConsultantModel
{
    public long MaYeuCau { get; set; }
    public long MaTuVanVien { get; set; }
    public string? GhiChu { get; set; }
}
