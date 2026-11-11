using System;
using System.Collections.Generic;

namespace NhaTot.Domain.Entities;

public partial class PhienBanMoHinh
{
    public int MaPhienBan { get; set; }

    public string? TenMoHinh { get; set; }

    public string? PhienBan { get; set; }

    public int? LoaiBdsapDung { get; set; }

    public string? DuongDanArtifact { get; set; }

    public DateTime? NgayHuanLuyen { get; set; }

    public int? SoMauHuanLuyen { get; set; }

    public string? GhiChu { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<KetQuaDanhGiaMoHinh> KetQuaDanhGiaMoHinhs { get; set; } = new List<KetQuaDanhGiaMoHinh>();

    public virtual ICollection<LichSuDinhGia> LichSuDinhGia { get; set; } = new List<LichSuDinhGia>();
}
