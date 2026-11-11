using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BaoCaoTinDangController : Controller
{
    private readonly NhaTotDbContext _context;

    public BaoCaoTinDangController(NhaTotDbContext context)
    {
        _context = context;
    }

    // GET: Admin/BaoCaoTinDang?trangThai=PENDING
    public async Task<IActionResult> Index(string trangThai = "PENDING")
    {
        ViewBag.CurrentStatus = trangThai;

        var query = _context.BaoCaoTinDangs
            .Include(b => b.MaTinNavigation)
            .Include(b => b.MaNguoiBaoCaoNavigation)
            .Include(b => b.MaNguoiXuLyNavigation)
            .AsQueryable();

        if (!string.IsNullOrEmpty(trangThai) && trangThai != "ALL")
        {
            query = query.Where(b => b.TrangThai == trangThai);
        }

        var list = await query.OrderByDescending(b => b.NgayBaoCao).ToListAsync();

        ViewBag.CountPending = await _context.BaoCaoTinDangs.CountAsync(b => b.TrangThai == "PENDING");
        ViewBag.CountResolved = await _context.BaoCaoTinDangs.CountAsync(b => b.TrangThai == "RESOLVED");
        ViewBag.CountDismissed = await _context.BaoCaoTinDangs.CountAsync(b => b.TrangThai == "DISMISSED");

        return View(list);
    }

    // POST: Admin/BaoCaoTinDang/Process (Xá»­ lÃ½ bÃ¡o cÃ¡o: KhÃ³a tin vi pháº¡m hoáº·c Bá» qua bÃ¡o cÃ¡o)
    [HttpPost]
    public async Task<IActionResult> Process([FromBody] ProcessReportRequest model)
    {
        try
        {
            var report = await _context.BaoCaoTinDangs.FindAsync(model.MaBaoCao);
            if (report == null)
                return Json(new { success = false, message = "KhÃ´ng tÃ¬m tháº¥y bÃ¡o cÃ¡o!" });

            var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            long adminId = long.TryParse(adminIdClaim, out var parsedId) ? parsedId : 1;

            report.NgayXuLy = DateTime.Now;
            report.MaNguoiXuLy = adminId;

            if (model.HanhDong == "KHOA_TIN")
            {
                report.TrangThai = "RESOLVED";

                // KhÃ³a/Háº¡ tin Ä‘Äƒng bá»‹ bÃ¡o cÃ¡o vi pháº¡m
                var tinDang = await _context.TinDangs.FindAsync(report.MaTin);
                if (tinDang != null)
                {
                    tinDang.TrangThai = "REJECTED";
                    tinDang.LyDoTuChoi = "Bá»‹ khÃ³a do vi pháº¡m bÃ¡o cÃ¡o: " + report.LyDo;
                    _context.TinDangs.Update(tinDang);
                }
            }
            else if (model.HanhDong == "BO_QUA")
            {
                report.TrangThai = "DISMISSED"; // Bá» qua bÃ¡o cÃ¡o rÃ¡c
            }

            _context.BaoCaoTinDangs.Update(report);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xá»­ lÃ½ bÃ¡o cÃ¡o thÃ nh cÃ´ng!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lá»—i khi xá»­ lÃ½: " + ex.Message });
        }
    }
}

public class ProcessReportRequest
{
    public long MaBaoCao { get; set; }
    public string HanhDong { get; set; } = string.Empty; // "KHOA_TIN" hoáº·c "BO_QUA"
}
