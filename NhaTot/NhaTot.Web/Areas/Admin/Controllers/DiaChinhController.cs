using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DiaChinhController : Controller
{
    private readonly NhaTotDbContext _context;

    public DiaChinhController(NhaTotDbContext context)
    {
        _context = context;
    }

    // GET: Admin/DiaChinh
    public async Task<IActionResult> Index()
    {
        var quanHuyens = await _context.QuanHuyens
            .Include(q => q.PhuongXas)
            .OrderBy(q => q.TenQuanHuyen)
            .ToListAsync();

        ViewBag.PhuongXas = await _context.PhuongXas
            .Include(p => p.MaQuanHuyenNavigation)
            .OrderBy(p => p.TenPhuongXa)
            .ToListAsync();

        return View(quanHuyens);
    }

    #region -- QUáº¬N / HUYá»†N --

    // POST: Admin/DiaChinh/CreateQuanHuyen
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuanHuyen(QuanHuyen model)
    {
        if (string.IsNullOrWhiteSpace(model.TenQuanHuyen))
        {
            TempData["ErrorMessage"] = "TÃªn Quáº­n/Huyá»‡n khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng!";
            return RedirectToAction(nameof(Index));
        }

        model.MaTinh = 1; // Máº·c Ä‘á»‹nh Tá»‰nh Nghá»‡ An (ID = 1)
        model.TrangThai = true;
        _context.QuanHuyens.Add(model);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"ThÃªm Quáº­n/Huyá»‡n '{model.TenQuanHuyen}' thÃ nh cÃ´ng!";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/DiaChinh/EditQuanHuyen
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditQuanHuyen(QuanHuyen model)
    {
        var item = await _context.QuanHuyens.FindAsync(model.MaQuanHuyen);
        if (item == null)
        {
            TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y dá»¯ liá»‡u Quáº­n/Huyá»‡n!";
            return RedirectToAction(nameof(Index));
        }

        item.TenQuanHuyen = model.TenQuanHuyen;
        item.MaHanhChinh = model.MaHanhChinh;
        item.TrangThai = model.TrangThai;

        _context.Update(item);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Cáº­p nháº­t Quáº­n/Huyá»‡n '{item.TenQuanHuyen}' thÃ nh cÃ´ng!";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/DiaChinh/DeleteQuanHuyen (Popup xÃ³a trÃªn trang Index)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuanHuyen(int maQuanHuyen)
    {
        var item = await _context.QuanHuyens
            .Include(q => q.PhuongXas)
            .FirstOrDefaultAsync(q => q.MaQuanHuyen == maQuanHuyen);

        if (item == null)
        {
            TempData["ErrorMessage"] = "Quáº­n/Huyá»‡n khÃ´ng tá»“n táº¡i!";
            return RedirectToAction(nameof(Index));
        }

        // Kiá»ƒm tra xem cÃ³ PhÆ°á»ng/XÃ£ phá»¥ thuá»™c khÃ´ng
        if (item.PhuongXas != null && item.PhuongXas.Any())
        {
            TempData["ErrorMessage"] = $"KhÃ´ng thá»ƒ xÃ³a '{item.TenQuanHuyen}' vÃ¬ váº«n cÃ²n {item.PhuongXas.Count} PhÆ°á»ng/XÃ£ trá»±c thuá»™c!";
            return RedirectToAction(nameof(Index));
        }

        _context.QuanHuyens.Remove(item);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"ÄÃ£ xÃ³a Quáº­n/Huyá»‡n '{item.TenQuanHuyen}' thÃ nh cÃ´ng!";
        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region -- PHÆ¯á»œNG / XÃƒ --

    // POST: Admin/DiaChinh/CreatePhuongXa
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePhuongXa(PhuongXa model)
    {
        if (string.IsNullOrWhiteSpace(model.TenPhuongXa) || model.MaQuanHuyen <= 0)
        {
            TempData["ErrorMessage"] = "Vui lÃ²ng Ä‘iá»n Ä‘á»§ TÃªn PhÆ°á»ng/XÃ£ vÃ  chá»n Quáº­n/Huyá»‡n trá»±c thuá»™c!";
            return RedirectToAction(nameof(Index));
        }

        model.TrangThai = true;
        _context.PhuongXas.Add(model);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"ThÃªm PhÆ°á»ng/XÃ£ '{model.TenPhuongXa}' thÃ nh cÃ´ng!";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/DiaChinh/EditPhuongXa
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPhuongXa(PhuongXa model)
    {
        var item = await _context.PhuongXas.FindAsync(model.MaPhuongXa);
        if (item == null)
        {
            TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y PhÆ°á»ng/XÃ£!";
            return RedirectToAction(nameof(Index));
        }

        item.TenPhuongXa = model.TenPhuongXa;
        item.MaQuanHuyen = model.MaQuanHuyen;
        item.MaHanhChinh = model.MaHanhChinh;
        item.TrangThai = model.TrangThai;

        _context.Update(item);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Cáº­p nháº­t PhÆ°á»ng/XÃ£ '{item.TenPhuongXa}' thÃ nh cÃ´ng!";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/DiaChinh/DeletePhuongXa (Popup xÃ³a trÃªn trang Index)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhuongXa(int maPhuongXa)
    {
        var item = await _context.PhuongXas.FindAsync(maPhuongXa);
        if (item == null)
        {
            TempData["ErrorMessage"] = "PhÆ°á»ng/XÃ£ khÃ´ng tá»“n táº¡i!";
            return RedirectToAction(nameof(Index));
        }

        // Kiá»ƒm tra Báº¥t Ä‘á»™ng sáº£n liÃªn káº¿t
        bool hasBds = await _context.BatDongSans.AnyAsync(b => b.MaPhuongXa == maPhuongXa);
        if (hasBds)
        {
            TempData["ErrorMessage"] = $"KhÃ´ng thá»ƒ xÃ³a '{item.TenPhuongXa}' vÃ¬ Ä‘Ã£ cÃ³ Báº¥t Ä‘á»™ng sáº£n thuá»™c khu vá»±c nÃ y!";
            return RedirectToAction(nameof(Index));
        }

        _context.PhuongXas.Remove(item);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"ÄÃ£ xÃ³a PhÆ°á»ng/XÃ£ '{item.TenPhuongXa}' thÃ nh cÃ´ng!";
        return RedirectToAction(nameof(Index));
    }

    #endregion
}
