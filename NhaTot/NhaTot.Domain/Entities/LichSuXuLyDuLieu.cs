using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class LichSuXuLyDuLieu
{
    public long MaXuLy { get; set; }

    public long? MaDuLieu { get; set; }

    public string? BuocXuLy { get; set; }

    public string? KetQua { get; set; }

    public string? ChiTiet { get; set; }

    public DateTime? ThoiDiem { get; set; }

    public virtual DuLieuThuThap? MaDuLieuNavigation { get; set; }
}
