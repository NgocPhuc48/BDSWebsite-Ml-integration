using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class KetQuaDanhGiaMoHinh
{
    public long MaKetQua { get; set; }

    public int? MaPhienBan { get; set; }

    public string? TapDuLieu { get; set; }

    public decimal? Mae { get; set; }

    public decimal? Rmse { get; set; }

    public decimal? R2 { get; set; }

    public int? SoMau { get; set; }

    public DateTime? NgayDanhGia { get; set; }

    public string? GhiChu { get; set; }

    public virtual PhienBanMoHinh? MaPhienBanNavigation { get; set; }
}
