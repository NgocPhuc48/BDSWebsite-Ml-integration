using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public partial class SocialBotController : Controller
{
    private readonly NhaTotDbContext _context;

    public SocialBotController(NhaTotDbContext context, IConfiguration configuration)
    {
        _context = context;
        ViewBag.ToolCaoDataUrl = configuration["ToolCaoData:BaseUrl"] ?? "http://localhost:5080";
    }

    // GET: Admin/SocialBot (Trang quáº£n lÃ½ danh má»¥c Nguá»“n cÃ o)
    public async Task<IActionResult> Index()
    {
        var nguonList = await _context.NguonDuLieus
            .Include(n => n.DuLieuThuThaps)
            .OrderByDescending(n => n.MaNguon)
            .ToListAsync();

        ViewBag.TongSoNguon = nguonList.Count;
        ViewBag.SoNguonHoatDong = nguonList.Count(n => n.TrangThai == "ACTIVE");
        ViewBag.TongSoBaiCao = await _context.DuLieuThuThaps.CountAsync();

        return View(nguonList);
    }

    // GET: Admin/SocialBot/GetSourceById/5 (Láº¥y dá»¯ liá»‡u nguá»“n cÃ o cho Modal Sá»­a)
    [HttpGet]
    public async Task<IActionResult> GetSourceById(int id)
    {
        var item = await _context.NguonDuLieus.FindAsync(id);
        if (item == null)
            return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y nguá»“n dá»¯ liá»‡u!" });

        return Json(new { success = true, data = item });
    }

    // POST: Admin/SocialBot/ToggleSource/5 (Báº‘t / Táº¯t Bot cÃ o tá»± Ä‘á»™ng)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSource(int id)
    {
        try
        {
            var item = await _context.NguonDuLieus.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y nguá»“n dá»¯ liá»‡u!" });

            item.TrangThai = (item.TrangThai == "ACTIVE") ? "INACTIVE" : "ACTIVE";
            _context.NguonDuLieus.Update(item);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = $"ÄÃ£ {(item.TrangThai == "ACTIVE" ? "Báº¬T" : "Táº®T")} Bot cÃ o nguá»“n: {item.TenNguon}", 
                newStatus = item.TrangThai 
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i há»‡ thá»‘ng: " + ex.Message });
        }
    }

    // POST: Admin/SocialBot/SaveSource (ThÃªm má»›i / Cáº­p nháº­t Nguá»“n cÃ o)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSource([FromBody] NguonDuLieu model)
    {
        if (string.IsNullOrWhiteSpace(model.TenNguon))
            return Json(new { success = false, message = "TÃªn nguá»“n cÃ o khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!" });

        try
        {
            if (model.MaNguon == 0) // ThÃªm má»›i
            {
                model.TrangThai = model.TrangThai ?? "ACTIVE";
                _context.NguonDuLieus.Add(model);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "ThÃªm nguá»“n cÃ o dá»¯ liá»‡u má»›i thÃ nh cÃ´ng!" });
            }
            else // Cáº­p nháº­t
            {
                var existing = await _context.NguonDuLieus.FindAsync(model.MaNguon);
                if (existing == null)
                    return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y nguá»“n dá»¯ liá»‡u!" });

                existing.TenNguon = model.TenNguon.Trim();
                existing.DiaChiWebsite = model.DiaChiWebsite?.Trim();
                existing.PhamViSuDung = model.PhamViSuDung?.Trim();
                existing.GhiChuGiayPhep = model.GhiChuGiayPhep?.Trim();
                existing.TrangThai = model.TrangThai ?? existing.TrangThai;

                _context.NguonDuLieus.Update(existing);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cáº­p nháº­t cáº¥u hÃ¬nh nguá»“n cÃ o thÃ nh cÃ´ng!" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i khi lÆ°u dá»¯ liá»‡u: " + ex.Message });
        }
    }

    // POST: Admin/SocialBot/DeleteSource/5 (XÃ³a nguá»“n cÃ o)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSource(int id)
    {
        try
        {
            var item = await _context.NguonDuLieus.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y nguá»“n dá»¯ liá»‡u cáº§n xÃ³a!" });

            bool hasData = await _context.DuLieuThuThaps.AnyAsync(d => d.MaNguon == id);
            if (hasData)
            {
                return Json(new { success = false, message = "KhÃ´ng thá»ƒ xÃ³a nguá»“n nÃ y vÃ¬ Ä‘Ã£ cÃ³ dá»¯ liá»‡u cÃ o lÆ°u trá»¯ trong há»‡ thá»‘ng!" });
            }

            _context.NguonDuLieus.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "XÃ³a nguá»“n cÃ o thÃ nh cÃ´ng!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i khi xÃ³a: " + ex.Message });
        }
    }

    // GET: Admin/SocialBot/NhatKyXuLy (Xem nháº­t kÃ½ bÃ³c tÃ¡ch dá»¯ liá»‡u ETL / NLP)
    public async Task<IActionResult> NhatKyXuLy(string? ketQua, string? searchKeyword)
    {
        ViewBag.CurrentKetQua = ketQua ?? "ALL";
        ViewBag.SearchKeyword = searchKeyword;

        var query = _context.LichSuXuLyDuLieus
            .Include(l => l.MaDuLieuNavigation)
                .ThenInclude(d => d!.MaNguonNavigation)
            .AsQueryable();

        if (!string.IsNullOrEmpty(ketQua) && ketQua != "ALL")
        {
            query = query.Where(l => l.KetQua == ketQua);
        }

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            string kw = searchKeyword.Trim().ToLower();
            query = query.Where(l => (l.BuocXuLy != null && l.BuocXuLy.ToLower().Contains(kw)) ||
                                     (l.ChiTiet != null && l.ChiTiet.ToLower().Contains(kw)));
        }

        var list = await query.OrderByDescending(l => l.ThoiDiem).Take(250).ToListAsync();
        return View(list);
    }
}
