using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NhaTot.Web.Areas.Admin.Models;
using NhaTot.Web.Services;

namespace NhaTot.Web.Areas.Admin.Controllers;

public partial class SocialBotController
{
    [HttpGet]
    public async Task<IActionResult> LichCao()
    {
        var schedule = await _context.LichCaoDuLieus.AsNoTracking().SingleAsync(x => x.Id == 1);
        ViewBag.Schedule = schedule;
        return View(new CrawlScheduleModel { Enabled = schedule.Enabled, IntervalMinutes = schedule.IntervalMinutes,
            PostsPerSource = schedule.PostsPerSource, LookbackDays = schedule.LookbackDays });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LichCao(CrawlScheduleModel model)
    {
        var schedule = await _context.LichCaoDuLieus.SingleAsync(x => x.Id == 1);
        if (!CrawlScheduleRules.Valid(model.IntervalMinutes, model.PostsPerSource, model.LookbackDays))
            ModelState.AddModelError("", "Chu kỳ phải từ 15–1440 phút, số bài 1–100, thời gian đăng 1/7/30 ngày.");
        if (model.Enabled && schedule.Status == "START_UNKNOWN" && !model.CheckedApifyConsole)
            ModelState.AddModelError("", "Cần kiểm tra và dừng phiên chưa xác định trên Apify Console trước khi bật lại.");
        if (model.Enabled && schedule.Status == "STARTING")
            ModelState.AddModelError("", "Phiên đang được khởi tạo. Đợi cập nhật trạng thái trước khi thay đổi lịch.");
        if (!ModelState.IsValid) { ViewBag.Schedule = schedule; return View(model); }
        if (model.Enabled && !schedule.Enabled)
        {
            schedule.NextRunUtc = DateTime.UtcNow;
            if (schedule.Status == "START_UNKNOWN") schedule.Status = "IDLE";
        }
        schedule.Enabled = model.Enabled; schedule.IntervalMinutes = model.IntervalMinutes;
        schedule.PostsPerSource = model.PostsPerSource; schedule.LookbackDays = model.LookbackDays;
        await _context.SaveChangesAsync();
        TempData["ScheduleMessage"] = model.Enabled
            ? "Đã bật lịch. Hệ thống kiểm tra mỗi 30 giây khi ứng dụng đang chạy."
            : "Đã tắt lịch tạo phiên mới. Phiên đang chạy vẫn được theo dõi và lưu kết quả.";
        return RedirectToAction(nameof(LichCao));
    }
}
