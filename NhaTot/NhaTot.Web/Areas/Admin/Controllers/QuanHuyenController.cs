using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class QuanHuyenController : Controller
{
    private readonly NhaTotDbContext _context;

    public QuanHuyenController(NhaTotDbContext context)
    {
        _context = context;
    }

    // GET: Admin/QuanHuyen
    public async Task<IActionResult> Index()
    {
        // Láº¥y danh sÃ¡ch Quáº­n/Huyá»‡n kÃ¨m thÃ´ng tin Tá»‰nh/ThÃ nh
        var list = await _context.QuanHuyens
            .Include(q => q.MaTinhNavigation)
            .ToListAsync();

        // Láº¥y danh sÃ¡ch Tá»‰nh/ThÃ nh Ä‘ang hoáº¡t Ä‘á»™ng Ä‘á»• vÃ o ViewBag cho Dropdown
        ViewBag.TinhThanhs = await _context.TinhThanhs
            .Where(t => t.TrangThai == true)
            .OrderBy(t => t.TenTinh)
            .ToListAsync();

        return View(list);
    }

    // GET: Admin/QuanHuyen/GetById/5 (Láº¥y dá»¯ liá»‡u Ä‘á»• vÃ o Modal Sá»­a)
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.QuanHuyens.FindAsync(id);
        if (item == null)
            return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y dá»¯ liá»‡u!" });

        return Json(new { success = true, data = item });
    }

    // POST: Admin/QuanHuyen/Save (LÆ°u ThÃªm má»›i / Cáº­p nháº­t qua AJAX)
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] QuanHuyen model)
    {
        if (string.IsNullOrWhiteSpace(model.TenQuanHuyen))
        {
            return Json(new { success = false, message = "TÃªn Quáº­n / Huyá»‡n khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!" });
        }

        if (model.MaTinh == null || model.MaTinh <= 0)
        {
            return Json(new { success = false, message = "Vui lÃ²ng chá»n Tá»‰nh / ThÃ nh phá»‘ thuá»™c vá»!" });
        }

        try
        {
            if (model.MaQuanHuyen == 0) // ThÃªm má»›i
            {
                model.TrangThai = model.TrangThai ?? true;
                _context.QuanHuyens.Add(model);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "ThÃªm má»›i Quáº­n / Huyá»‡n thÃ nh cÃ´ng!" });
            }
            else // Cáº­p nháº­t
            {
                var existing = await _context.QuanHuyens.FindAsync(model.MaQuanHuyen);
                if (existing == null)
                    return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y báº£n ghi Ä‘á»ƒ cáº­p nháº­t!" });

                existing.MaTinh = model.MaTinh;
                existing.TenQuanHuyen = model.TenQuanHuyen;
                existing.MaHanhChinh = model.MaHanhChinh;
                existing.TrangThai = model.TrangThai;

                _context.QuanHuyens.Update(existing);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cáº­p nháº­t Quáº­n / Huyá»‡n thÃ nh cÃ´ng!" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i há»‡ thá»‘ng: " + ex.Message });
        }
    }

    // POST: Admin/QuanHuyen/Delete/5 (XÃ³a qua AJAX)
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var item = await _context.QuanHuyens.FindAsync(id);
            if (item == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y dá»¯ liá»‡u cáº§n xÃ³a!" });

            // Kiá»ƒm tra rÃ ng buá»™c dá»¯ liá»‡u: Náº¿u Ä‘ang chá»©a PhÆ°á»ng/XÃ£ con thÃ¬ khÃ´ng cho xÃ³a
            bool hasPhuongXa = await _context.PhuongXas.AnyAsync(p => p.MaQuanHuyen == id);
            if (hasPhuongXa)
            {
                return Json(new { success = false, message = "KhÃ´ng thá»ƒ xÃ³a Quáº­n/Huyá»‡n nÃ y vÃ¬ Ä‘ang chá»©a dá»¯ liá»‡u PhÆ°á»ng/XÃ£!" });
            }

            _context.QuanHuyens.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "XÃ³a Quáº­n / Huyá»‡n thÃ nh cÃ´ng!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i khi xÃ³a: " + ex.Message });
        }
    }
}
