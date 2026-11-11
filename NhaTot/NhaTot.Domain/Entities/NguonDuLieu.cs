using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class NguonDuLieu
{
    public int MaNguon { get; set; }

    public string? TenNguon { get; set; }

    public string? DiaChiWebsite { get; set; }

    public string? PhamViSuDung { get; set; }

    public string? GhiChuGiayPhep { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<DuLieuThuThap> DuLieuThuThaps { get; set; } = new List<DuLieuThuThap>();
}
