using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class PhanCongTuVan
{
    public long MaPhanCong { get; set; }

    public long? MaYeuCau { get; set; }

    public long? MaTuVanVien { get; set; }

    public long? MaNguoiPhanCong { get; set; }

    public DateTime? NgayPhanCong { get; set; }

    public string? GhiChu { get; set; }

    public virtual NguoiDung? MaNguoiPhanCongNavigation { get; set; }

    public virtual NguoiDung? MaTuVanVienNavigation { get; set; }

    public virtual YeuCauTuVan? MaYeuCauNavigation { get; set; }
}
