using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class LoaiBdsController : Controller
{
    private readonly NhaTotDbContext _context;

    public LoaiBdsController(NhaTotDbContext context)
    {
        _context = context;
    }

    // GET: Admin/LoaiBds
    public async Task<IActionResult> Index()
    {
        var list = await _context.LoaiBds.ToListAsync();
        return View(list);
    }

    // GET: Admin/LoaiBds/GetById/5 (Láº¥y dá»¯ liá»‡u Ä‘á»• vÃ o Modal Sá»­a)
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.LoaiBds.FindAsync(id);
        if (item == null)
            return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y Loáº¡i BÄS!" });

        return Json(new { success = true, data = item });
    }

    // POST: Admin/LoaiBds/Save (ThÃªm má»›i / Cáº­p nháº­t qua AJAX)
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] LoaiBd model)
    {
        if (string.IsNullOrWhiteSpace(model.TenLoaiBds))
        {
            return Json(new { success = false, message = "TÃªn Loáº¡i BÄS khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!" });
        }

        try
        {
            if (model.MaLoaiBds == 0) // ThÃªm má»›i
            {
                model.TrangThai = model.TrangThai ?? true;
                _context.LoaiBds.Add(model);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "ThÃªm má»›i Loáº¡i BÄS thÃ nh cÃ´ng!" });
            }
            else // Cáº­p nháº­t
            {
                var existing = await _context.LoaiBds.FindAsync(model.MaLoaiBds);
                if (existing == null)
                    return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y báº£n ghi Ä‘á»ƒ cáº­p nháº­t!" });

                existing.TenLoaiBds = model.TenLoaiBds;
                existing.MoTa = model.MoTa;
                existing.TrangThai = model.TrangThai;

                _context.LoaiBds.Update(existing);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cáº­p nháº­t Loáº¡i BÄS thÃ nh cÃ´ng!" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i há»‡ thá»‘ng: " + ex.Message });
        }
    }

    // POST: Admin/LoaiBds/Delete/5 (XÃ³a qua AJAX)
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var item = await _context.LoaiBds.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y Loáº¡i BÄS cáº§n xÃ³a!" });

            // Kiá»ƒm tra rÃ ng buá»™c: Náº¿u Ä‘Ã£ cÃ³ BÄS thuá»™c Loáº¡i nÃ y thÃ¬ khÃ´ng cho xÃ³a
            bool hasBds = await _context.BatDongSans.AnyAsync(b => b.MaLoaiBds == id);
            if (hasBds)
            {
                return Json(new { success = false, message = "KhÃ´ng thá»ƒ xÃ³a vÃ¬ Ä‘ang cÃ³ Báº¥t Ä‘á»™ng sáº£n thuá»™c Loáº¡i nÃ y!" });
            }

            _context.LoaiBds.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "XÃ³a Loáº¡i BÄS thÃ nh cÃ´ng!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i khi xÃ³a: " + ex.Message });
        }
    }
}
