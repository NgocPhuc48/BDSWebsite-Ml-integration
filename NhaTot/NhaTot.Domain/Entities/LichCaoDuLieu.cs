namespace NhaTot.Domain.Entities;

public sealed class LichCaoDuLieu
{
    public int Id { get; set; }
    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; } = 60;
    public int PostsPerSource { get; set; } = 10;
    public int LookbackDays { get; set; } = 1;
    public DateTime? NextRunUtc { get; set; }
    public DateTime? LastStartedUtc { get; set; }
    public string? ActiveRunId { get; set; }
    public string? SourcesJson { get; set; }
    public string Status { get; set; } = "IDLE";
    public string? LastMessage { get; set; }
    public string? EncryptedApifyToken { get; set; }
}
