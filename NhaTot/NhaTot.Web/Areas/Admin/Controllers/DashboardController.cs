using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly NhaTotDbContext _context;

    public DashboardController(NhaTotDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // 1. Thá»‘ng kÃª sá»‘ lÆ°á»£ng tá»•ng quan
        ViewBag.TongNguoiDung = await _context.NguoiDungs.CountAsync();
        ViewBag.TongMoiGioi = await _context.NguoiDungs.CountAsync(u => u.VaiTro == 2);
        
        ViewBag.TongTinDang = await _context.TinDangs.CountAsync();
        ViewBag.TinChoDuyet = await _context.TinDangs.CountAsync(t => t.TrangThai == "PENDING");
        ViewBag.TinDaDuyet = await _context.TinDangs.CountAsync(t => t.TrangThai == "APPROVED");

        ViewBag.TongLoaiBds = await _context.LoaiBds.CountAsync();
        ViewBag.TongQuanHuyen = await _context.QuanHuyens.CountAsync();

        // 2. Thá»‘ng kÃª PhÃ¢n há»‡ VÃ­ & AI Lead
        ViewBag.TongSoDuDiem = await _context.ViTaiKhoans.SumAsync(v => (decimal?)v.SoDuDiem) ?? 0;
        ViewBag.TongLeadsMxH = await _context.LeadKhachHangNhuCaus.CountAsync();
        ViewBag.TongLuotDinhGia = await _context.LichSuDinhGias.CountAsync();

        // 3. Láº¥y 5 Tin Ä‘Äƒng Ä‘ang chá» duyá»‡t má»›i nháº¥t
        var tinChoDuyetList = await _context.TinDangs
            .Where(t => t.TrangThai == "PENDING")
            .OrderByDescending(t => t.NgayDang)
            .Take(5)
            .ToListAsync();

        ViewBag.TinChoDuyetList = tinChoDuyetList;

        // 4. Láº¥y 5 ThÃ nh viÃªn vá»«a má»›i Ä‘Äƒng kÃ½
        var nguoiDungMoiList = await _context.NguoiDungs
            .OrderByDescending(u => u.NgayTao)
            .Take(5)
            .ToListAsync();

        ViewBag.NguoiDungMoiList = nguoiDungMoiList;

        return View();
    }
}
