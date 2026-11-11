using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class PhuongXaController : Controller
{
    private readonly NhaTotDbContext _context;

    public PhuongXaController(NhaTotDbContext context)
    {
        _context = context;
    }

    // GET: Admin/PhuongXa
    public async Task<IActionResult> Index(int? maQuanHuyen)
    {
        var query = _context.PhuongXas
            .Include(p => p.MaQuanHuyenNavigation)
            .ThenInclude(q => q!.MaTinhNavigation)
            .AsQueryable();

        // Lá»c danh sÃ¡ch theo Quáº­n/Huyá»‡n náº¿u ngÆ°á»i dÃ¹ng chá»n bá»™ lá»c
        if (maQuanHuyen.HasValue && maQuanHuyen > 0)
        {
            query = query.Where(p => p.MaQuanHuyen == maQuanHuyen);
        }

        var list = await query.ToListAsync();

        // Láº¥y danh sÃ¡ch Quáº­n/Huyá»‡n cho Dropdown lá»c vÃ  Modal ThÃªm/Sá»­a
        ViewBag.QuanHuyens = await _context.QuanHuyens
            .Include(q => q.MaTinhNavigation)
            .Where(q => q.TrangThai == true)
            .OrderBy(q => q.TenQuanHuyen)
            .ToListAsync();

        ViewBag.SelectedQuanHuyen = maQuanHuyen;

        return View(list);
    }

    // GET: Admin/PhuongXa/GetById/5 (Láº¥y dá»¯ liá»‡u Ä‘á»• vÃ o Modal Sá»­a)
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.PhuongXas.FindAsync(id);
        if (item == null)
            return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y dá»¯ liá»‡u PhÆ°á»ng / XÃ£!" });

        return Json(new { success = true, data = item });
    }

    // POST: Admin/PhuongXa/Save (LÆ°u ThÃªm má»›i / Cáº­p nháº­t qua AJAX)
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] PhuongXa model)
    {
        if (string.IsNullOrWhiteSpace(model.TenPhuongXa))
        {
            return Json(new { success = false, message = "TÃªn PhÆ°á»ng / XÃ£ khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!" });
        }

        if (model.MaQuanHuyen == null || model.MaQuanHuyen <= 0)
        {
            return Json(new { success = false, message = "Vui lÃ²ng chá»n Quáº­n / Huyá»‡n thuá»™c vá»!" });
        }

        try
        {
            if (model.MaPhuongXa == 0) // ThÃªm má»›i
            {
                model.TrangThai = model.TrangThai ?? true;
                _context.PhuongXas.Add(model);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "ThÃªm má»›i PhÆ°á»ng / XÃ£ thÃ nh cÃ´ng!" });
            }
            else // Cáº­p nháº­t
            {
                var existing = await _context.PhuongXas.FindAsync(model.MaPhuongXa);
                if (existing == null)
                    return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y báº£n ghi Ä‘á»ƒ cáº­p nháº­t!" });

                existing.MaQuanHuyen = model.MaQuanHuyen;
                existing.TenPhuongXa = model.TenPhuongXa;
                existing.MaHanhChinh = model.MaHanhChinh;
                existing.TrangThai = model.TrangThai;

                _context.PhuongXas.Update(existing);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cáº­p nháº­t PhÆ°á»ng / XÃ£ thÃ nh cÃ´ng!" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i há»‡ thá»‘ng: " + ex.Message });
        }
    }

    // POST: Admin/PhuongXa/Delete/5 (XÃ³a qua AJAX)
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var item = await _context.PhuongXas.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y dá»¯ liá»‡u cáº§n xÃ³a!" });

            // Kiá»ƒm tra rÃ ng buá»™c dá»¯ liá»‡u: Náº¿u cÃ³ Báº¥t Ä‘á»™ng sáº£n thuá»™c PhÆ°á»ng/XÃ£ nÃ y thÃ¬ khÃ´ng cho xÃ³a
            bool hasBds = await _context.BatDongSans.AnyAsync(b => b.MaPhuongXa == id);
            if (hasBds)
            {
                return Json(new { success = false, message = "KhÃ´ng thá»ƒ xÃ³a PhÆ°á»ng / XÃ£ nÃ y vÃ¬ Ä‘ang chá»©a dá»¯ liá»‡u Báº¥t Ä‘á»™ng sáº£n!" });
            }

            _context.PhuongXas.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "XÃ³a PhÆ°á»ng / XÃ£ thÃ nh cÃ´ng!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i khi xÃ³a: " + ex.Message });
        }
    }
}
