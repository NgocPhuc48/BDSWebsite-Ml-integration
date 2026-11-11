using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class NhatKyHeThong
{
    public long MaNhatKy { get; set; }

    public long? MaNguoiDung { get; set; }

    public string? HanhDong { get; set; }

    public string? DoiTuong { get; set; }

    public string? MaDoiTuong { get; set; }

    public string? DiaChiIp { get; set; }

    public DateTime? ThoiDiem { get; set; }

    public virtual NguoiDung? MaNguoiDungNavigation { get; set; }
}
