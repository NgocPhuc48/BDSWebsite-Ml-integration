using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class ThongBao
{
    public long MaThongBao { get; set; }

    public long? MaNguoiDung { get; set; }

    public string? TieuDe { get; set; }

    public string? NoiDung { get; set; }

    public string? LoaiThongBao { get; set; }

    public bool? DaDoc { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual NguoiDung? MaNguoiDungNavigation { get; set; }
}
