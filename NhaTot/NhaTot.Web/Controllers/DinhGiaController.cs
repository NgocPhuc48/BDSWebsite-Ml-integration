using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using NhaTot.Web.Models;
using NhaTot.Web.Services;
namespace NhaTot.Web.Controllers;

public class DinhGiaController(NhaTotDbContext db, IAiDinhGiaService valuation, ILogger<DinhGiaController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.DanhSachTinhThanh = await db.TinhThanhs.Where(t => t.TrangThai == true).ToListAsync();
        ViewBag.DanhSachLoaiBds = await db.LoaiBds.Where(l => l.TrangThai == true).ToListAsync();
        return View(new DinhGiaInputModel());
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TinhGia([FromBody] DinhGiaInputModel input)
    {
        if (!ModelState.IsValid) return Json(new { success = false, message = "Vui lòng kiểm tra loại BĐS, phường/xã và diện tích." });
        var result = (KetQuaDinhGiaViewModel)await valuation.DinhGiaBatDongSanAsync(input);
        if (result.Success)
        {
            try
            {
                db.LichSuDinhGias.Add(new LichSuDinhGia {
                    MaNguoiDung = long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null,
                    MaPhienBan = result.MaPhienBan, MaPhuongXa = input.MaPhuongXa, MaLoaiBds = input.MaLoaiBds,
                    DienTich = input.DienTich, GiaDuDoanVnd = result.GiaDuDoanVnd,
                    GiaThapNhatVnd = result.GiaThapNhatVnd, GiaCaoNhatVnd = result.GiaCaoNhatVnd,
                    CanhBao = result.Message.Length > 1000 ? result.Message[..1000] : result.Message,
                    ThoiGianDinhGia = DateTime.Now });
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) { logger.LogWarning(ex, "Không lưu được lịch sử định giá."); }
        }
        return Json(result);
    }
}
