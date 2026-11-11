using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class LichHen
{
    public long MaLichHen { get; set; }

    public long? MaYeuCau { get; set; }

    public long? MaTin { get; set; }

    public long? MaNguoiTao { get; set; }

    public DateTime? ThoiGianHen { get; set; }

    public string? DiaDiem { get; set; }

    public string? TrangThai { get; set; }

    public string? GhiChu { get; set; }

    public virtual NguoiDung? MaNguoiTaoNavigation { get; set; }

    public virtual TinDang? MaTinNavigation { get; set; }

    public virtual YeuCauTuVan? MaYeuCauNavigation { get; set; }
}
