using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class YeuCauTuVan
{
    public long MaYeuCau { get; set; }

    public long? MaNguoiGui { get; set; }

    public long? MaNhuCau { get; set; }

    public string? TieuDe { get; set; }

    public string? NoiDung { get; set; }

    public string? TrangThai { get; set; }

    public int? MucDoUuTien { get; set; }

    public DateTime? NgayTao { get; set; }

    public DateTime? NgayDong { get; set; }

    public virtual ICollection<LichHen> LichHens { get; set; } = new List<LichHen>();

    public virtual NguoiDung? MaNguoiGuiNavigation { get; set; }

    public virtual NhuCauTimKiem? MaNhuCauNavigation { get; set; }

    public virtual ICollection<PhanCongTuVan> PhanCongTuVans { get; set; } = new List<PhanCongTuVan>();
}
