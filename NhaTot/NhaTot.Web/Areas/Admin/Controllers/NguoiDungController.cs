using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using BCrypt.Net;
namespace NhaTot.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class NguoiDungController : Controller
{
    private readonly NhaTotDbContext _context;

    public NguoiDungController(NhaTotDbContext context)
    {
        _context = context;
    }

    // GET: Admin/NguoiDung
    public async Task<IActionResult> Index(int? vaiTro, string? trangThai)
    {
        ViewBag.SelectedVaiTro = vaiTro;
        ViewBag.SelectedTrangThai = trangThai;

        var query = _context.NguoiDungs.AsQueryable();

        if (vaiTro.HasValue && vaiTro > 0)
        {
            query = query.Where(u => u.VaiTro == vaiTro);
        }

        if (!string.IsNullOrEmpty(trangThai) && trangThai != "ALL")
        {
            query = query.Where(u => u.TrangThai == trangThai);
        }

        var list = await query.OrderByDescending(u => u.NgayTao).ToListAsync();

        // Thá»‘ng kÃª sá»‘ lÆ°á»£ng theo vai trÃ²
        ViewBag.CountTotal = await _context.NguoiDungs.CountAsync();
        ViewBag.CountUser = await _context.NguoiDungs.CountAsync(u => u.VaiTro == 1);
        ViewBag.CountBroker = await _context.NguoiDungs.CountAsync(u => u.VaiTro == 2);
        ViewBag.CountConsultant = await _context.NguoiDungs.CountAsync(u => u.VaiTro == 3);
        ViewBag.CountAdmin = await _context.NguoiDungs.CountAsync(u => u.VaiTro == 4);

        return View(list);
    }

    // GET: Admin/NguoiDung/GetById/5 (Láº¥y thÃ´ng tin tÃ i khoáº£n Ä‘á»• vÃ o Modal)
    [HttpGet]
    public async Task<IActionResult> GetById(long id)
    {
        var item = await _context.NguoiDungs.FindAsync(id);
        if (item == null)
            return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y ngÆ°á»i dÃ¹ng!" });

        return Json(new { success = true, data = item });
    }

    // POST: Admin/NguoiDung/UpdateRole (Thay Ä‘á»•i vai trÃ² / PhÃ¢n quyá»n RBAC)
    [HttpPost]
    public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleModel model)
    {
        if (model.MaNguoiDung <= 0 || model.VaiTro < 1 || model.VaiTro > 4)
        {
            return Json(new { success = false, message = "Dá»¯ liá»‡u vai trÃ² khÃ´ng há»£p lá»‡!" });
        }

        try
        {
            var user = await _context.NguoiDungs.FindAsync(model.MaNguoiDung);
            if (user == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y tÃ i khoáº£n!" });

            user.VaiTro = model.VaiTro;
            user.NgayCapNhat = DateTime.Now;

            _context.NguoiDungs.Update(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cáº­p nháº­t phÃ¢n quyá»n thÃ nh cÃ´ng!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i há»‡ thá»‘ng: " + ex.Message });
        }
    }

    // POST: Admin/NguoiDung/ToggleStatus/5 (KhÃ³a / Má»Ÿ khÃ³a tÃ i khoáº£n)
    [HttpPost]
    public async Task<IActionResult> ToggleStatus(long id)
    {
        try
        {
            var user = await _context.NguoiDungs.FindAsync(id);
            if (user == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y tÃ i khoáº£n!" });

            // Äá»•i tráº¡ng thÃ¡i giá»¯a ACTIVE vÃ  LOCKED
            user.TrangThai = (user.TrangThai == "ACTIVE") ? "LOCKED" : "ACTIVE";
            user.NgayCapNhat = DateTime.Now;

            _context.NguoiDungs.Update(user);
            await _context.SaveChangesAsync();

            string actionName = user.TrangThai == "ACTIVE" ? "Má»Ÿ khÃ³a" : "KhÃ³a";
            return Json(new { success = true, message = $"{actionName} tÃ i khoáº£n thÃ nh cÃ´ng!", newStatus = user.TrangThai });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i khi xá»­ lÃ½: " + ex.Message });
        }
    }

    // POST: Admin/NguoiDung/ResetPassword (Äáº·t láº¡i máº­t kháº©u cho tÃ i khoáº£n)
    [HttpPost]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
    {
        if (model.MaNguoiDung <= 0 || string.IsNullOrWhiteSpace(model.MatKhauMoi))
        {
            return Json(new { success = false, message = "Vui lÃ²ng nháº­p máº­t kháº©u má»›i há»£p lá»‡!" });
        }

        try
        {
            var user = await _context.NguoiDungs.FindAsync(model.MaNguoiDung);
            if (user == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y tÃ i khoáº£n!" });

            // Hash máº­t kháº©u má»›i (sá»­ dá»¥ng BCrypt hoáº·c thuáº­t toÃ¡n mÃ£ hÃ³a máº·c Ä‘á»‹nh cá»§a há»‡ thá»‘ng)
            user.HashMatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhauMoi.Trim());
            user.NgayCapNhat = DateTime.Now;

            _context.NguoiDungs.Update(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cáº¥p láº¡i máº­t kháº©u má»›i thÃ nh cÃ´ng!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i khi Ä‘á»•i máº­t kháº©u: " + ex.Message });
        }
    }
}

public class UpdateRoleModel
{
    public long MaNguoiDung { get; set; }
    public int VaiTro { get; set; }
}

public class ResetPasswordModel
{
    public long MaNguoiDung { get; set; }
    public string MatKhauMoi { get; set; } = string.Empty;
}
