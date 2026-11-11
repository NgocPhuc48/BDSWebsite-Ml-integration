using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class QuanLyViController : Controller
{
    private readonly NhaTotDbContext _context;

    public QuanLyViController(NhaTotDbContext context)
    {
        _context = context;
    }

    // GET: Admin/QuanLyVi
    public async Task<IActionResult> Index(string? loaiGiaoDich, DateTime? tuNgay, DateTime? denNgay, string? searchKeyword)
    {
        ViewBag.LoaiGiaoDich = loaiGiaoDich;
        ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
        ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");
        ViewBag.SearchKeyword = searchKeyword;

        // Thá»‘ng kÃª tá»•ng quan dÃ²ng tiá»n há»‡ thá»‘ng
        ViewBag.TongSoDuHeThong = await _context.ViTaiKhoans.SumAsync(v => (decimal?)v.SoDuDiem) ?? 0;
        ViewBag.TongViHoatDong = await _context.ViTaiKhoans.CountAsync(v => v.TrangThai == "ACTIVE");

        // Tá»•ng Ä‘iá»ƒm náº¡p & trá»« tá»« lá»‹ch sá»­ giao dá»‹ch
        ViewBag.TongDiemNap = await _context.LichSuGiaoDiches
            .Where(g => g.LoaiGiaoDich == "NAP_DIEM")
            .SumAsync(g => (decimal?)g.SoDiem) ?? 0;

        ViewBag.TongDiemHoanCoc = await _context.LichSuGiaoDiches
            .Where(g => g.LoaiGiaoDich == "HOAN_COC")
            .SumAsync(g => (decimal?)g.SoDiem) ?? 0;

        // Truy váº¥n lá»‹ch sá»­ giao dá»‹ch
        var query = _context.LichSuGiaoDiches
            .Include(g => g.MaViNavigation)
                .ThenInclude(v => v!.MaNguoiDungNavigation)
            .AsQueryable();

        if (!string.IsNullOrEmpty(loaiGiaoDich) && loaiGiaoDich != "ALL")
        {
            query = query.Where(g => g.LoaiGiaoDich == loaiGiaoDich);
        }

        if (tuNgay.HasValue)
        {
            query = query.Where(g => g.NgayTao >= tuNgay.Value.Date);
        }

        if (denNgay.HasValue)
        {
            var endOfDay = denNgay.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(g => g.NgayTao <= endOfDay);
        }

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            string kw = searchKeyword.Trim().ToLower();
            query = query.Where(g =>
                (g.MaViNavigation != null && g.MaViNavigation.MaNguoiDungNavigation != null &&
                 (g.MaViNavigation.MaNguoiDungNavigation.HoTen!.ToLower().Contains(kw) ||
                  g.MaViNavigation.MaNguoiDungNavigation.Email!.ToLower().Contains(kw) ||
                  g.MaViNavigation.MaNguoiDungNavigation.SoDienThoai!.Contains(kw))) ||
                (g.MoTa != null && g.MoTa.ToLower().Contains(kw)));
        }

        var list = await query.OrderByDescending(g => g.NgayTao).ToListAsync();

        // Láº¥y danh sÃ¡ch vÃ­ Ä‘á»ƒ dÃ¹ng trong Modal Cá»™ng/Trá»« Ä‘iá»ƒm
        ViewBag.DanhSachVi = await _context.ViTaiKhoans
            .Include(v => v.MaNguoiDungNavigation)
            .Where(v => v.TrangThai == "ACTIVE")
            .OrderBy(v => v.MaNguoiDungNavigation!.HoTen)
            .ToListAsync();

        return View(list);
    }

    // GET: Admin/QuanLyVi/GetDetail/5 (Chi tiáº¿t giao dá»‹ch)
    [HttpGet]
    public async Task<IActionResult> GetDetail(long id)
    {
        var item = await _context.LichSuGiaoDiches
            .Include(g => g.MaViNavigation)
                .ThenInclude(v => v!.MaNguoiDungNavigation)
            .FirstOrDefaultAsync(g => g.MaGiaoDich == id);

        if (item == null)
            return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y giao dá»‹ch!" });

        return Json(new { success = true, data = item });
    }

    // POST: Admin/QuanLyVi/AdjustBalance (Cá»™ng / Trá»« Ä‘iá»ƒm thá»§ cÃ´ng bá»Ÿi Admin)
    [HttpPost]
    public async Task<IActionResult> AdjustBalance([FromBody] AdjustBalanceModel model)
    {
        if (model.MaVi <= 0 || model.SoDiem == 0)
        {
            return Json(new { success = false, message = "ThÃ´ng tin vÃ­ hoáº·c sá»‘ Ä‘iá»ƒm khÃ´ng há»£p lá»‡!" });
        }

        if (string.IsNullOrWhiteSpace(model.MoTa))
        {
            return Json(new { success = false, message = "Vui lÃ²ng nháº­p lÃ½ do / mÃ´ táº£ giao dá»‹ch!" });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var vi = await _context.ViTaiKhoans.FindAsync(model.MaVi);
            if (vi == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y vÃ­ tÃ i khoáº£n!" });

            decimal currentBalance = vi.SoDuDiem ?? 0;

            // Náº¿u lÃ  trá»« Ä‘iá»ƒm, kiá»ƒm tra sá»‘ dÆ° Ä‘á»§ hay khÃ´ng
            if (model.LoaiGiaoDich == "TRU_DIEM" && currentBalance < model.SoDiem)
            {
                return Json(new { success = false, message = $"Sá»‘ dÆ° vÃ­ khÃ´ng Ä‘á»§! (Hiá»‡n cÃ³: {currentBalance:N0} Ä‘iá»ƒm)" });
            }

            decimal newBalance = model.LoaiGiaoDich == "TRU_DIEM"
                ? currentBalance - model.SoDiem
                : currentBalance + model.SoDiem;

            vi.SoDuDiem = newBalance;
            vi.NgayCapNhat = DateTime.Now;

            _context.ViTaiKhoans.Update(vi);

            // Ghi nháº­t kÃ½ giao dá»‹ch
            var history = new LichSuGiaoDich
            {
                MaVi = model.MaVi,
                LoaiGiaoDich = model.LoaiGiaoDich, // "NAP_DIEM" hoáº·c "TRU_DIEM"
                SoDiem = model.SoDiem,
                SoDuSauGiaoDich = newBalance,
                MoTa = $"[Admin Äiá»u chá»‰nh] {model.MoTa.Trim()}",
                NgayTao = DateTime.Now
            };

            _context.LichSuGiaoDiches.Add(history);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Json(new { success = true, message = "Äiá»u chá»‰nh sá»‘ dÆ° vÃ­ thÃ nh cÃ´ng!" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Json(new { success = false, message = "Lá»—i há»‡ thá»‘ng: " + ex.Message });
        }
    }
}

public class AdjustBalanceModel
{
    public long MaVi { get; set; }
    public string LoaiGiaoDich { get; set; } = "NAP_DIEM"; // NAP_DIEM / TRU_DIEM
    public decimal SoDiem { get; set; }
    public string MoTa { get; set; } = string.Empty;
}
