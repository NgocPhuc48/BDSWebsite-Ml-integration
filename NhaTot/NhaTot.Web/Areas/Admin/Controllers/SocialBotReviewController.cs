using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;
using NhaTot.Web.Areas.Admin.Models;
using NhaTot.Web.Services;

namespace NhaTot.Web.Areas.Admin.Controllers;

public partial class SocialBotController
{
    private static string? NormalizeProvinceChoice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim();
        if (v.Contains("Nghệ An", StringComparison.OrdinalIgnoreCase) || v.Contains("Nghe An", StringComparison.OrdinalIgnoreCase))
            return "Nghệ An";
        if (v.Contains("Hà Tĩnh", StringComparison.OrdinalIgnoreCase) || v.Contains("Ha Tinh", StringComparison.OrdinalIgnoreCase))
            return "Hà Tĩnh";
        return null;
    }

    private async Task ReviewChoices(CrawlReviewModel model, DuLieuThuThap entity)
    {
        model.RawContent = RealEstateTextExtractor.CleanIcons(entity.NoiDungGoc);
        model.OriginalUrl = entity.Urltin;
        model.ExtractedLocation = string.Join(", ", new[] { entity.PhuongXaNguon, entity.TinhNguon }.Where(s => !string.IsNullOrWhiteSpace(s)));
        model.ExtractedType = entity.LoaiBdsnguon;
        model.Status = entity.TrangThaiXuLy;
        var types = await _context.LoaiBds.Where(x => x.TrangThai == true).ToListAsync();
        model.PropertyTypes = types
            .OrderBy(x => (x.TenLoaiBds ?? "").StartsWith("Cho thuê", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ThenBy(x => x.TenLoaiBds)
            .Select(x => new SelectListItem(x.TenLoaiBds, x.MaLoaiBds.ToString()))
            .ToList();
    }

    [HttpGet]
    public async Task<IActionResult> Duyet(long id)
    {
        var item = await _context.DuLieuThuThaps.FindAsync(id);
        if (item == null) return NotFound();
        var detectedProvince = NormalizeProvinceChoice(item.TinhNguon) ?? NormalizeProvinceChoice(item.NoiDungGoc);
        int? propertyTypeId = item.MaLoaiBdsDaDuyet;
        if (!propertyTypeId.HasValue && !string.IsNullOrWhiteSpace(item.LoaiBdsnguon))
        {
            var mappedName = item.LoaiBdsnguon == "Đất" ? "Đất nền" : item.LoaiBdsnguon;
            propertyTypeId = await _context.LoaiBds
                .Where(x => x.TrangThai == true && x.TenLoaiBds == mappedName)
                .Select(x => (int?)x.MaLoaiBds)
                .FirstOrDefaultAsync();
        }
        var cleanTitle = RealEstateTextExtractor.CleanIcons(item.TieuDe);
        var model = new CrawlReviewModel
        {
            Id = id,
            Title = string.IsNullOrWhiteSpace(cleanTitle) ? (item.TieuDe ?? "") : cleanTitle,
            ExactPrice = item.GiaRaoVnd,
            PriceMin = item.GiaRaoVnd.HasValue ? null : item.GiaMinVnd,
            PriceMax = item.GiaRaoVnd.HasValue ? null : item.GiaMaxVnd,
            Area = item.DienTich,
            PropertyTypeId = propertyTypeId,
            Province = detectedProvince,
            WardId = item.MaPhuongXaDaDuyet,
            TransactionType = item.LoaiGiaoDichDaDuyet ?? "BAN",
            ContentType = item.LoaiNoiDungDaDuyet ?? "OFFER"
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
        var normalizedProvince = NormalizeProvinceChoice(model.Province);

        decimal? finalExact = null;
        decimal? finalMin = null;
        decimal? finalMax = null;

        if (model.ExactPrice.HasValue)
        {
            finalExact = model.ExactPrice.Value;
        }
        else if (model.PriceMin.HasValue || model.PriceMax.HasValue)
        {
            if (!model.PriceMin.HasValue || !model.PriceMax.HasValue)
                ModelState.AddModelError("", "Nếu nhập Khoảng giá thì cần nhập đủ cả GiaMinVND và GiaMaxVND.");
            else if (model.PriceMin.Value > model.PriceMax.Value)
                ModelState.AddModelError("", "Giá tối thiểu (GiaMinVND) không được lớn hơn Giá tối đa (GiaMaxVND).");
            else if (model.PriceMin.Value == model.PriceMax.Value)
                finalExact = model.PriceMin.Value;
            else
            {
                finalMin = model.PriceMin.Value;
                finalMax = model.PriceMax.Value;
            }
        }

        if (model.PropertyTypeId.HasValue && type == null)
            ModelState.AddModelError(nameof(model.PropertyTypeId), "Loại BĐS không hợp lệ.");
        if (!string.IsNullOrWhiteSpace(model.Province) && normalizedProvince == null)
            ModelState.AddModelError(nameof(model.Province), "Địa phương chỉ hỗ trợ Nghệ An hoặc Hà Tĩnh.");

        if (operation == "APPROVED")
        {
            var hasAnyPrice = finalExact.HasValue || (finalMin.HasValue && finalMax.HasValue);
            if (type == null || normalizedProvince == null || !hasAnyPrice || model.Area is null)
                ModelState.AddModelError("", "Cần đủ loại BĐS, địa phương, Giá (hoặc Khoảng giá) và diện tích để duyệt.");
            if (!Uri.TryCreate(item.Urltin, UriKind.Absolute, out var url) || (url.Scheme != "https" && url.Scheme != "http")
                || !(url.Host == "facebook.com" || url.Host.EndsWith(".facebook.com", StringComparison.OrdinalIgnoreCase)
                     || url.Host == "fb.com" || url.Host.EndsWith(".fb.com", StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError("", "Bài gốc phải có URL Facebook hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            await ReviewChoices(model, item);
            return View(model);
        }

        int? resolvedWardId = item.MaPhuongXaDaDuyet;
        if (normalizedProvince != null && !string.IsNullOrWhiteSpace(item.PhuongXaNguon))
        {
            var matchedWard = await _context.PhuongXas
                .Where(x => x.TrangThai == true
                    && x.TenPhuongXa == item.PhuongXaNguon
                    && x.MaQuanHuyenNavigation != null
                    && x.MaQuanHuyenNavigation.MaTinhNavigation != null
                    && x.MaQuanHuyenNavigation.MaTinhNavigation.TenTinh!.Contains(normalizedProvince))
                .Select(x => (int?)x.MaPhuongXa)
                .FirstOrDefaultAsync();
            if (matchedWard.HasValue) resolvedWardId = matchedWard;
        }

        var before = JsonSerializer.Serialize(new
        {
            item.TieuDe, item.GiaRaoVnd, item.GiaMinVnd, item.GiaMaxVnd, item.DienTich, item.MaLoaiBdsDaDuyet,
            item.MaPhuongXaDaDuyet, item.TinhNguon, item.LoaiGiaoDichDaDuyet, item.LoaiNoiDungDaDuyet, item.TrangThaiXuLy
        });

        item.TieuDe = model.Title.Trim();
        item.GiaRaoVnd = finalExact;
        item.GiaMinVnd = finalMin;
        item.GiaMaxVnd = finalMax;
        item.DienTich = model.Area;
        item.MaLoaiBdsDaDuyet = model.PropertyTypeId;
        item.MaPhuongXaDaDuyet = resolvedWardId;
        item.LoaiGiaoDichDaDuyet = model.TransactionType;
        item.LoaiNoiDungDaDuyet = model.ContentType;
        item.LoaiBdsnguon = type?.TenLoaiBds;
        item.TinhNguon = normalizedProvince;
        item.TrangThaiXuLy = operation;

        var audit = $"Admin {User.FindFirstValue(ClaimTypes.NameIdentifier)}; Before: {before}; After: " +
            JsonSerializer.Serialize(new { model.Title, ExactPrice = finalExact, PriceMin = finalMin, PriceMax = finalMax, model.Area, model.PropertyTypeId, Province = normalizedProvince, model.TransactionType, model.ContentType, operation });
        for (var i = 0; i < audit.Length; i += 900)
        {
            item.LichSuXuLyDuLieus.Add(new LichSuXuLyDuLieu
            {
                BuocXuLy = "REVIEW_POST",
                KetQua = operation,
                ThoiDiem = DateTime.UtcNow,
                ChiTiet = audit.Substring(i, Math.Min(900, audit.Length - i))
            });
        }

        await _context.SaveChangesAsync();
        TempData["ExtractionMessage"] = $"Đã cập nhật bài #{item.MaDuLieu}. Chỉ bài chào bán/cho thuê đã duyệt mới xuất hiện trong gợi ý Bẫy giá.";
        return RedirectToAction(nameof(DuLieu));
    }
}
