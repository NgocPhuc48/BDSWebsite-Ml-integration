using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using NhaTot.Web.Areas.Admin.Models;

namespace NhaTot.Web.Areas.Admin.Controllers;

public partial class SocialBotController
{
    private async Task ReviewChoices(CrawlReviewModel model, DuLieuThuThap entity)
    {
        model.RawContent = entity.NoiDungGoc;
        model.OriginalUrl = entity.Urltin;
        model.ExtractedLocation = $"{entity.PhuongXaNguon}, {entity.TinhNguon}";
        model.ExtractedType = entity.LoaiBdsnguon;
        model.Status = entity.TrangThaiXuLy;
        model.PropertyTypes = await _context.LoaiBds.Where(x => x.TrangThai == true)
            .OrderBy(x => x.TenLoaiBds).Select(x => new SelectListItem(x.TenLoaiBds, x.MaLoaiBds.ToString())).ToListAsync();
        model.Wards = await _context.PhuongXas.Where(x => x.TrangThai == true)
            .OrderBy(x => x.TenPhuongXa).Select(x => new SelectListItem(
                x.TenPhuongXa + " / " + x.MaQuanHuyenNavigation!.TenQuanHuyen + " / " + x.MaQuanHuyenNavigation.MaTinhNavigation!.TenTinh,
                x.MaPhuongXa.ToString())).ToListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Duyet(long id)
    {
        var item = await _context.DuLieuThuThaps.FindAsync(id);
        if (item == null) return NotFound();
        var model = new CrawlReviewModel {
            Id = id, Title = item.TieuDe ?? "", Price = item.GiaRaoVnd, Area = item.DienTich,
            PropertyTypeId = item.MaLoaiBdsDaDuyet, WardId = item.MaPhuongXaDaDuyet,
            TransactionType = item.LoaiGiaoDichDaDuyet ?? "BAN", ContentType = item.LoaiNoiDungDaDuyet ?? "OFFER"
        };
        await ReviewChoices(model, item);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duyet(CrawlReviewModel model, string operation)
    {
        var item = await _context.DuLieuThuThaps.FindAsync(model.Id);
        if (item == null) return NotFound();
        if (operation is not ("APPROVED" or "REJECTED" or "NEEDS_REVIEW")) return BadRequest();
        if (model.TransactionType is not ("BAN" or "CHO_THUE") || model.ContentType is not ("OFFER" or "DEMAND"))
            ModelState.AddModelError("", "Loại giao dịch hoặc nội dung không hợp lệ.");
        var type = await _context.LoaiBds.FirstOrDefaultAsync(x => x.MaLoaiBds == model.PropertyTypeId && x.TrangThai == true);
        var ward = await _context.PhuongXas.Include(x => x.MaQuanHuyenNavigation)!.ThenInclude(x => x!.MaTinhNavigation)
            .FirstOrDefaultAsync(x => x.MaPhuongXa == model.WardId && x.TrangThai == true);
        if (model.PropertyTypeId.HasValue && type == null) ModelState.AddModelError(nameof(model.PropertyTypeId), "Loại BĐS không hợp lệ.");
        if (model.WardId.HasValue && ward == null) ModelState.AddModelError(nameof(model.WardId), "Phường/xã không hợp lệ.");
        if (operation == "APPROVED")
        {
            if (type == null || ward?.MaQuanHuyenNavigation?.MaTinhNavigation == null || model.Price is null || model.Area is null)
                ModelState.AddModelError("", "Cần đủ loại BĐS, địa phương, giá và diện tích để duyệt.");
            if (!Uri.TryCreate(item.Urltin, UriKind.Absolute, out var url) || url.Scheme != "https"
                || !(url.Host == "facebook.com" || url.Host.EndsWith(".facebook.com", StringComparison.Ordinal)))
                ModelState.AddModelError("", "Bài gốc phải có URL HTTPS Facebook hợp lệ.");
        }
        if (!ModelState.IsValid) { await ReviewChoices(model, item); return View(model); }
        var before = JsonSerializer.Serialize(new { item.TieuDe, item.GiaRaoVnd, item.DienTich, item.MaLoaiBdsDaDuyet,
            item.MaPhuongXaDaDuyet, item.LoaiGiaoDichDaDuyet, item.LoaiNoiDungDaDuyet, item.TrangThaiXuLy });
        item.TieuDe = model.Title.Trim(); item.GiaRaoVnd = model.Price; item.DienTich = model.Area;
        item.MaLoaiBdsDaDuyet = model.PropertyTypeId; item.MaPhuongXaDaDuyet = model.WardId;
        item.LoaiGiaoDichDaDuyet = model.TransactionType; item.LoaiNoiDungDaDuyet = model.ContentType;
        item.LoaiBdsnguon = type?.TenLoaiBds;
        item.PhuongXaNguon = ward?.TenPhuongXa;
        item.TinhNguon = ward?.MaQuanHuyenNavigation?.MaTinhNavigation?.TenTinh;
        item.TrangThaiXuLy = operation;
        // Chia nhật ký để không cắt mất nội dung trong cột ChiTiet(1000).
        var audit = $"Admin {User.FindFirstValue(ClaimTypes.NameIdentifier)}; Before: {before}; After: " +
            JsonSerializer.Serialize(new { model.Title, model.Price, model.Area, model.PropertyTypeId, model.WardId, model.TransactionType, model.ContentType, operation });
        for (var i = 0; i < audit.Length; i += 900)
            item.LichSuXuLyDuLieus.Add(new LichSuXuLyDuLieu { BuocXuLy = "REVIEW_POST", KetQua = operation,
                ThoiDiem = DateTime.UtcNow, ChiTiet = audit.Substring(i, Math.Min(900, audit.Length - i)) });
        await _context.SaveChangesAsync();
        TempData["ExtractionMessage"] = $"Đã cập nhật bài #{item.MaDuLieu}. Chỉ bài chào bán/cho thuê đã duyệt mới xuất hiện trong gợi ý Bẫy giá.";
        return RedirectToAction(nameof(DuLieu));
    }
}
