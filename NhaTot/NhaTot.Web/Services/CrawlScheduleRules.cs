namespace NhaTot.Web.Services;

public static class CrawlScheduleRules
{
    public static bool Valid(int interval, int limit, int days) =>
        interval is >= 15 and <= 1440 && limit is >= 1 and <= 100 && days is 1 or 7 or 30;
    public static bool Due(bool enabled, string? runId, string status, DateTime? next, DateTime now) =>
        enabled && string.IsNullOrEmpty(runId) && status != "STARTING" && (!next.HasValue || next <= now);
}
