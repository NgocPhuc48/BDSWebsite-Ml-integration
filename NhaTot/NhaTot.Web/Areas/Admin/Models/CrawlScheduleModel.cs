using System.ComponentModel.DataAnnotations;

namespace NhaTot.Web.Areas.Admin.Models;

public sealed class CrawlScheduleModel
{
    public bool Enabled { get; set; }
    [Range(15, 1440)] public int IntervalMinutes { get; set; } = 60;
    [Range(1, 100)] public int PostsPerSource { get; set; } = 10;
    public int LookbackDays { get; set; } = 1;
    public bool CheckedApifyConsole { get; set; }
}
