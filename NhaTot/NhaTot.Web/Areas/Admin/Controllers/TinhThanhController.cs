using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class TinhThanhController : Controller
{
    private readonly NhaTotDbContext _context;

    public TinhThanhController(NhaTotDbContext context)
    {
        _context = context;
    }

    // GET: Admin/TinhThanh
    public async Task<IActionResult> Index()
    {
        var list = await _context.TinhThanhs.ToListAsync();
        return View(list);
    }

    // GET: Admin/TinhThanh/GetById/5 (Láº¥y dá»¯ liá»‡u Ä‘á»• vÃ o Popup Sá»­a)
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.TinhThanhs.FindAsync(id);
        if (item == null)
            return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y dá»¯ liá»‡u!" });

        return Json(new { success = true, data = item });
    }

    // POST: Admin/TinhThanh/Save (LÆ°u dá»¯ liá»‡u ThÃªm má»›i / Cáº­p nháº­t qua AJAX)
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] TinhThanh model)
    {
        if (string.IsNullOrWhiteSpace(model.TenTinh))
        {
            return Json(new { success = false, message = "TÃªn tá»‰nh/thÃ nh khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!" });
        }

        try
        {
            if (model.MaTinh == 0) // ThÃªm má»›i
            {
                model.TrangThai = model.TrangThai ?? true;
                _context.TinhThanhs.Add(model);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "ThÃªm má»›i tá»‰nh/thÃ nh thÃ nh cÃ´ng!" });
            }
            else // Cáº­p nháº­t
            {
                var existing = await _context.TinhThanhs.FindAsync(model.MaTinh);
                if (existing == null)
                    return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y báº£n ghi Ä‘á»ƒ cáº­p nháº­t!" });

                existing.TenTinh = model.TenTinh;
                existing.MaHanhChinh = model.MaHanhChinh;
                existing.TrangThai = model.TrangThai;

                _context.TinhThanhs.Update(existing);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cáº­p nháº­t tá»‰nh/thÃ nh thÃ nh cÃ´ng!" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i há»‡ thá»‘ng: " + ex.Message });
        }
    }

    // POST: Admin/TinhThanh/Delete/5 (XÃ³a qua AJAX)
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var item = await _context.TinhThanhs.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y tá»‰nh/thÃ nh cáº§n xÃ³a!" });

            // Kiá»ƒm tra rÃ ng buá»™c dá»¯ liá»‡u: Náº¿u cÃ³ Quáº­n/Huyá»‡n thuá»™c Tá»‰nh nÃ y thÃ¬ khÃ´ng cho xÃ³a
            bool hasQuanHuyen = await _context.QuanHuyens.AnyAsync(q => q.MaTinh == id);
            if (hasQuanHuyen)
            {
                return Json(new { success = false, message = "KhÃ´ng thá»ƒ xÃ³a tá»‰nh/thÃ nh nÃ y vÃ¬ Ä‘ang chá»©a dá»¯ liá»‡u Quáº­n/Huyá»‡n!" });
            }

            _context.TinhThanhs.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "XÃ³a tá»‰nh/thÃ nh thÃ nh cÃ´ng!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i khi xÃ³a: " + ex.Message });
        }
    }
}
