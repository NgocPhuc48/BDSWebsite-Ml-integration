using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NhaTot.Domain.Entities;

namespace NhaTot.Web.Services;

public sealed class CrawlScheduleWorker(IServiceScopeFactory scopes, ILogger<CrawlScheduleWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        do
        {
            try { await TickAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Lịch cào chưa xử lý được. Kiểm tra kết nối database và cấu hình SocialBot."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NhaTotDbContext>();
        var apify = scope.ServiceProvider.GetRequiredService<ApifyCrawlService>();
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            var schedule = await db.LichCaoDuLieus.SingleOrDefaultAsync(x => x.Id == 1, ct);
            if (schedule == null) return;
            if (!string.IsNullOrEmpty(schedule.ActiveRunId))
            {
                var run = await apify.StatusAsync(schedule.ActiveRunId, ct);
                var status = run.GetProperty("status").GetString() ?? "UNKNOWN";
                schedule.Status = status;
                if (status is "SUCCEEDED" or "ABORTED")
                {
                    var items = await apify.ResultsAsync(run.GetProperty("defaultDatasetId").GetString()!, ct);
                    var sources = JsonSerializer.Deserialize<Dictionary<string, int>>(schedule.SourcesJson ?? "{}")!;
                    var importer = scope.ServiceProvider.GetRequiredService<CrawlImportService>();
                    var count = await importer.ImportAsync(items, sources, schedule.ActiveRunId, ct);
                    schedule.LastMessage = $"Đã lưu {count.Saved} bài, bỏ qua {count.Duplicate} bài trùng, {count.Skipped} bài không hợp lệ.";
                    Finish(schedule);
                }
                else if (status is "FAILED" or "TIMED-OUT")
                {
                    schedule.LastMessage = $"Apify kết thúc với trạng thái {status}.";
                    Finish(schedule);
                }
                else if (DateTime.UtcNow - schedule.LastStartedUtc > TimeSpan.FromMinutes(20))
                {
                    await apify.AbortAsync(schedule.ActiveRunId, ct);
                    schedule.LastMessage = "Phiên quá 20 phút; đã yêu cầu dừng, chờ Apify xác nhận.";
                }
                await db.SaveChangesAsync(ct);
                return;
            }
            if (schedule.Status == "STARTING")
            {
                schedule.Enabled = false; schedule.Status = "START_UNKNOWN";
                schedule.LastMessage = "Phiên khởi tạo bị gián đoạn. Kiểm tra và dừng phiên trên Apify Console trước khi bật lại lịch.";
                await db.SaveChangesAsync(ct); return;
            }
            if (!CrawlScheduleRules.Due(schedule.Enabled, schedule.ActiveRunId, schedule.Status, schedule.NextRunUtc, DateTime.UtcNow)) return;
            if (string.IsNullOrWhiteSpace(await scope.ServiceProvider.GetRequiredService<ApifyTokenStore>().GetAsync(ct)))
            {
                schedule.Enabled = false; schedule.Status = "CONFIG_ERROR"; schedule.LastMessage = "Mở Kết nối Apify trong SocialBot để nhập token.";
                await db.SaveChangesAsync(ct); return;
            }
            var sourcesList = await db.NguonDuLieus.AsNoTracking().Where(x => x.TrangThai == "ACTIVE").ToListAsync(ct);
            var sourceMap = sourcesList.Where(x => !string.IsNullOrEmpty(CrawlImportService.CanonicalUrl(x.DiaChiWebsite ?? "")))
                .GroupBy(x => CrawlImportService.CanonicalUrl(x.DiaChiWebsite!)).ToDictionary(g => g.Key, g => g.First().MaNguon);
            if (sourceMap.Count is < 1 or > 10 || !CrawlScheduleRules.Valid(schedule.IntervalMinutes, schedule.PostsPerSource, schedule.LookbackDays))
            {
                schedule.Enabled = false; schedule.Status = "CONFIG_ERROR";
                schedule.LastMessage = "Cần 1–10 nguồn Facebook đang bật và cấu hình lịch hợp lệ.";
                await db.SaveChangesAsync(ct); return;
            }
            schedule.Status = "STARTING"; schedule.LastStartedUtc = DateTime.UtcNow;
            schedule.SourcesJson = JsonSerializer.Serialize(sourceMap); schedule.LastMessage = "Đang tạo phiên Apify.";
            await db.SaveChangesAsync(ct);
            try
            {
                // Nhận ID kể cả khi host đang dừng để có thể tiếp tục quản lý phiên sau khởi động lại.
                var run = await apify.StartAsync(sourceMap.Keys.ToArray(), schedule.PostsPerSource, schedule.LookbackDays, CancellationToken.None);
                schedule.ActiveRunId = run.GetProperty("id").GetString(); schedule.Status = "RUNNING";
                await db.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                schedule.Enabled = false; schedule.Status = "START_UNKNOWN";
                schedule.LastMessage = "Không xác nhận được việc khởi tạo. Kiểm tra Apify Console trước khi bật lại lịch.";
                await db.SaveChangesAsync(CancellationToken.None);
                logger.LogWarning(ex, "Khởi tạo lịch cào chưa xác nhận được kết quả.");
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static void Finish(LichCaoDuLieu schedule)
    {
        schedule.ActiveRunId = null;
        schedule.NextRunUtc = DateTime.UtcNow.AddMinutes(schedule.IntervalMinutes);
    }
}
