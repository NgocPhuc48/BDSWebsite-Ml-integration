using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class ChiTietDat
{
    public long MaChiTietDat { get; set; }

    public long? MaBds { get; set; }

    public string? LoaiDat { get; set; }

    public decimal? MatTienM { get; set; }

    public decimal? ChieuSauM { get; set; }

    public string? LoaiDuong { get; set; }

    public decimal? DoRongDuongM { get; set; }

    public string? HinhDang { get; set; }

    public virtual BatDongSan? MaBdsNavigation { get; set; }
}
