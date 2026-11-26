namespace ToolCaoData.Models;

public class PostRecord
{
    public string PostId { get; set; } = "";
    public string ActionId { get; set; } = "";
    public string Text { get; set; } = "";
    public string PostUrl { get; set; } = "#";
    public string CreateTime { get; set; } = "Không có";
    public string Author { get; set; } = "Tác giả không rõ";
    public string AuthorUsername { get; set; } = "";
    public string AuthorUserId { get; set; } = "";
    public string AuthorProfileUrl { get; set; } = "#";
    public string AuthorAvatar { get; set; } = "";
    public long Likes { get; set; }
    public long Views { get; set; }
    public long Shares { get; set; }
    public long Comments { get; set; }
    public string Type { get; set; } = "post";
    public int ImageCount { get; set; }
    public int VideoCount { get; set; }
    public string VideoCoverImage { get; set; } = "";
    public double DurationInSec { get; set; }
}
