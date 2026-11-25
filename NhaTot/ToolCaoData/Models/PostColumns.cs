using System.Text;

namespace ToolCaoData.Models;

public record PostColumn(string Key, string Label, Func<PostRecord, object?> Get);

/// <summary>Tương đương mảng COLUMNS trong App.jsx (dùng chung cho bảng và xuất CSV).</summary>
public static class PostColumns
{
    private static string Files(int n) => n > 0 ? $"{n} tệp" : "0";

    public static readonly IReadOnlyList<PostColumn> All = new List<PostColumn>
    {
        new("createTime", "Thời gian tạo", r => r.CreateTime),
        new("postUrl", "Đường dẫn bài viết", r => r.PostUrl),
        new("postId", "ID Bài viết", r => r.PostId),
        new("actionId", "ID Hành động", r => r.ActionId),
        new("text", "Nội dung văn bản", r => r.Text),
        new("author", "Tên tác giả", r => r.Author),
        new("authorUsername", "Tên người dùng", r => r.AuthorUsername),
        new("authorUserId", "ID người dùng", r => r.AuthorUserId),
        new("authorProfileUrl", "Trang cá nhân", r => r.AuthorProfileUrl),
        new("authorAvatar", "Ảnh đại diện", r => r.AuthorAvatar),
        new("likes", "Lượt thích", r => r.Likes),
        new("views", "Lượt xem", r => r.Views),
        new("shares", "Lượt chia sẻ", r => r.Shares),
        new("comments", "Bình luận", r => r.Comments),
        new("type", "Loại bài", r => r.Type),
        new("imageList", "Danh sách ảnh", r => Files(r.ImageCount)),
        new("videoList", "Danh sách video", r => Files(r.VideoCount)),
        new("videoCoverImage", "Ảnh bìa video", r => r.VideoCoverImage),
        new("durationInSec", "Thời lượng video (s)", r => r.DurationInSec),
    };

    /// <summary>Tạo nội dung CSV (có BOM UTF-8 để Excel đọc đúng tiếng Việt).</summary>
    public static string BuildCsv(IReadOnlyList<PostRecord> rows)
    {
        static string Q(object? v) => "\"" + (v?.ToString() ?? "").Replace("\"", "\"\"") + "\"";

        var sb = new StringBuilder("\uFEFF");
        sb.Append(string.Join(",", new[] { Q("STT") }.Concat(All.Select(c => Q(c.Label)))));
        for (int i = 0; i < rows.Count; i++)
        {
            sb.Append('\n');
            var row = rows[i];
            sb.Append(string.Join(",", new[] { Q(i + 1) }.Concat(All.Select(c => Q(c.Get(row))))));
        }
        return sb.ToString();
    }
}
