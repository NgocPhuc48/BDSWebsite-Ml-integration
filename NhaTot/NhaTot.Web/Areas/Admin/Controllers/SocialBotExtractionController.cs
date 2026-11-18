using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Web.Services;

namespace NhaTot.Web.Areas.Admin.Controllers;

public partial class SocialBotController
{
    [HttpGet]
    public async Task<IActionResult> DuLieu(CancellationToken ct)
    {
        var items = await _context.DuLieuThuThaps.AsNoTracking().Include(x => x.MaNguonNavigation)
            .OrderByDescending(x => x.MaDuLieu).Take(100).ToListAsync(ct);
        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ExtractPending([FromServices] CrawlExtractionService extraction, CancellationToken ct)
    {
        try
        {
            var count = await extraction.ProcessPendingAsync(ct);
            TempData["ExtractionMessage"] = $"Đã bóc tách {count} bài NEW. Mỗi lần xử lý tối đa 100 bài; các bài thiếu thông tin cần kiểm tra lại.";
        }
        catch (Microsoft.Data.SqlClient.SqlException)
        {
            TempData["ExtractionMessage"] = "Không xử lý được dữ liệu. Kiểm tra kết nối database rồi thử lại.";
        }
        catch (DbUpdateException) { TempData["ExtractionMessage"] = "Không lưu được kết quả bóc tách. Hãy thử lại."; }
        return RedirectToAction(nameof(DuLieu));
    }
}
